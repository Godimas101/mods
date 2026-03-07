---
name: space-engineers-modding
description: 'Create, debug, and publish Space Engineers mods (blocks, items, scripts, weapons, gameplay). Use for: creating new mods, fixing mod errors, adding features, publishing to Workshop, SDK reference, API usage, backward compatibility, CustomData configuration, inventory systems, power systems, text surface scripts.'
argument-hint: 'What type of mod to create/modify/debug'
---

# Space Engineers Modding

Expert assistance for creating, modifying, debugging, and publishing Space Engineers mods of all types.

## When to Use

- Creating new mods (blocks, items, weapons, tools, scripts, gameplay mechanics)
- Debugging mod errors or unexpected behavior
- Adding features to existing mods
- Publishing or updating mods on Steam Workshop
- Understanding Space Engineers API and SDK
- Working with inventories, power systems, CustomData, or block interactions
- Implementing text surface scripts (LCD displays)
- Ensuring backward compatibility for public Workshop releases

## Core Resources

### SDK Location
```
D:\SteamLibrary\steamapps\common\SpaceEngineersModSDK\
```

**Key folders:**
- `Bin64_Profile/` - Game DLLs with XML documentation for IntelliSense
- `OriginalContent/` - Vanilla game assets (models, materials, definitions)
- `Tools/VRageEditor/` - Model viewer, animation controller, behavior tree editor

