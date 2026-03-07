# Known Issues & Gotchas

Common pitfalls, quirks, and solutions when modding Space Engineers.

## Type System Issues

### Item Type Collisions
**Problem:** Items can share `SubtypeId` but have different `TypeId`, causing dictionary collisions.

**Example:**
- `ConsumableItem_Fruit` (cooked food)
- `SeedItem_Fruit` (seeds for farming)
- Both have SubtypeId: `"Fruit"`

**Solution:** Always use composite keys in dictionaries:
```csharp
// ❌ WRONG - Causes collisions
string key = item.Type.SubtypeId.ToString();
dict[key] = value;

// ✅ CORRECT - Unique keys
string typeId = item.Type.TypeId.ToString().Split('_')[1]; // "ConsumableItem" → "ConsumableItem"
string subtypeId = item.Type.SubtypeId.ToString();
string key = $"{typeId}_{subtypeId}"; // "ConsumableItem_Fruit" or "SeedItem_Fruit"
dict[key] = value;
```

**Affected Items:**
- Fruit / Fruit Seeds
- Vegetable / Vegetable Seeds  
- Mushroom / Mushroom Spores

### VRage.MyFixedPoint
**Problem:** Inventory amounts are `MyFixedPoint`, not `int` or `float`.

**Solution:** Convert properly:
```csharp
MyInventoryItem item = ...;

// ❌ WRONG - Won't compile
int amount = item.Amount;

// ✅ CORRECT
int amount = (int)item.Amount;
// OR
int amount = item.Amount.ToIntSafe(); // Extension method
```

## Scrolling & Display Issues

### Multi-Category Scrolling
**Problem:** When multiple categories are drawn on same screen, each needs to know remaining space, not total space.

**Example:** Screen with Battery Summary + Reactor List + Solar List
- If header + battery summary uses 200px
- Reactor list starts at Y=200
- But calculates available space from Y=0
- Result: Content overflows screen

**Solution:** Calculate remaining space from current position:
```csharp
// ❌ WRONG - Total screen space
float availableHeight = screenHeight - viewPortOffsetY;
int availableLines = (int)(availableHeight / lineHeight);

// ✅ CORRECT - Remaining space from current position
float currentY = position.Y - surfaceData.viewPortOffsetY;
float remainingHeight = screenHeight - currentY;
int availableLines = Math.Max(1, (int)(remainingHeight / lineHeight));
```

### Hidden Categories
**Problem:** When categories can be hidden via config, visible categories must account for space used by visible items above them.

**Solution:** Position-based calculation naturally handles this - each category starts where previous ended.

## Performance Issues

### Subgrid Scanning
**Problem:** Scanning all subgrids every frame is extremely expensive.

**Solution:** Cache subgrid scans and update infrequently:
```csharp
private List<IMyTerminalBlock> cachedSubgridBlocks = new List<IMyTerminalBlock>();
private int subgridScanTick = 0;
private const int SUBGRID_SCAN_FREQUENCY = 600; // Every 10 seconds

void Update()
{
    // Always get main grid blocks (cheap)
    var mainBlocks = GetMainGridBlocks();
    
    // Conditionally update subgrid cache
    if (scanSubgrids)
    {
        subgridScanTick++;
        if (subgridScanTick >= SUBGRID_SCAN_FREQUENCY)
        {
            cachedSubgridBlocks.Clear();
            cachedSubgridBlocks = GetSubgridBlocks();
            subgridScanTick = 0;
        }
    }
    else
    {
        cachedSubgridBlocks.Clear();
    }
    
    // Merge main + cached subgrids
    var allBlocks = mainBlocks.Concat(cachedSubgridBlocks);
}
```

### LINQ Overhead
**Problem:** LINQ queries can be expensive in tight loops.

**Solution:** Use for loops for performance-critical code:
```csharp
// ❌ SLOW - Creates enumerators
var result = blocks.Where(b => b.IsFunctional).Select(b => b.CustomName);

// ✅ FAST - Direct iteration
List<string> result = new List<string>();
for (int i = 0; i < blocks.Count; i++)
{
    if (blocks[i].IsFunctional)
        result.Add(blocks[i].CustomName);
}
```

## Block Component Issues

### Missing Components
**Problem:** Not all blocks have all components. Accessing missing component returns null.

**Solution:** Always null-check components:
```csharp
var resourceSource = block.Components.Get<MyResourceSourceComponent>();
if (resourceSource == null)
{
    // Block doesn't have this component
    return;
}

// Safe to use
float output = resourceSource.DefinedOutput;
```

### DetailedInfo Parsing
**Problem:** `block.DetailedInfo` format can change between game versions or mods.

