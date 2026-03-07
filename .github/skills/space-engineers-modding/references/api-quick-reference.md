# Space Engineers API Quick Reference

## Core Interfaces

### IMyTerminalBlock
Base interface for all controllable blocks.

**Properties:**
- `CustomName` - Block display name
- `CustomData` - User-editable data string (INI format)
- `CubeGrid` - Grid the block belongs to
- `EntityId` - Unique identifier
- `WorldPosition` - Position in world coordinates

**Methods:**
- `GetInventory(int index)` - Get block inventory
- `HasInventory` - Check if block has inventory

### IMyTextSurface
LCD screen rendering interface.

**Properties:**
- `ContentType` - `ContentType.SCRIPT` or `ContentType.TEXT_AND_IMAGE`
- `Script` - Current script name
- `SurfaceSize` - Screen resolution
- `TextureSize` - Render target size
- `ScriptBackgroundColor` - Background color
- `ScriptForegroundColor` - Foreground color

**Methods:**
- `DrawFrame()` - Begin sprite rendering (use in `using` block)
- `GetSprites()` - Available sprite list

### IMyInventory
Inventory access and manipulation.

**Properties:**
- `CurrentMass` - Current inventory mass
- `MaxVolume` - Maximum volume capacity
- `CurrentVolume` - Current volume used
- `ItemCount` - Number of item stacks

**Methods:**
- `GetItems(List<MyInventoryItem>)` - Get all items
- `CanItemsBeAdded(amount, definitionId)` - Check capacity
- `ContainItems(amount, definitionId)` - Check if item exists
- `GetItemAmount(definitionId)` - Get item quantity

### IMyCubeGrid
Grid/ship structure.

**Properties:**
- `GridSizeEnum` - Large or small grid
- `Physics` - Physics component (null if static)
- `DisplayName` - Grid name
- `WorldAABB` - Bounding box

**Methods:**
- `GetBlocks(List<IMySlimBlock>)` - Get all blocks
- `GetFatBlocks<T>(List<T>)` - Get functional blocks of type

### IMyPowerProducer
Power generation blocks (reactors, solar, wind, engines).

**Properties:**
- `MaxOutput` - Maximum power output (MW)
- `CurrentOutput` - Current power output (MW)
- `Enabled` - Whether block is enabled

### IMyBatteryBlock
Battery storage and I/O.

**Properties:**
- `CurrentStoredPower` - Energy stored (MWh)
- `MaxStoredPower` - Maximum capacity (MWh)
- `CurrentInput` - Charge rate (MW)
- `CurrentOutput` - Discharge rate (MW)
- `ChargeMode` - Auto, Recharge, or Discharge

## Common Components

### MyResourceSourceComponent
Detailed power production information.

**Access:**
```csharp
var resourceSource = block.Components.Get<MyResourceSourceComponent>();
if (resourceSource != null)
{
    float definedOutput = resourceSource.DefinedOutputByType(MyResourceDistributorComponent.ElectricityId);
}
```

## VRage Math Types

### Vector2
2D coordinates for sprite positioning.
```csharp
Vector2 position = new Vector2(100f, 50f);
Vector2 center = viewport.Center;
```

### RectangleF
Viewport bounds for LCD surfaces.
```csharp
RectangleF viewport = new RectangleF(
    (m_surface.TextureSize - m_surface.SurfaceSize) / 2f,
    m_surface.SurfaceSize
);
```

### Color
RGBA color values (VRageMath.Color).
```csharp
Color white = Color.White;
Color custom = new Color(255, 128, 0); // RGB
Color withAlpha = new Color(255, 128, 0, 200); // RGBA
```

## Sprite Types

### MySprite
Individual drawable element.

**Types:**
- `SpriteType.TEXTURE` - Textured sprite
- `SpriteType.TEXT` - Text string
- `SpriteType.CLIP_RECT` - Clipping region

