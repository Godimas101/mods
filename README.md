# Space Engineers Mods

Custom mods for Space Engineers, primarily focused on LCD screen information displays and gameplay enhancements.

## Mods in This Repository

### InfoLCD - Apex Update
Current version of the InfoLCD mod suite with advanced features:
- Multi-screen summary displays (Items, Cargo, Power, Ammo, Components, etc.)
- Scrolling support with configurable speed and direction
- Category filtering and visibility controls
- Multi-category scrolling with MaxListLines limits
- Subgrid scanning with performance optimization
- Color-coded status indicators
- CustomData configuration with INI format

**Status:** Active development, published on Steam Workshop

### InfoLCD - Apex Advanced
Modified version of the InfoLCD mod, designed to work with the Apex Advanced mod

**Status:** Maintenance mode (bug fixes only)

### Artillery MKII Turret - Goliath [Mod Adjuster For SG]
Turret mod adjuster for SturmGrenadier servers.

**Status:** Stable

### Not Just For Looks
Gameplay enhancement mod.

**Status:** Stable

### Scripts
Additional utility scripts and standalone tools.

## Development

### Prerequisites
- Space Engineers game installation
- Space Engineers Mod SDK (auto-installs via Steam)
- Visual Studio or VS Code for C# development
- .NET Framework (included with SDK)

### SDK Location
The Space Engineers Mod SDK is located here once installed from Steam:
```
SteamLibrary\steamapps\common\SpaceEngineersModSDK\
```

After Steam installs/updates Space Engineers, the SDK is automatically updated. Use it for:
- API reference (DLLs with XML documentation)
- Decompiling source implementations
- Asset structure examples (models, textures)
- Testing tools (ModelViewer, etc.)

### Building Mods
1. Reference SDK DLLs in your C# project for IntelliSense
2. Write mod scripts in `Data/Scripts/<namespace>/`
3. Register text surface scripts in `Data/TextSurfaceScripts.sbc` (if applicable)
4. Test in game by placing mod in `%AppData%\SpaceEngineers\Mods\` or via workshop

### Testing
- Launch Space Engineers in Creative mode
- Enable mod in World Settings
- Test with various LCD sizes (corner LCD vs standard)
- Verify CustomData configuration options
- Test with subgrids and complex ships

## AI Assistant Skill

This repository includes a comprehensive **Space Engineers Modding Skill** for AI assistants (GitHub Copilot, Claude, etc.).

### What It Provides
- Complete modding workflows for all mod types
- API quick reference with common interfaces and patterns
- Known issues and solutions for common pitfalls
- Text surface script template
- SDK usage guidance
- Backward compatibility best practices

### Using the Skill

**With Claude:**
- Type `/space-engineers-modding` in chat
- Or mention Space Engineers modding topics naturally (auto-loads)

**With GitHub Copilot (VS Code):**
- The skill is automatically available when you open this workspace
- Ask questions about Space Engineers modding in the chat

### Setting Up the Skill on Your Own Computer

The skill is located at [`.github/skills/space-engineers-modding/`](.github/skills/space-engineers-modding/) and is automatically available when you clone this repository.

**For VS Code / GitHub Copilot:**
1. Clone this repository
2. Open the `space-engineers-mods` folder in VS Code
3. The skill will be automatically detected from `.github/skills/`
4. Start chatting - the skill is available immediately

**For Claude Desktop / Claude.ai:**

**Option 1: Project-Scoped (this repository only)**
1. Clone this repository
2. Add this folder to your Claude project workspace
3. The skill will be available when working in this project
4. Type `/space-engineers-modding` or mention modding topics

**Option 2: Global (available in all Claude sessions)**
1. Copy the skill folder to Claude's global skills directory:
   - Windows: `C:\Users\<YourName>\.claude\skills\space-engineers-modding\`
   - macOS/Linux: `~/.claude/skills/space-engineers-modding/`
2. Create the `.claude/skills/` directory if it doesn't exist
3. Copy the entire `space-engineers-modding` folder (including SKILL.md, references/, templates/)
4. Restart Claude Desktop (if using desktop app)
5. The skill is now available in all your Claude conversations

**Skill Contents:**
```
space-engineers-modding/
├── SKILL.md                    # Main skill file
├── README.md                   # Skill documentation
├── references/
│   ├── api-quick-reference.md  # API interfaces and patterns
│   └── known-issues.md         # Common pitfalls and solutions
└── templates/
    └── text-surface-script.cs  # LCD script template
