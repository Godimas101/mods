# Space Engineers Modding Skill

Claude skill for creating, debugging, and publishing Space Engineers mods.

## Skill Structure

- **SKILL.md** - Main skill file with comprehensive modding guide
- **references/** - Progressive loading references
  - `api-quick-reference.md` - Common interfaces and patterns
  - `known-issues.md` - Pitfalls and solutions
- **templates/** - Starting templates
  - `text-surface-script.cs` - LCD script template

## What This Skill Provides

This skill gives AI assistants comprehensive Space Engineers modding knowledge:

- **Complete workflows** for all mod types (blocks, items, scripts, weapons, gameplay)
- **API quick reference** with common interfaces (`IMyTerminalBlock`, `IMyTextSurface`, etc.)
- **Known issues** and solutions for common pitfalls (item type collisions, VRage types, etc.)
- **Code templates** for text surface scripts (LCD displays)
- **SDK usage guidance** (decompilation, IntelliSense, asset reference)
- **Backward compatibility** best practices for Workshop releases

## Invocation

**With Claude:**
- Type `/space-engineers-modding` in chat
- Or mention topics naturally: "Space Engineers mod", "LCD script", "CustomData", etc.
- Claude will auto-load the skill when relevant

**With GitHub Copilot (VS Code):**
- The skill is automatically available in this workspace
- Ask modding questions in Copilot Chat

## Setting Up on Your Computer

### For VS Code / GitHub Copilot

**Project-Scoped (Recommended):**
1. Clone the repository containing this skill
2. Open the workspace in VS Code
3. Skills in `.github/skills/` are automatically detected
4. Start using immediately - no additional setup needed

### For Claude Desktop / Claude.ai

**Option 1: Project-Scoped (this repository only)**
1. Clone the repository
2. Add the folder to your Claude project workspace
3. Type `/space-engineers-modding` or mention modding topics
4. Skill loads automatically when working in this project

**Option 2: Global (available everywhere)**

Make the skill available in all Claude conversations:

**Windows:**
```powershell
# Create Claude skills directory
mkdir "$env:USERPROFILE\.claude\skills" -Force

# Copy skill folder
Copy-Item -Recurse -Path ".\.github\skills\space-engineers-modding" `
    -Destination "$env:USERPROFILE\.claude\skills\space-engineers-modding"
```

**macOS/Linux:**
```bash
# Create Claude skills directory
mkdir -p ~/.claude/skills

# Copy skill folder
cp -r ./.github/skills/space-engineers-modding ~/.claude/skills/space-engineers-modding
```

**Manual Copy:**
1. Navigate to your user directory:
   - Windows: `C:\Users\<YourName>\`
   - macOS/Linux: `~` (home directory)
2. Create folder: `.claude/skills/` (if it doesn't exist)
3. Copy the entire `space-engineers-modding` folder into `.claude/skills/`
4. Restart Claude Desktop (if using desktop app)

**Verify Installation:**
- Type `/` in Claude chat
- You should see `space-engineers-modding` in the skills list

## Customizing for Your Setup

If your Space Engineers SDK is in a different location, update the path in `SKILL.md`:

```markdown
<!-- Find this section in SKILL.md -->
### SDK Location
```
D:\SteamLibrary\steamapps\common\SpaceEngineersModSDK\
```

<!-- Update to your path -->
```

Common SDK locations:
- Default Steam: `C:\Program Files (x86)\Steam\steamapps\common\SpaceEngineersModSDK\`
- Custom Steam library: `<YourDrive>\SteamLibrary\steamapps\common\SpaceEngineersModSDK\`

## Skill Coverage

This skill covers ALL Space Engineers mod types:
- **Text surface scripts** (LCD displays) - Most comprehensive coverage
- **Custom blocks** - Block definitions and logic
- **Items & components** - Physical items and crafting components
- **Weapons & tools** - Handheld and ship weapons
- **Gameplay mechanics** - Session-wide modifications
- **Session components** - Global game logic

## Usage Examples

**Creating a new text surface script:**
```
"I need to create an LCD script that shows battery status"
```
Claude will use the skill to provide:
- Complete template code
- API references for `IMyBatteryBlock`
- CustomData configuration setup
- Registration in `TextSurfaceScripts.sbc`

**Debugging issues:**
```
"My inventory scanning is showing doubled item counts"
```
Claude will reference known issues and provide the composite key solution.

**Understanding APIs:**
```
"How do I access power output from a reactor?"
```
Claude will provide API quick reference for `IMyPowerProducer` and `MyResourceSourceComponent`.

## Resources

- **SDK**: `D:\SteamLibrary\steamapps\common\SpaceEngineersModSDK\`
- **Project guide**: [CLAUDE.md](../../../CLAUDE.md)
- **Official docs**: [SE Modding Wiki](https://spaceengineers.wiki.gg/wiki/Modding/Reference)
- **API docs**: [Mod API Reference](https://keensoftwarehouse.github.io/SpaceEngineersModAPI/api/index.html)

## Contributing to the Skill

To improve this skill:
1. Update `SKILL.md` with new patterns or workflows
2. Add to `references/` for complex topics
3. Update `templates/` with new starter code
4. Document in `known-issues.md` if you find new pitfalls

Keep SKILL.md under 500 lines - use reference files for detailed content.