**Common Textures:**
- `"SquareSimple"` - Solid rectangle
- `"Circle"` - Solid circle
- `"SemiCircle"` - Half circle
- `"SquareHollow"` - Outlined rectangle

**Text Creation:**
```csharp
var sprite = MySprite.CreateText(
    "Display Text",
    "White",          // Font name
    Color.White,      // Color
    0.8f,            // Scale
    TextAlignment.CENTER
);
sprite.Position = new Vector2(x, y);
```

## MyIni Configuration

### Reading Values
```csharp
MyIni config = new MyIni();
MyIniParseResult result;

if (!config.TryParse(customData, out result))
{
    // Handle parse error
    string error = result.ToString();
}

// Get with defaults
bool value = config.Get("Section", "Key").ToBoolean(defaultValue);
string text = config.Get("Section", "Key").ToString("default");
int number = config.Get("Section", "Key").ToInt32(0);
float decimal = config.Get("Section", "Key").ToSingle(1.0f);

// Check existence
if (config.ContainsKey("Section", "Key"))
{
    // Key exists
}

// Get all keys in section
List<MyIniKey> keys = new List<MyIniKey>();
config.GetKeys("Section", keys);
```

### Writing Values
```csharp
config.Set("Section", "Key", value);
config.SetComment("Section", "Key", "Description of this setting");
config.SetSectionComment("Section", "Section description");

// Convert back to string
string output = config.ToString();
```

## Text Surface Script Base

### MyTextSurfaceScriptBase
Base class for LCD scripts.

**Override Methods:**
- `Run()` - Called each frame while script is active
- `Dispose()` - Called when script is disposed/changed

**Available Properties:**
- `m_surface` - IMyTextSurface instance
- `m_foregroundColor` - Configured foreground color
- `m_backgroundColor` - Configured background color

**Registration:**
Required in `Data/TextSurfaceScripts.sbc`:
```xml
<TextSurfaceScript>
  <Id>
    <TypeId>MyObjectBuilder_TextSurfaceScript</TypeId>
    <SubtypeId>TSS_ScriptName</SubtypeId>
  </Id>
  <Script>Namespace.ClassName</Script>
</TextSurfaceScript>
```

## Common Patterns

### Safe Block Casting
```csharp
IMyTerminalBlock terminal = entity as IMyTerminalBlock;
if (terminal == null) return; // Not a terminal block

IMyPowerProducer producer = terminal as IMyPowerProducer;
if (producer != null)
{
    // Is a power producer
}
```

### Inventory Iteration
```csharp
foreach (var item in items)
{
    // Full type: "MyObjectBuilder_Component"
    string fullType = item.Type.TypeId.ToString();
    
    // Parse: "Component"
    string typeId = fullType.Split('_')[1];
    
    // Subtype: "SteelPlate"
    string subtypeId = item.Type.SubtypeId.ToString();
    
    // Unique key (prevents collisions)
    string key = $"{typeId}_{subtypeId}";
    
    // Amount conversion
    int amount = (int)item.Amount; // or item.Amount.ToIntSafe()
}
```

### Error Handling
```csharp
try
{
    // Parse DetailedInfo or other unpredictable data
    string[] lines = block.DetailedInfo.Split('\n');
    foreach (var line in lines)
    {
        // Parse with care - format may change
    }
}
catch (Exception ex)
{
    MyLog.Default.WriteLine($"[ModName] Error: {ex.Message}");
}
```

### Performance Optimization
```csharp
// Cache block lists
private List<IMyTerminalBlock> cachedBlocks = new List<IMyTerminalBlock>();
private int ticksSinceUpdate = 0;

void Update()
{
    ticksSinceUpdate++;
    
    if (ticksSinceUpdate >= updateFrequency)
    {
        // Refresh cache
        cachedBlocks.Clear();
        grid.GetFatBlocks(cachedBlocks);
        ticksSinceUpdate = 0;
    }
    
    // Use cached list
    foreach (var block in cachedBlocks)
    {
        // Process
    }
}
```