```

**Note:** The skill automatically references the SDK location (`D:\SteamLibrary\steamapps\common\SpaceEngineersModSDK\`). Update paths in `SKILL.md` if your installation differs.

## Documentation

- **[CLAUDE.md](./CLAUDE.md)** - Comprehensive AI assistant guide with SDK structure, API patterns, and modding concepts
- **[SCROLLING_IMPLEMENTATION.md](./SCROLLING_IMPLEMENTATION.md)** - Detailed scrolling system documentation with both unified and multi-category approaches
- **[FUTURE_IDEAS.md](./FUTURE_IDEAS.md)** - Planned features and enhancements
- **[.github/skills/space-engineers-modding/](./.github/skills/space-engineers-modding/)** - AI assistant skill for modding workflows

## Key Resources

### Official Documentation
- [Space Engineers Modding Wiki](https://spaceengineers.wiki.gg/wiki/Modding/Reference) - Primary reference
- [Mod API Documentation](https://keensoftwarehouse.github.io/SpaceEngineersModAPI/api/index.html) - Complete API reference
- [Steam Workshop](https://steamcommunity.com/app/244850/workshop/) - Browse and publish mods

### Community
- [Space Engineers Discord](https://discord.gg/spaceengineers) - Active modding community
- [Keen Software House Forums](https://forums.keenswh.com/) - Official forums with modding section

## Important Modding Principles

### Backward Compatibility (Critical for Workshop Releases)
- **Never break existing CustomData formats** - Add new options, don't remove/rename
- **Support old config keys** - Check for both old and new formats
- **Test with existing configs** - Verify old CustomData still works before releasing
- **Graceful degradation** - New features should be optional
- **Document breaking changes** - Clearly note incompatibilities in mod descriptions

### Performance
- Cache subgrid scans and update infrequently
- Minimize LINQ in performance-critical code
- Always check for dedicated servers: `if (MyAPIGateway.Utilities?.IsDedicated ?? false) return;`
- Use composite keys for dictionaries when mixing item TypeIds

### Safety
- Always null-check block/component access
- Wrap DetailedInfo parsing in try/catch
- Never rely on default parameter values
- Validate all user configuration input

## Common Issues

### Item Type Collisions
Items can share SubtypeIds but have different TypeIds (e.g., `ConsumableItem_Fruit` vs `SeedItem_Fruit`).

**Solution:** Use composite keys: `$"{typeId}_{subtypeId}"`

### Multi-Category Scrolling Space Calculation
Each category must calculate space from current drawing position, not screen top.

**Solution:** `float currentY = position.Y - surfaceData.viewPortOffsetY;`

### VRage.MyFixedPoint
Inventory amounts are `MyFixedPoint`, not `int` or `float`.

**Solution:** `int amount = (int)item.Amount;` or `item.Amount.ToIntSafe()`

See [CLAUDE.md](./CLAUDE.md) and the skill's `known-issues.md` for complete lists.

## Contributing

When modifying mods:
1. Test thoroughly in Creative and Survival modes
2. Verify backward compatibility with existing CustomData
3. Update documentation if adding new features
4. Follow existing code patterns and conventions
5. Add comments for complex logic

## License

These mods are created for personal use and shared on the Space Engineers Steam Workshop. Please respect the Space Engineers EULA and modding guidelines.

## Credits

Created for [The Canadian Space](https://thecanadian.space) community and published on the Space Engineers Steam Workshop.

SDK provided by Keen Software House.

AI assistance powered by GitHub Copilot and Claude.
