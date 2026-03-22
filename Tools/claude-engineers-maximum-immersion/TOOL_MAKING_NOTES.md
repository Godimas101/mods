# Claude Engineers: Maximum Immersion — Tool Notes

## Concept

A local dev tool that lets you talk to Claude from inside Space Engineers. Chat commands in-game are forwarded over WebSocket to a VS Code extension, which calls the Claude API and sends the reply back as an in-game chat message. Screenshots can be attached so Claude has visual context.

**Local only.** No cloud relay, no dedicated server. Single-player / offline worlds only (Pulsar is client-side).

---

## Architecture

```
SE Game (Pulsar plugin)  ──WebSocket──►  VS Code Extension  ──HTTPS──►  Claude API
                         ◄────────────                       ◄──────────
```

### Two components:
1. **Pulsar client plugin** (C#) — game side
2. **VS Code extension** (TypeScript/Node.js) — AI side

---

## Component 1: Pulsar Plugin (`SEAIChatBridge`)

### What Pulsar Is
Pulsar is a client-side plugin loader for Space Engineers. Plugins are compiled from GitHub source on the player's machine — they are NOT game mods and do NOT go in the world's mod list. Always active when Pulsar is running.

Key distinction: Pulsar plugins use `IPlugin { Init(), Update(), Dispose() }` — **not** `MySessionComponentBase`. This is a common mistake (Grok made it in early notes).

### Plugin Template
**Correct template for client-only (Pulsar) plugins:** [github.com/sepluginloader/ClientPluginTemplate](https://github.com/sepluginloader/ClientPluginTemplate)
- Archived read-only June 2025 but still usable — SE1 Legacy API hasn't changed enough to break it
- Clone, run `python setup.py MyPluginName` (Python 3.x required), open .sln in Visual Studio
- The older [PluginTemplate](https://github.com/sepluginloader/PluginTemplate) generates Client + Torch + DS in one solution — use it only if you need all three targets

### Correct Plugin Structure

The architecture needs two files: `Plugin.cs` (the `IPlugin` entry point) and a session component that handles all game-API-dependent logic. PluginLoader auto-discovers `[MySessionComponentDescriptor]` classes in your assembly.

**Plugin.cs** — IPlugin entry point, minimal:
```csharp
using System.Runtime.CompilerServices;
using VRage.Plugins;

namespace SEAIChatBridge
{
    public class SEAIChatBridgePlugin : IPlugin
    {
        [MethodImpl(MethodImplOptions.NoInlining)]  // ⚠️ Required — prevents JIT inlining which breaks Harmony
        public void Init(object gameInstance)
        {
            // ⚠️ MyAPIGateway is null here — don't touch game APIs
            // Harmony patches go here if needed; we don't need any for this plugin
        }

        public void Update() { }

        public void Dispose() { }
    }
}
```

**BridgeSession.cs** — session component handles everything game-related:
```csharp
using System.Collections.Concurrent;
using Sandbox.ModAPI;
using VRage.Game.Components;

namespace SEAIChatBridge
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class BridgeSession : MySessionComponentBase
    {
        public static BridgeSession Instance;

        private bool _aiEnabled = false;
        private bool _screenshotNext = false;
        private WsClient _ws;
        private readonly ConcurrentQueue<string> _pendingReplies = new ConcurrentQueue<string>();

        public override void BeforeStart()
        {
            Instance = this;
            _ws = new WsClient("ws://localhost:8081", OnReplyReceived);
            // ⚠️ Use MessageEntered (NOT MessageEnteredSender) for local command interception
            // MessageEntered: sendToOthers = false suppresses local display too
            // MessageEnteredSender: sendToOthers only controls network relay, not local display
            MyAPIGateway.Utilities.MessageEntered += OnMessageEntered;
        }

        private void OnMessageEntered(string message, ref bool sendToOthers)
        {
            var msg = message.Trim();

            if (msg == "/ai-start")      { _aiEnabled = true;  ShowMsg("AI forwarding ON");  sendToOthers = false; return; }
            if (msg == "/ai-stop")       { _aiEnabled = false; ShowMsg("AI forwarding OFF"); sendToOthers = false; return; }
            if (msg == "/ai-screenshot") { _screenshotNext = true; ShowMsg("Next message will include screenshot"); sendToOthers = false; return; }

            if (!_aiEnabled) return;

            string imageBase64 = null;
            if (_screenshotNext)
            {
                imageBase64 = CaptureScreenshot();
                _screenshotNext = false;
            }

            // Fire-and-forget send — WsClient handles background thread
            _ws.SendAsync(new { type = "player_chat", message = msg, imageBase64 });
            sendToOthers = false;
        }

        // Called on background WS thread — must NOT touch game APIs directly
        private void OnReplyReceived(string replyText)
        {
            _pendingReplies.Enqueue(replyText);
        }

        public override void UpdateAfterSimulation()
        {
            // Drain reply queue on game thread — safe to call ShowMessage here
            while (_pendingReplies.TryDequeue(out string reply))
                MyAPIGateway.Utilities.ShowMessage("AI Assistant", reply);
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Utilities.MessageEntered -= OnMessageEntered;
            _ws?.Dispose();
            Instance = null;
        }

        private void ShowMsg(string text) =>
            MyAPIGateway.Utilities?.ShowMessage("AI Bridge", text);
    }
}
```

### ⚠️ MessageEntered vs MessageEnteredSender

Use `MessageEntered(string message, ref bool sendToOthers)` for local command interception. Setting `sendToOthers = false` suppresses the message from both the network AND local chat display.

`MessageEnteredSender(ulong sender, string message, ref bool sendToOthers)` only controls **network relay** — `sendToOthers = false` won't hide the message from the local player's chat. Wrong tool for this job.

### ⚠️ Thread Safety — Reply Queue Pattern

`MyAPIGateway.Utilities.ShowMessage()` is not thread-safe and must be called on the game thread. WebSocket receive callbacks run on a background thread. The correct pattern:
1. WS callback enqueues reply into `ConcurrentQueue<string>`
2. `UpdateAfterSimulation()` (game thread) drains the queue and calls `ShowMessage`

Never call `InvokeOnGameThread()` from a WS callback — use the queue pattern instead.

### WebSocket from C#
Options:
- `System.Net.WebSockets.ClientWebSocket` — built into .NET 4.5+, no NuGet needed, works reliably in SE's .NET Framework 4.8.1 environment
- `WebSocketSharp` — simpler API but no precedent in PluginHub plugins; requires bundling an extra DLL

**Recommendation:** `ClientWebSocket` — no dependencies, confirmed working in SE's runtime. Wrap in a small helper class that handles reconnect logic.

**NuGet note:** If you do want a third-party library, Pulsar's PluginHub descriptor supports `<NuGetReferences>` with `<PackageReference>` entries — packages are downloaded at plugin compile time on the player's machine. But for this use case, BCL ClientWebSocket is the right call.

**C# 7.3 constraint:** SE uses .NET Framework 4.8 with C# 7.3 max — no async streams, no `IAsyncEnumerable`. Basic `async`/`await` works fine. Run the receive loop in `Task.Run(async () => { while (true) { await ws.ReceiveAsync(...); } })`. Don't `await` inside event handlers (deadlock risk on game thread) — use fire-and-forget + the reply queue.

**Concurrent access:** `ClientWebSocket` only supports one concurrent send and one concurrent receive per instance. Serialize sends with a `SemaphoreSlim(1,1)` if multiple threads could send.

### Screenshot Capture

**⚠️ F4 / SendKeys will NOT work.** SE uses VRage's own input polling (`MyInput.Static`) rather than the Win32 message queue.

**⚠️ `System.Drawing` (Graphics.CopyFromScreen) is NOT in SE's Bin64.** You'd need to ship it as a bundled DLL, and it may return black frames in D3D11 modes anyway. Skip it.

**SE screenshot save path:** `%AppData%\Roaming\SpaceEngineers\Screenshots\` — timestamped PNGs.

**Recommended approach — `MyRenderProxy.TakeScreenshot()` + read file:**
```csharp
using VRageRender;
using System.IO;

private string CaptureScreenshot()
{
    string dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SpaceEngineers", "Screenshots");
    string path = Path.Combine(dir, $"ai_{DateTime.Now:yyyyMMdd_HHmmss}.png");

    // Use SE's own render system to capture — stays within SE ecosystem
    MyRenderProxy.TakeScreenshot(new VRageMath.Vector2(1f, 1f), path, false, false, false);

    // Wait 1-2 frames for the file to appear (use a flag + UpdateAfterSimulation countdown)
    // Then read and encode:
    if (!File.Exists(path)) return null;
    byte[] bytes = File.ReadAllBytes(path);
    File.Delete(path);  // clean up
    return Convert.ToBase64String(bytes);
}
```

Note: There's a 1-2 frame delay between calling `TakeScreenshot` and the file appearing. The cleanest pattern is to set a flag in `OnMessageEntered`, then check for the file in `UpdateAfterSimulation` on the next tick.

**Alternative — Harmony patch SE's internal screenshot method:** Most robust (intercepts bitmap data before file write), but requires decompiling `Sandbox.Game.dll` to find the target method. Overkill for v1.

### JSON Serialization

**⚠️ Do NOT use `System.Text.Json`** — it is a .NET Core 3+ / .NET 5+ library. It does not exist in .NET Framework 4.8 without an extra NuGet package, and SE's Bin64 doesn't include it.

**Use `Newtonsoft.Json`** — it is bundled with PluginLoader (version 13.0.3) and available to all plugins at zero cost. No NuGet reference needed at runtime (add it to your `.csproj` for IntelliSense but do NOT ship the DLL in your plugin ZIP — PluginLoader provides it).

```csharp
using Newtonsoft.Json;

var payload = new { type = "player_chat", message = msg, imageBase64 };
string json = JsonConvert.SerializeObject(payload);
```

### HarmonyLib Notes

- **`[MethodImpl(MethodImplOptions.NoInlining)]` on `Init()`** — required. Without it the JIT may inline the method, breaking Harmony's ability to detour it.
- **Never call `harmony.UnpatchAll()` in `Dispose()`** — it removes patches from ALL loaded plugins, not just yours. The template comments state this explicitly. Just leave patches in place; Pulsar manages cleanup on game exit.
- `harmony.PatchAll(Assembly.GetExecutingAssembly())` in `Init()` is the correct place to apply patches.
- For this plugin, we don't actually need any Harmony patches — `MessageEnteredSender` via the ModAPI is sufficient. Harmony would only be needed if we needed to intercept something without a clean ModAPI hook.

### Thread Safety

- `IPlugin.Update()` and `OnMessageEntered` both run on the **main simulation thread** — safe to call game APIs.
- **WebSocket receive callbacks are on a background thread.** Never touch `MyAPIGateway` or game state directly from a WS callback. Marshal back to the game thread:
  ```csharp
  _ws.OnMessage += (text) =>
  {
      MyAPIGateway.Utilities.InvokeOnGameThread(() =>
      {
          ShowMsg("AI Assistant", text);
      });
  };
  ```

---

## Component 2: VS Code Extension (`se-claude-bridge`)

### Scaffolding

```bash
npm install -g yo generator-code
yo code
# Pick: New Extension (TypeScript)
# Do NOT pick "web extension" — web extensions can't use Node.js APIs

cd se-claude-bridge
npm install ws @anthropic-ai/sdk
npm install --save-dev @types/ws esbuild
```

### File Structure

```
se-claude-bridge/
├── src/
│   └── extension.ts        # activate() entry point
├── media/
│   └── chat.js             # webview UI (runs in browser sandbox)
├── package.json            # extension manifest
├── esbuild.js              # build config
└── tsconfig.json
```

### package.json (key sections)

```json
{
  "engines": { "vscode": "^1.85.0" },
  "main": "./dist/extension.js",
  "activationEvents": [],
  "contributes": {
    "viewsContainers": {
      "activitybar": [
        { "id": "se-claude", "title": "SE Claude", "icon": "media/robot.svg" }
      ]
    },
    "views": {
      "se-claude": [
        { "type": "webview", "id": "seClaude.chatView", "name": "Chat" }
      ]
    },
    "configuration": {
      "properties": {
        "seClaude.apiKey": { "type": "string", "description": "Anthropic API key" }
      }
    }
  }
}
```

`activationEvents: []` — VS Code 1.74+ auto-activates when the contributed view is opened.

### WebSocket Server (ws package)

```typescript
import { WebSocketServer } from 'ws';

const wss = new WebSocketServer({ port: 8081 });
wss.on('connection', (ws) => {
    ws.on('message', async (raw) => {
        const msg = JSON.parse(raw.toString()); // { message, imageBase64? }
        const reply = await callClaude(msg.message, msg.imageBase64);
        ws.send(JSON.stringify({ type: 'reply', text: reply }));
    });
});
```

### Claude API Call (with vision)

```typescript
import Anthropic from '@anthropic-ai/sdk';

const anthropic = new Anthropic({ apiKey: /* from settings */ });

async function callClaude(text: string, imageBase64?: string) {
    const content: any[] = [];

    if (imageBase64) {
        content.push({
            type: 'image',
            source: { type: 'base64', media_type: 'image/png', data: imageBase64 }
            // ⚠️ data must be raw base64 — strip "data:image/png;base64," prefix if present
        });
    }

    content.push({ type: 'text', text });

    const response = await anthropic.messages.create({
        model: 'claude-sonnet-4-6',
        max_tokens: 1024,
        messages: [{ role: 'user', content }]
    });

    return response.content[0].text;
}
```

**Vision notes:**
- Put image block BEFORE text block for best results
- Max 5 MB per image — resize if needed (target ~1.15 MP / 1568px max side)
- Supported types: `image/jpeg`, `image/png`, `image/gif`, `image/webp`
- ~1600 tokens per screenshot at typical SE resolution — keep in mind cost

### WebviewView (Sidebar Chat Panel)

Use `WebviewView` (not `WebviewPanel` or `TreeView`) — it lives in the sidebar.

```typescript
class ChatViewProvider implements vscode.WebviewViewProvider {
    resolveWebviewView(webviewView: vscode.WebviewView) {
        webviewView.webview.options = { enableScripts: true };
        webviewView.webview.html = getHtml(webviewView.webview);

        webviewView.webview.onDidReceiveMessage((msg) => {
            // handle messages from the webview UI
        });
    }

    postMessage(msg: object) {
        this._view?.webview.postMessage(msg);
    }
}
```

The webview iframe runs in a browser sandbox — cannot import Node modules. All WS/API logic stays in the extension host. They communicate via `postMessage` / `onDidReceiveMessage`.

### Bundling (esbuild)

```javascript
// esbuild.js
require('esbuild').build({
    entryPoints: ['src/extension.ts'],
    bundle: true,
    outfile: 'dist/extension.js',
    external: ['vscode'],   // ⚠️ MUST be external — provided by VS Code runtime
    format: 'cjs',
    platform: 'node',
});
```

`ws` bundles cleanly — pure JS, no native `.node` file.

### Gotchas

| Gotcha | Fix |
|--------|-----|
| `external: ['vscode']` missing | Cryptic load error — always exclude vscode from bundle |
| Port 8081 already bound on re-activate | Guard with `if (!wss)` or close in `deactivate()` |
| `dangerouslyAllowBrowser` on Anthropic SDK | Not needed — extension host is real Node.js, not a browser |
| Webview JS trying to `require()` | Won't work — webview is sandboxed. Keep all logic in extension.ts |
| Missing Content-Security-Policy in webview HTML | Scripts silently fail — always include CSP meta tag with nonces |
| API key in source | Use `context.secrets.get()` or `vscode.workspace.getConfiguration()` |

---

## Data Flow Summary

```
[SE Chat] /ai-screenshot
[SE Chat] "What block should I use for this thruster mount?"
    → Pulsar plugin captures screenshot (F4 → read PNG → base64)
    → sends JSON: { message: "...", imageBase64: "..." }
    → WebSocket → localhost:8081

[VS Code Extension]
    → receives JSON
    → calls Claude API: [image_block, text_block]
    → gets reply text
    → sends JSON: { type: "reply", text: "..." } → WebSocket → game
    → posts to chat panel webview

[SE Chat] "AI Assistant: You could use a Blast Door block here..."
```

---

## Build Order

1. Pulsar plugin (C#) — simpler, fewer moving parts, testable in-game immediately
2. VS Code extension (TypeScript) — WebSocket server + chat panel + Claude API

Can develop and test the plugin side first by just `console.log`ing what would be sent, before the extension exists.

---

## Open Questions

- [ ] Should the VS Code extension maintain a system prompt that describes the player's mod project? (auto-read from CLAUDE.md / MOD_MAKING_NOTES.md?)
- [ ] Should conversation history persist across sessions, or reset each `/ai-start`?
- [x] ~~Screenshot: F4 method or P/Invoke?~~ → **`MyRenderProxy.TakeScreenshot()` + read file.** F4/SendKeys won't work (VRage input). `System.Drawing`/CopyFromScreen not in SE's Bin64. Use SE's own render system.
- [ ] Port 8081 configurable in VS Code settings?
- [ ] Should the extension auto-start the WS server when VS Code opens, or only on command?

---

## References

- [Pulsar loader](https://github.com/SpaceGT/Pulsar)
- [ClientPluginTemplate](https://github.com/sepluginloader/ClientPluginTemplate) — client-only (use this)
- [PluginTemplate (unified Client+Torch+DS)](https://github.com/sepluginloader/PluginTemplate)
- [Pulsar Discord](https://discord.gg/z8ZczP2YZY)
- [VS Code Extension API — Webview](https://code.visualstudio.com/api/extension-guides/webview)
- [VS Code Extension Anatomy](https://code.visualstudio.com/api/get-started/extension-anatomy)
- [VS Code webview-view-sample](https://github.com/microsoft/vscode-extension-samples/blob/main/webview-view-sample/src/extension.ts)
- [ws npm package](https://www.npmjs.com/package/ws)
- [@anthropic-ai/sdk](https://www.npmjs.com/package/@anthropic-ai/sdk)
- [Anthropic Vision docs](https://platform.claude.com/docs/en/docs/build-with-claude/vision)

---

## Session Log

### 2026-03-22 — Project scoped + research complete
- Concept originated from Grok conversation (in-game Claude bridge via Pulsar + VS Code extension)
- Grok's code had critical errors: used `[MySessionComponentDescriptor]` (game mod pattern) instead of `IPlugin`, linked wrong template, used wrong `MessageEnteredSender` for command suppression, used Update() polling for API readiness
- Researched VS Code extension development and Pulsar plugin specifics
- Documented full architecture, both components, gotchas, and open questions
- Pulsar research confirmed (two research passes, second corrected the first):
  - **Session component required** (`[MySessionComponentDescriptor]`) — PluginLoader auto-discovers it; subscribe in `BeforeStart()`, unsubscribe in `UnloadData()`
  - **`MessageEntered`** (not `MessageEnteredSender`) for local command interception — `sendToOthers=false` suppresses local display; `MessageEnteredSender.sendToOthers` only controls network relay
  - `ClientWebSocket` works in SE's .NET 4.8.1; C# 7.3 max — basic async/await fine, no async streams
  - **`Newtonsoft.Json`** (bundled with PluginLoader) — not `System.Text.Json` (not available in .NET Framework 4.8)
  - F4/SendKeys screenshot won't work — SE uses VRage input, not Win32 message queue
  - **`MyRenderProxy.TakeScreenshot()`** + read file; `System.Drawing` not in SE's Bin64
  - WS callbacks are background thread — use `ConcurrentQueue` + `UpdateAfterSimulation` drain (not InvokeOnGameThread)
  - `[MethodImpl(NoInlining)]` required on Init(); never call `harmony.UnpatchAll()`
  - Template: `sepluginloader/ClientPluginTemplate` (archived Jun 2025 but still usable)