**Solution:** Wrap parsing in try/catch and provide fallbacks:
```csharp
try
{
    string[] lines = block.DetailedInfo.Split('\n');
    foreach (var line in lines)
    {
        if (line.Contains("Max Output:"))
        {
            // Parse with care
            string value = line.Split(':')[1].Trim();
        }
    }
}
catch (Exception ex)
{
    MyLog.Default.WriteLine($"[Mod] DetailedInfo parse failed: {ex.Message}");
    // Use fallback method (component access, etc.)
}
```

## Configuration Issues

### Default Values are Dangerous
**Problem:** Relying on default parameter values can cause runtime failures.

**Example:**
```csharp
// ❌ FAILS at runtime - Missing required parameters
var config = new BlockConfig()
{
    resource = "message",
    operation = "post",
    text = "Hello"
};

// ✅ WORKS - All parameters explicit
var config = new BlockConfig()
{
    resource = "message",
    operation = "post",
    select = "channel",
    channelId = "C123",
    text = "Hello"
};
```

**Solution:** Always explicitly set all parameters that control behavior.

### CustomData Backward Compatibility
**Problem:** Changing CustomData format breaks existing user configurations.

**Solution:** Support both old and new formats:
```csharp
MyIni config = new MyIni();
config.TryParse(customData, out _);

// Check for new format first
bool enabled = config.Get("Section", "NewKey").ToBoolean();

// Fall back to old format
if (!config.ContainsKey("Section", "NewKey"))
{
    enabled = config.Get("Section", "OldKey").ToBoolean(true);
}
```

## Server & Multiplayer Issues

### Dedicated Server Rendering
**Problem:** Text surface scripts run on dedicated servers, wasting CPU on rendering that nobody sees.

**Solution:** Always check for dedicated server:
```csharp
public override void Run()
{
    // Skip all rendering on dedicated servers
    if (MyAPIGateway.Utilities?.IsDedicated ?? false)
        return;
    
    // Normal rendering
}
```

### World Access
**Problem:** `MyAPIGateway` may be null during initialization.

**Solution:** Null-check before use:
```csharp
if (MyAPIGateway.Utilities?.IsDedicated ?? false)
    return;

// Safe to use MyAPIGateway now
var session = MyAPIGateway.Session;
```

## Grid & Block Relationships

### Block Grid Access
**Problem:** Blocks can be on different grids (pistons, rotors, connectors).

**Solution:** Always verify grid relationships:
```csharp
var controlBlock = m_surface.Entity as IMyTerminalBlock;
var controlGrid = controlBlock.CubeGrid;

foreach (var block in blocks)
{
    if (block.CubeGrid.EntityId == controlGrid.EntityId)
    {
        // Same grid
    }
    else
    {
        // Different grid (subgrid)
    }
}
```

### Null Physics
**Problem:** Static grids (stations) may have null Physics component.

**Solution:** Check before accessing:
```csharp
IMyCubeGrid grid = ...;

if (grid.Physics != null)
{
    float mass = grid.Physics.Mass;
}
else
{
    // Static grid, no physics
}
```

## String Operations

### StringBuilder for Performance
**Problem:** String concatenation in loops creates garbage.

**Solution:** Use StringBuilder:
```csharp
// ❌ SLOW
string result = "";
foreach (var item in items)
{
    result += item.ToString() + "\n";
}

// ✅ FAST
StringBuilder sb = new StringBuilder();
foreach (var item in items)
{
    sb.AppendLine(item.ToString());
}
string result = sb.ToString();
```

## Registration Issues

### Text Surface Script Registration
**Problem:** Script doesn't appear in LCD screen list.

**Cause:** Missing or incorrect `TextSurfaceScripts.sbc` file.

**Solution:** Ensure correct registration:
```xml
<?xml version="1.0"?>
<Definitions xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <TextSurfaceScripts>
    <TextSurfaceScript>
      <Id>
        <TypeId>MyObjectBuilder_TextSurfaceScript</TypeId>
        <SubtypeId>TSS_YourScriptName</SubtypeId>
      </Id>
      <Script>YourNamespace.YourClassName</Script>
    </TextSurfaceScript>
  </TextSurfaceScripts>
</Definitions>
```

**Common mistakes:**
- SubtypeId doesn't match convention (usually `TSS_Name`)
- Script path is incorrect (must be `Namespace.ClassName`)
- File not in `Data/` folder

## Testing Edge Cases

Always test these scenarios:
1. ✅ Empty inventories (no divide by zero)
2. ✅ Hidden categories (position calculations)
3. ✅ Corner LCD vs standard LCD (different sizes)
4. ✅ No blocks of requested type found
5. ✅ Subgrids disconnected during operation
6. ✅ Custom data missing or malformed
7. ✅ Extremely large values (overflow)
8. ✅ Clean game install (no helper mods)
9. ✅ Multiplayer/dedicated server
10. ✅ Old save games (backward compatibility)