### Documentation
- [Space Engineers Modding Wiki](https://spaceengineers.wiki.gg/wiki/Modding/Reference) - Primary reference
- [Mod API Documentation](https://keensoftwarehouse.github.io/SpaceEngineersModAPI/api/index.html) - Complete API reference
- [CLAUDE.md](../../../CLAUDE.md) - Project-specific modding guide with patterns and gotchas

## Mod Types & Workflows

### 1. Text Surface Scripts (LCD Displays)
**Base Class:** `MyTextSurfaceScriptBase`
**Use Case:** Custom information displays on LCD screens
**Key APIs:** `IMyTextSurface`, `MySpriteDrawFrame`, `MySprite`

**Common Patterns:**
- Inherit from `MyTextSurfaceScriptBase`
- Override `Run()` for continuous updates or `Dispose()` for cleanup
- Access terminal block: `m_surface.Entity as IMyTerminalBlock`
- Parse CustomData with `MyIni` for user configuration
- Use `m_surface.DrawFrame()` for sprite rendering
- Check dedicated server: `if (MyAPIGateway.Utilities?.IsDedicated ?? false) return;`

### 2. Custom Blocks
**Base Classes:** `MyGameLogicComponent`, `MyTerminalBlock`
**Use Case:** New functional blocks or modified vanilla blocks
**File Structure:**
- `Data/CubeBlocks/*.sbc` - Block definitions
- `Data/Scripts/` - Block logic (optional)
- `Models/` - 3D models and textures

**Key Considerations:**
- Define block in SBC file with proper ObjectBuilder
- Reference models and build stages
- Implement custom logic with `MyGameLogicComponent`
- Add terminal controls for player interaction

### 3. Items & Components
**Use Case:** Custom tools, weapons, consumables, or components
**File Structure:**
- `Data/PhysicalItems.sbc` - Item definitions
- `Data/Components.sbc` - Component definitions
- `Models/Items/` - 3D models for items

### 4. Weapons & Tools
**Use Case:** Custom handheld or ship weapons
**File Structure:**
- `Data/Weapons/*.sbc` - Weapon definitions
- `Data/Ammo/*.sbc` - Ammunition definitions
- Logic in scripts if needed

### 5. Gameplay Mechanics
**Use Case:** Session-wide modifications (economy, spawn, environmental)
**Base Class:** `MySessionComponentBase`
**Pattern:** Register session component to run game logic globally

## Essential API Patterns

### Block Discovery & Access
```csharp
// Get terminal block
IMyTerminalBlock terminalBlock = block as IMyTerminalBlock;

// Get grid
IMyCubeGrid grid = terminalBlock.CubeGrid;

// Get all blocks of type
List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid)
    .GetBlocksOfType<IMyPowerProducer>(blocks);
```

### Inventory Access
```csharp
// Get inventory
IMyInventory inventory = block.GetInventory(0);

// Iterate items
List<MyInventoryItem> items = new List<MyInventoryItem>();
inventory.GetItems(items);

foreach (var item in items)
{
    // Parse type: "MyObjectBuilder_Component" → "Component"
    string typeId = item.Type.TypeId.ToString().Split('_')[1];
    string subtypeId = item.Type.SubtypeId.ToString();
    
    // Get amount (VRage.MyFixedPoint → number)
    var amount = (int)item.Amount;
    
    // Use composite key to avoid SubtypeId collisions
    string uniqueKey = $"{typeId}_{subtypeId}";
}
```

### Power Systems
```csharp
// Battery
IMyBatteryBlock battery = block as IMyBatteryBlock;
float stored = battery.CurrentStoredPower;
float capacity = battery.MaxStoredPower;
float input = battery.CurrentInput;
float output = battery.CurrentOutput;

// Producer
IMyPowerProducer producer = block as IMyPowerProducer;
float maxOutput = producer.MaxOutput;
float currentOutput = producer.CurrentOutput;

// Get resource component (detailed info)
var resourceSource = block.Components.Get<MyResourceSourceComponent>();
if (resourceSource != null)
{
    float definedOutput = resourceSource.DefinedOutput;
}
```

### CustomData Configuration
```csharp
MyIni config = new MyIni();
MyIniParseResult result;

if (!config.TryParse(terminalBlock.CustomData, out result))
{
    // Invalid INI format - handle error
    return;
}

// Read values
bool enabled = config.Get("SectionName", "Enabled").ToBoolean(true);
string text = config.Get("SectionName", "Text").ToString("Default");
int number = config.Get("SectionName", "Number").ToInt32(42);

// Write values
config.Set("SectionName", "Enabled", true);
config.SetComment("SectionName", "Enabled", "Enable this feature");
terminalBlock.CustomData = config.ToString();
```

### Drawing on LCD Screens
```csharp
using (var frame = m_surface.DrawFrame())
{
    // Create sprites
    var background = new MySprite()
    {
        Type = SpriteType.TEXTURE,
        Data = "SquareSimple",
        Position = viewport.Center,
        Size = viewport.Size,
        Color = Color.Black
    };
    frame.Add(background);
    
    var text = MySprite.CreateText(
        "Hello World",
        "White",
        Color.White,
        0.8f,
        TextAlignment.CENTER
    );
    text.Position = viewport.Center;
    frame.Add(text);
}
```

## Critical Concepts

### VRage Types
- `MyFixedPoint` - Fixed-point decimal for inventory amounts (use `.ToIntSafe()` extension)
- `MyDefinitionId` - Unique identifier for item types (`TypeId` + `SubtypeId`)
- `Vector2` - 2D coordinates for sprite positioning
- `RectangleF` - Viewport bounds for LCD surfaces

### Mod API vs Ingame API
- **Mod API** (`Sandbox.ModAPI`, `VRage.Game.ModAPI`) - Full access, used in mods
- **Ingame API** (`Sandbox.ModAPI.Ingame`) - Sandboxed, used in Programmable Blocks
- **Text Surface Scripts use Mod API** - Not sandboxed, full game access

### TypeId Collisions
Items can share `SubtypeId` but have different `TypeIds`:
- `ConsumableItem_Fruit` vs `SeedItem_Fruit` both have SubtypeId "Fruit"
- **Always use composite keys**: `$"{typeId}_{subtypeId}"`

### Dedicated Servers
Text surface scripts and rendering should not run on dedicated servers:
```csharp
if (MyAPIGateway.Utilities?.IsDedicated ?? false) return;
```

## Backward Compatibility (Public Workshop Releases)

**Critical for mods with existing users:**

1. **Never break CustomData formats** - Add new options, don't remove/rename existing
2. **Support old config keys** - Check for both old and new formats
3. **Default values are dangerous** - Always explicitly check and handle missing config
4. **Version gracefully** - Old saves must continue working
5. **Test with existing configs** - Verify old CustomData still works
6. **Document breaking changes** - Clearly note any incompatibilities in mod description

## SDK Usage

### 1. IntelliSense / API Reference
- Reference DLLs from `Bin64_Profile/` in Visual Studio project
- XML files provide full documentation in IDE
- Example: `Sandbox.Game.dll` + `Sandbox.Game.xml`

### 2. Decompilation (Source Study)
- Use dnSpy or ILSpy to decompile SDK DLLs
- Study vanilla implementations:
  - Text surface scripts in `Sandbox.Game.dll`
  - Block logic and behaviors
  - Inventory systems and item handling
- Understand internal APIs and best practices

### 3. Asset Reference
- `OriginalContent/Models/` - Examine vanilla model structure
- Use as templates for custom items/blocks
- Understand LOD (Level of Detail) configurations
- Check item definitions in XML files

### 4. Testing Tools
- `Tools/VRageEditor/ModelViewer.bat` - Preview 3D models
- Verify model structure before deployment
- Test LOD transitions

## Debugging Patterns

### Logging
```csharp
MyLog.Default.WriteLine($"[ModName] Debug message: {variable}");
// Logs appear in SpaceEngineers.log
```

### Common Errors
1. **Null reference on block access** - Always check `as` cast results
2. **Type collisions** - Use composite keys for dictionaries
3. **DetailedInfo parsing fails** - Wrap in try/catch, handle format changes
4. **Subgrid performance** - Cache subgrid scans, update infrequently
5. **Position calculation errors** - Account for viewport offset in scrolling

### Testing Checklist
- Test in Creative and Survival modes
- Test with various LCD sizes (corner LCD vs standard)
- Test with subgrids and complex ships
- Test with hidden/filtered categories
- Test CustomData edge cases (missing keys, invalid values)
- Test with clean game (no other mods)

## File Structure Template

```
ModName/
├── Data/
│   ├── Scripts/
│   │   └── ModNamespace/
│   │       ├── MyScript.cs
│   │       └── ...
│   ├── CubeBlocks/
│   │   └── MyBlock.sbc
│   ├── TextSurfaceScripts.sbc  (for LCD scripts)
│   └── ...sbc files
├── Models/
│   ├── Cubes/
│   └── Items/
├── Textures/
├── metadata.mod
└── thumb.png
```

## Workshop Publishing

### metadata.mod Format
```xml
<?xml version="1.0"?>
<ModMetadata xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <ModVersion>1.0</ModVersion>
</ModMetadata>
```

### Publishing Checklist
1. Test with clean game install (no other mods)
2. Verify all file paths are correct
3. Check performance with many blocks/items
4. Include clear usage instructions
5. Document CustomData configuration options
6. Test backward compatibility with previous version
7. Create clear thumbnail (thumb.png)
8. Write detailed description with examples

## Common Namespaces

```csharp
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
using System.Collections.Generic;
using System.Text;
```

## Performance Best Practices

1. **Cache expensive operations** - Store subgrid scans, block lists
2. **Update frequency** - Don't scan every frame, use tick counters
3. **Minimize LINQ** - Use for loops for performance-critical code
4. **Null checks** - Always verify block/component existence
5. **String operations** - Use StringBuilder for concatenation
6. **Sprite count** - Minimize sprites per frame on LCD displays

## Next Steps

When user asks for mod assistance:
1. Identify mod type (block, item, script, weapon, etc.)
2. Reference appropriate SDK DLLs for API access
3. Check [CLAUDE.md](../../../CLAUDE.md) for project-specific patterns
4. Use SDK decompilation for implementation examples
5. Apply backward compatibility principles for Workshop releases
6. Test thoroughly before publishing

## Additional Resources

For project-specific patterns, known issues, and detailed examples, see [CLAUDE.md](../../../CLAUDE.md) in the space-engineers-mods folder.
