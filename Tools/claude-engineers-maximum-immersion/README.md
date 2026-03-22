# Claude Engineers: Maximum Immersion

> *Talk to your AI co-engineer directly from the cockpit.*

A local development tool that bridges Space Engineers in-game chat to Claude in VS Code. Type commands in-game, get AI responses back as chat messages — with optional screenshot context so Claude can see exactly what you're looking at.

---

## How It Works

```
You type /ai-start in SE chat
         │
         ▼
Pulsar plugin (C# client plugin)
  ├─ Intercepts chat messages
  ├─ Optional: captures screenshot on /ai-screenshot
  └─ Sends JSON over WebSocket → localhost:8081
         │
         ▼
VS Code Extension (TypeScript / Node.js)
  ├─ WebSocket server receives { text, image? }
  ├─ Calls Claude API (with vision if screenshot attached)
  ├─ Shows conversation in a sidebar chat panel
  └─ Sends reply back over WebSocket
         │
         ▼
Pulsar plugin receives reply
  └─ Displays as in-game chat message from "AI Assistant"
```

Everything runs locally. No cloud relay, no dedicated server, <100ms latency.

---

## Commands (In-Game Chat)

| Command | Effect |
|---------|--------|
| `/ai-start` | Enable AI forwarding — all subsequent messages go to Claude |
| `/ai-stop` | Disable forwarding, return chat to normal |
| `/ai-screenshot` | Next message will include a screenshot of the game window |

---

## Two Components

### 1. Pulsar Client Plugin (`SEAIChatBridge`)
- C# plugin loaded by [Pulsar](https://github.com/SpaceGT/Pulsar)
- Hooks into SE chat via `MyAPIGateway.Utilities.MessageEntered`
- Manages WebSocket connection to the VS Code extension
- Handles screenshot capture

### 2. VS Code Extension (`se-claude-bridge`)
- TypeScript extension with a sidebar chat panel (WebviewView)
- Runs a local WebSocket server on port 8081
- Calls the Anthropic Claude API with text + optional vision
- Shows full conversation history in the chat panel

---

## Status

> Work in progress — not yet built.

---

## Requirements

- Space Engineers (Steam)
- [Pulsar](https://github.com/SpaceGT/Pulsar) plugin loader
- VS Code
- An Anthropic API key

---

*Please prompt responsibly.*
