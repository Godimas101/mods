using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Utils;
using VRageMath;

namespace MahrianeIndustries.LCDInfo
{
    [MyTextSurfaceScript("LCDInfoScreenOresSummary", "$IOS LCD - Ores")]
    public class LCDOresSummaryInfo : MyTextSurfaceScriptBase
    {
        MyIni config = new MyIni();

        public static string CONFIG_SECTION_ID = "SettingsOresSummary";

        // The IMyInventoryItem.Type.TypeIds this Script is looking for.
        List<string> item_types = new List<string>
        {
            "Ore"
        };

        void TryCreateSurfaceData()
        {
            if (surfaceData != null)
                return;

            compactMode = mySurface.SurfaceSize.X / mySurface.SurfaceSize.Y > 4;
            var textSize = compactMode ? .6f : .4f;

            // Initialize surface settings
            surfaceData = new SurfaceDrawer.SurfaceData
            {
                surface = mySurface,
                textSize = textSize,
                titleOffset = 280,
                ratioOffset = 82,
                viewPortOffsetX = compactMode ? 10 : 20,
                viewPortOffsetY = compactMode ? 5 : 10,
                newLine = new Vector2(0, 30 * textSize),
                showHeader = true,
                showSummary = true,
                showMissing = false,
                showRatio = false,
                showBars = true,
                showSubgrids = false,
                showDocked = false,
                showScrap = false,
                useColors = true
            };
        }

        void CreateConfig()
        {
            TryCreateSurfaceData();

            config.Clear();

            // Build custom formatted config with section headers
            StringBuilder sb = new StringBuilder();
            
            // Preserve existing CustomData from other sections only (not our config section)
            string existing = myTerminalBlock.CustomData ?? "";
            if (!string.IsNullOrWhiteSpace(existing))
            {
                // Manually remove our section while preserving all other content (including comments)
                string[] lines = existing.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                bool inOurSection = false;
                bool addedOtherContent = false;
                
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    string trimmed = line.Trim();
                    
                    // Check if this is a section header
                    if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                    {
                        string sectionName = trimmed.Substring(1, trimmed.Length - 2);
                        inOurSection = (sectionName == CONFIG_SECTION_ID);
                        
                        // If not our section, add it
                        if (!inOurSection)
                        {
                            if (addedOtherContent) sb.AppendLine(); // blank line before new section
                            sb.AppendLine(line);
                            addedOtherContent = true;
                        }
                    }
                    else if (!inOurSection && addedOtherContent)
                    {
                        // Add any line that's not in our section
                        sb.AppendLine(line);
                    }
                    else if (!inOurSection && !string.IsNullOrWhiteSpace(trimmed))
                    {
                        // First non-empty line before any section
                        sb.AppendLine(line);
                        addedOtherContent = true;
                    }
                }
                
                // Add blank line separator if we preserved other content
                if (addedOtherContent)
                    sb.AppendLine();
            }

            sb.AppendLine($"[{CONFIG_SECTION_ID}]");
            sb.AppendLine();
            sb.AppendLine("; [ ORES - GENERAL OPTIONS ]");
            sb.AppendLine("SearchId=*");
            sb.AppendLine("; Block name filter: Use '*' for all, or text to match block names (case-insensitive substring match)");
            sb.AppendLine("; Examples: 'Cargo' matches 'Main Cargo', 'Engineering,Medical' matches blocks containing either word");
            sb.AppendLine("ExcludeIds=");
            sb.AppendLine("; Exclude blocks containing these words (comma-separated, case-insensitive)");
            sb.AppendLine("; Example: 'Airlock,Backup' excludes blocks with 'Airlock' or 'Backup' in their names");
            ConfigHelpers.AppendShowHeaderConfig(sb, surfaceData.showHeader);
            ConfigHelpers.AppendShowSummaryConfig(sb, surfaceData.showSummary);
            ConfigHelpers.AppendShowMissingConfig(sb, surfaceData.showMissing);
            ConfigHelpers.AppendShowRatioConfig(sb, surfaceData.showRatio);
            ConfigHelpers.AppendShowBarsConfig(sb, surfaceData.showBars);
            ConfigHelpers.AppendShowSubgridsConfig(sb, surfaceData.showSubgrids);
            ConfigHelpers.AppendSubgridUpdateFrequencyConfig(sb, surfaceData.subgridUpdateFrequency);
            ConfigHelpers.AppendShowDockedConfig(sb, surfaceData.showDocked);
            ConfigHelpers.AppendUseColorsConfig(sb, surfaceData.useColors);

            sb.AppendLine();
            ConfigHelpers.AppendScrollingConfig(sb);

            sb.AppendLine("; [ ORES - LAYOUT OPTIONS ]");
            sb.AppendLine($"TextSize={surfaceData.textSize}");
            sb.AppendLine($"ViewPortOffsetX={surfaceData.viewPortOffsetX}");
            sb.AppendLine($"ViewPortOffsetY={surfaceData.viewPortOffsetY}");
            sb.AppendLine($"TitleFieldWidth={surfaceData.titleOffset}");
            sb.AppendLine($"RatioFieldWidth={surfaceData.ratioOffset}");

            sb.AppendLine();
            sb.AppendLine("; [ ORES - SCREEN OPTIONS ]");
            sb.AppendLine($"ShowScrap={surfaceData.showScrap}");
            sb.AppendLine($"UseSubtypeId={surfaceData.useSubtypeId}");

            sb.AppendLine();
            sb.AppendLine("; [ ORES - ITEM THRESHOLDS ]");

            CreateCargoItemDefinitionList();

            foreach (CargoItemDefinition itemDefinition in itemDefinitions)
            {
                sb.AppendLine($"{itemDefinition.subtypeId}={itemDefinition.minAmount}");
            }

            foreach (CargoItemDefinition itemDefinition in unknownItemDefinitions)
            {
                sb.AppendLine($"{itemDefinition.subtypeId}={itemDefinition.minAmount}");
            }

            sb.AppendLine();

            myTerminalBlock.CustomData = sb.ToString();
        }

        void LoadConfig()
        {
            try
            {
                configError = false;
                MyIniParseResult result;
                TryCreateSurfaceData();

                if (config.TryParse(myTerminalBlock.CustomData, CONFIG_SECTION_ID, out result))
                {

                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowHeader", ref surfaceData.showHeader, ref configError);
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowSummary", ref surfaceData.showSummary, ref configError);
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowMissing", ref surfaceData.showMissing, ref configError);
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowRatio", ref surfaceData.showRatio, ref configError);
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowBars", ref surfaceData.showBars, ref configError);
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowSubgrids", ref surfaceData.showSubgrids, ref configError);
                    if (config.ContainsKey(CONFIG_SECTION_ID, "SubgridUpdateFrequency")) surfaceData.subgridUpdateFrequency = config.Get(CONFIG_SECTION_ID, "SubgridUpdateFrequency").ToInt32();
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowDocked", ref surfaceData.showDocked, ref configError);

                    if (config.ContainsKey(CONFIG_SECTION_ID, "TextSize"))
                        surfaceData.textSize = config.Get(CONFIG_SECTION_ID, "TextSize").ToSingle(defaultValue: 1.0f);
                    else
                        configError = true;

                    MahUtillities.TryGetConfigFloat(config, CONFIG_SECTION_ID, "TitleFieldWidth", ref surfaceData.titleOffset, ref configError);
                    MahUtillities.TryGetConfigFloat(config, CONFIG_SECTION_ID, "RatioFieldWidth", ref surfaceData.ratioOffset, ref configError);
                    MahUtillities.TryGetConfigFloat(config, CONFIG_SECTION_ID, "ViewPortOffsetX", ref surfaceData.viewPortOffsetX, ref configError);
                    MahUtillities.TryGetConfigFloat(config, CONFIG_SECTION_ID, "ViewPortOffsetY", ref surfaceData.viewPortOffsetY, ref configError);

                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "UseColors", ref surfaceData.useColors, ref configError);
                    
                    // UseSubtypeId is optional for backward compatibility
                    if (config.ContainsKey(CONFIG_SECTION_ID, "UseSubtypeId"))
                        surfaceData.useSubtypeId = config.Get(CONFIG_SECTION_ID, "UseSubtypeId").ToBoolean();

                    // Optional: Show/Hide Scrap (default false)
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowScrap"))
                        surfaceData.showScrap = config.Get(CONFIG_SECTION_ID, "ShowScrap").ToBoolean();

                    // Scrolling options (optional; default false/60/1)
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ToggleScroll"))
                        toggleScroll = config.Get(CONFIG_SECTION_ID, "ToggleScroll").ToBoolean(false);
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ReverseDirection"))
                        reverseDirection = config.Get(CONFIG_SECTION_ID, "ReverseDirection").ToBoolean(false);
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ScrollSpeed"))
                        scrollSpeed = Math.Max(1, config.Get(CONFIG_SECTION_ID, "ScrollSpeed").ToInt32(60));
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ScrollLines"))
                        scrollLines = Math.Max(1, config.Get(CONFIG_SECTION_ID, "ScrollLines").ToInt32(1));

                    surfaceData.newLine = new Vector2(0, 30 * surfaceData.textSize);

                    if (config.ContainsKey(CONFIG_SECTION_ID, "SearchId"))
                    {
                        searchId = config.Get(CONFIG_SECTION_ID, "SearchId").ToString();
                        searchId = string.IsNullOrWhiteSpace(searchId) ? "*" : searchId;
                    }
                    else
                        configError = true;

                    CreateExcludeIdsList();

                    // Is Corner LCD?
                    if (compactMode)
                    {
                        surfaceData.showHeader = true;
                        surfaceData.showSummary = true;
                        surfaceData.textSize = 0.6f;
                        surfaceData.titleOffset = 200;
                        surfaceData.viewPortOffsetX = 10;
                        surfaceData.viewPortOffsetY = 5;
                        surfaceData.newLine = new Vector2(0, 30 * surfaceData.textSize);
                    }
                }
                else
                {
                    MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDOresSummaryInfo: Config Syntax error at Line {result}");
                }

                CreateCargoItemDefinitionList();

                // Check Ores config
                foreach (CargoItemDefinition definition in itemDefinitions)
                {
                    if (config.ContainsKey(CONFIG_SECTION_ID, definition.subtypeId))
                        definition.minAmount = config.Get(CONFIG_SECTION_ID, definition.subtypeId).ToInt32();
                }

                // Check unknownOres config
                foreach (CargoItemDefinition definition in unknownItemDefinitions)
                {
                    if (config.ContainsKey(CONFIG_SECTION_ID, definition.subtypeId))
                        definition.minAmount = config.Get(CONFIG_SECTION_ID, definition.subtypeId).ToInt32();
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDOresSummaryInfo: Caught Exception while loading config: {e.ToString()}");
            }
        }

        void CreateExcludeIdsList()
        {
            if (!config.ContainsKey(CONFIG_SECTION_ID, "ExcludeIds")) return;

            string[] exclude = config.Get(CONFIG_SECTION_ID, "ExcludeIds").ToString().Split(',');
            excludeIds.Clear();
            minVisibleAmount = 0;

            foreach (string s in exclude)
            {
                string t = s.Trim();

                if (t.Contains("<"))
                {
                    t = t.Replace("<", "").Trim();
                    int.TryParse(t, out minVisibleAmount);
                    continue;
                }

                if (String.IsNullOrEmpty(t) || t == "*" || t == "" || t.Length < 3) continue;

                excludeIds.Add(t);
            }
        }

        void CreateCargoItemDefinitionList()
        {
            itemDefinitions.Clear();

            foreach (CargoItemDefinition definition in MahDefinitions.cargoItemDefinitions)
            {
                if (item_types.Contains(definition.typeId))
                {
                    int minAmount = config.ContainsKey(CONFIG_SECTION_ID, definition.subtypeId) ? (int)config.Get(CONFIG_SECTION_ID, definition.subtypeId).ToInt64() : definition.minAmount;
                    itemDefinitions.Add(new CargoItemDefinition { typeId = definition.typeId, subtypeId = definition.subtypeId, displayName = definition.displayName, volume = definition.volume, minAmount = minAmount, sortId = definition.sortId });
                }
            }

            foreach (CargoItemDefinition definition in unknownItemDefinitions)
            {
                if (item_types.Contains(definition.typeId))
                {
                    int minAmount = config.ContainsKey(CONFIG_SECTION_ID, definition.subtypeId) ? (int)config.Get(CONFIG_SECTION_ID, definition.subtypeId).ToInt64() : definition.minAmount;
                    itemDefinitions.Add(new CargoItemDefinition { typeId = definition.typeId, subtypeId = definition.subtypeId, displayName = definition.displayName, volume = definition.volume, minAmount = minAmount, sortId = definition.sortId });
                }
            }
        }

        IMyTextSurface mySurface;
        IMyTerminalBlock myTerminalBlock;

        List<string> excludeIds = new List<string>();
        List<CargoItemDefinition> itemDefinitions = new List<CargoItemDefinition>();
        List<CargoItemDefinition> unknownItemDefinitions = new List<CargoItemDefinition>();
        List<IMyInventory> inventories = new List<IMyInventory>();
        List<IMyInventory> subgridInventories = new List<IMyInventory>();  // Cached subgrid inventories
        List<VRage.Game.ModAPI.Ingame.MyInventoryItem> inventoryItems = new List<VRage.Game.ModAPI.Ingame.MyInventoryItem>();

        Dictionary<string, CargoItemType> cargo = new Dictionary<string, CargoItemType>();

        VRage.Collections.DictionaryValuesReader<MyDefinitionId, MyDefinitionBase> myDefinitions;
        MyDefinitionId myDefinitionId;
        SurfaceDrawer.SurfaceData surfaceData;
        string searchId = "";
        string gridId = "Unknown grid";
        int minVisibleAmount = 0;
        int subgridScanTick = 0;
        bool configError = false;
        bool compactMode = false;
        bool isStation = false;
        Sandbox.ModAPI.Ingame.MyShipMass gridMass;

        // Scrolling state
        bool toggleScroll = false;
        bool reverseDirection = false;
        int scrollSpeed = 5;
        int scrollLines = 1;
        int scrollOffset = 0;
        int ticksSinceLastScroll = 0;

        public LCDOresSummaryInfo(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            mySurface = surface;
            myTerminalBlock = block as IMyTerminalBlock;
        }

        public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update10;

        public override void Dispose()
        {

        }

        public override void Run()
        {
            // Prevent execution on a dedicated server to avoid server-side load
            if (Sandbox.ModAPI.MyAPIGateway.Utilities?.IsDedicated ?? false)
                return;

            MahDefinitions.LoadExternalItems();
            if (myTerminalBlock.CustomData.Length <= 0 || !myTerminalBlock.CustomData.Contains(CONFIG_SECTION_ID))
                CreateConfig();

            LoadConfig();

            UpdateInventories();
            UpdateContents();

            // Update scroll offset if scrolling is enabled
            if (toggleScroll)
            {
                ticksSinceLastScroll++;
                if (ticksSinceLastScroll >= scrollSpeed)
                {
                    ticksSinceLastScroll = 0;
                    if (reverseDirection)
                        scrollOffset -= scrollLines;
                    else
                        scrollOffset += scrollLines;
                    
                    // Scroll offset will wrap around in the draw methods based on actual item count
                }
            }
            else
            {
                // Reset scroll when disabled
                scrollOffset = 0;
                ticksSinceLastScroll = 0;
            }

            var myFrame = mySurface.DrawFrame();
            var myViewport = new RectangleF((mySurface.TextureSize - mySurface.SurfaceSize) / 2f, mySurface.SurfaceSize);
            var myPosition = new Vector2(surfaceData.viewPortOffsetX, surfaceData.viewPortOffsetY) + myViewport.Position;
            myDefinitions = MyDefinitionManager.Static.GetAllDefinitions();

            if (configError)
                SurfaceDrawer.DrawErrorSprite(ref myFrame, surfaceData, $"<< Config error. Please Delete CustomData >>", Color.Orange);
            else
                DrawMainSprite(ref myFrame, ref myPosition);

            myFrame.Dispose();
        }

        void UpdateInventories()
        {
            try
            {
                var myCubeGrid = myTerminalBlock.CubeGrid as MyCubeGrid;
                if (myCubeGrid == null) return;

                IMyCubeGrid cubeGrid = myCubeGrid as IMyCubeGrid;
                isStation = cubeGrid.IsStatic;
                gridId = cubeGrid.CustomName;

                // Determine if we should scan subgrids/docked on this tick
                bool scanSubgrids = false;
                if (surfaceData.showSubgrids || surfaceData.showDocked)
                {
                    subgridScanTick++;
                    if (subgridScanTick >= surfaceData.subgridUpdateFrequency / 10)  // Divide by 10 for Update10 timing
                    {
                        subgridScanTick = 0;
                        scanSubgrids = true;
                    }
                }

                // Always scan main grid inventories (instant updates)
                var mainInventories = MahUtillities.GetInventories(myCubeGrid, searchId, excludeIds, ref gridMass, false, false);

                // Periodically update subgrid/docked inventory cache
                if (scanSubgrids)
                {
                    var allInventories = MahUtillities.GetInventories(myCubeGrid, searchId, excludeIds, ref gridMass, surfaceData.showSubgrids, surfaceData.showDocked);
                    subgridInventories.Clear();
                    foreach (var inventory in allInventories)
                    {
                        if (!mainInventories.Contains(inventory))
                            subgridInventories.Add(inventory);
                    }
                }

                // Merge main (fresh) and subgrid (cached) inventories
                inventories.Clear();
                inventories.AddRange(mainInventories);
                inventories.AddRange(subgridInventories);
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDOresSummaryInfo: Caught Exception while updating inventories: {e.ToString()}");
            }
        }

        void UpdateContents()
        {
            try
            {
                cargo.Clear();
                unknownItemDefinitions.Clear();

                foreach (var inventory in inventories)
                {
                    if (inventory == null) continue;
                    if (inventory.ItemCount == 0)
                        continue;

                    inventoryItems.Clear();
                    inventory.GetItems(inventoryItems);

                    // Use centralized sorting utility (currently SubtypeId, extensible for future modes)
                    foreach (var item in MahSorting.SortItems(inventoryItems, MahSorting.ItemSortMode.SubtypeId))
                    {
                        if (item == null) continue;

                        var typeId = item.Type.TypeId.Split('_')[1];
                        var subtypeId = item.Type.SubtypeId;
                        var currentAmount = item.Amount.ToIntSafe();

                        if (item_types.Contains(typeId))
                        {
                            if (!cargo.ContainsKey(subtypeId))
                            {
                                cargo.Add(subtypeId, new CargoItemType { item = item, amount = currentAmount });
                                CargoItemDefinition itemDefinition = FindCargoItemDefinition(typeId, subtypeId);

                                if (itemDefinition == null)
                                {
                                    itemDefinition = new CargoItemDefinition();
                                    itemDefinition.typeId = typeId;
                                    itemDefinition.subtypeId = subtypeId;
                                    itemDefinition.displayName = subtypeId.Length >= 15 ? subtypeId.Substring(0, 15) : subtypeId;
                                    itemDefinition.volume = .1f;
                                    itemDefinition.minAmount = 1000;
                                    itemDefinition.sortId = "misc"; // default category

                                    itemDefinitions.Add(itemDefinition);
                                    unknownItemDefinitions.Add(itemDefinition);
                                }

                                cargo[subtypeId].definition = itemDefinition;
                            }
                            else
                            {
                                cargo[subtypeId].amount += currentAmount;
                            }

                            cargo[subtypeId].item = item;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDOresSummaryInfo: Caught Exception while updating contents: {e.ToString()}");
            }
        }

        void DrawMainSprite(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            try
            {
                double total = 0;
                double current = 0;

                // Total Cargo
                foreach (IMyInventory inventory in inventories)
                {
                    if (inventory == null) continue;

                    total += (double)inventory.MaxVolume;
                    current += (double)inventory.CurrentVolume;
                }

                if (surfaceData.showHeader)
                {
                    SurfaceDrawer.DrawHeader(ref frame, ref position, surfaceData, $"Ores [{cargo.Count}/{itemDefinitions.Count}/{unknownItemDefinitions.Count}/{inventories.Count}]:");
                    if (compactMode) position -= 2 * surfaceData.newLine;
                }

                if (surfaceData.showSummary)
                {
                    if (!compactMode)
                    {
                        SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Summary [{(searchId == "*" ? "All" : searchId)} -{excludeIds.Count}] >> ({inventories.Count})", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                        position += surfaceData.newLine;
                    }
                    position += surfaceData.newLine;
                    SurfaceDrawer.DrawBar(ref frame, ref position, surfaceData, "Total", current, total, Unit.Percent, false, false);

                    // Total Volume
                    double volume = 0;

                    foreach (var item in cargo)
                    {
                        volume += item.Value.amount * item.Value.definition.volume;
                    }

                    volume /= 1000;
                    SurfaceDrawer.DrawBar(ref frame, ref position, surfaceData, "Ores", volume, current, Unit.Percent, false, true);
                    position += surfaceData.newLine;
                }

                // If this is a corner LCD, no more data will be visible.
                if (compactMode) return;

                if (surfaceData.showMissing)
                {
                    DrawAllKnownSprite(ref frame, ref position);
                }
                else
                {
                    DrawAllAvailableSprite(ref frame, ref position);
                }

                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDOresSummaryInfo: Caught Exception while DrawMainSprite: {e.ToString()}");
            }
        }

        void DrawAllKnownSprite(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            try
            {
                // Calculate header position and data start position
                Vector2 headerPosition = position;
                
                SurfaceDrawer.WriteTextSprite(ref frame, headerPosition, surfaceData, $"Id [Ores]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                SurfaceDrawer.WriteTextSprite(ref frame, headerPosition, surfaceData, "Available", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);

                headerPosition += surfaceData.newLine; // Blank line after header
                Vector2 dataStartPosition = headerPosition;

                // Collect all items into a single list for scrolling
                List<CargoItemDefinition> allItems = new List<CargoItemDefinition>();

                // Sort items by either subtypeId or displayName based on useSubtypeId setting
                var sortedItems = surfaceData.useSubtypeId 
                    ? itemDefinitions.OrderBy(d => d.subtypeId) 
                    : itemDefinitions.OrderBy(d => d.displayName);

                foreach (var itemDefinition in sortedItems)
                {
                    if (itemDefinition == null) continue;
                    if (IgnoreDefinition(itemDefinition)) continue;
                    if (!surfaceData.showScrap && itemDefinition.subtypeId.Equals("Scrap", StringComparison.OrdinalIgnoreCase)) continue;
                    
                    allItems.Add(itemDefinition);
                }

                // Sort unknown items by either subtypeId or displayName based on useSubtypeId setting
                var sortedUnknownItems = surfaceData.useSubtypeId 
                    ? unknownItemDefinitions.OrderBy(d => d.subtypeId) 
                    : unknownItemDefinitions.OrderBy(d => d.displayName);

                foreach (var itemDefinition in sortedUnknownItems)
                {
                    if (itemDefinition == null) continue;
                    if (IgnoreDefinition(itemDefinition)) continue;
                    if (!surfaceData.showScrap && itemDefinition.subtypeId.Equals("Scrap", StringComparison.OrdinalIgnoreCase)) continue;
                    
                    allItems.Add(itemDefinition);
                }

                // Calculate lines available for data (remaining screen height - header - blank line)
                // NOTE: Use remaining height (total - already used) since header/summary may have been drawn above
                float screenHeight = mySurface.SurfaceSize.Y;
                float lineHeight = 30f * surfaceData.textSize;  // Explicit float for precision
                float usedHeight = dataStartPosition.Y;  // Y position after drawing header (accounts for viewport offset + any content above)
                int totalScreenLines = (int)((screenHeight - usedHeight) / lineHeight);
                int headerLines = 0; // Header already accounted for in dataStartPosition.Y
                int availableDataLines = Math.Max(1, totalScreenLines - headerLines);

                // Apply scrolling if enabled and needed
                int totalDataLines = allItems.Count;
                int startIndex = 0;
                
                if (toggleScroll && totalDataLines > availableDataLines)
                {
                    // Normalize scroll offset to stay within bounds (use local variable)
                    int normalizedOffset = ((scrollOffset % totalDataLines) + totalDataLines) % totalDataLines;
                    startIndex = normalizedOffset;
                }

                // Draw items with scrolling/wrapping
                position = dataStartPosition;
                int linesDrawn = 0;
                
                for (int i = 0; i < totalDataLines && linesDrawn < availableDataLines; i++)
                {
                    int itemIndex = (startIndex + i) % totalDataLines;
                    var itemDefinition = allItems[itemIndex];
                    
                    string displayText = surfaceData.useSubtypeId ? itemDefinition.subtypeId : itemDefinition.displayName;

                    SurfaceDrawer.DrawItemSprite(ref frame, ref position, surfaceData,
                        itemDefinition.subtypeId,
                        displayText,
                        cargo.ContainsKey(itemDefinition.subtypeId) ? cargo[itemDefinition.subtypeId].amount : 0,
                        itemDefinition.minAmount,
                        true);
                    
                    linesDrawn++;
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDOresSummaryInfo: Caught Exception while DrawAllKnownSprite: {e.ToString()}");
            }
        }

        void DrawAllAvailableSprite(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            try
            {
                if (cargo.Count <= 0)
                {
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, "No Ores found.", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    position += surfaceData.newLine;
                    return;
                }

                // Calculate header position and data start position
                Vector2 headerPosition = position;
                
                SurfaceDrawer.WriteTextSprite(ref frame, headerPosition, surfaceData, $"Id [Ores]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                SurfaceDrawer.WriteTextSprite(ref frame, headerPosition, surfaceData, "Available", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                
                headerPosition += surfaceData.newLine; // Blank line after header
                Vector2 dataStartPosition = headerPosition;
                
                // Sort items and collect into list
                var sortedCargo = surfaceData.useSubtypeId 
                    ? MahSorting.SortCargoItems(cargo, MahSorting.ItemSortMode.SubtypeId)
                    : MahSorting.SortCargoItems(cargo, MahSorting.ItemSortMode.DisplayName);
                
                List<KeyValuePair<string, CargoItemType>> allItems = new List<KeyValuePair<string, CargoItemType>>();
                
                foreach (var item in sortedCargo)
                {
                    if (item.Value.item == null) continue;

                    var typeId = item.Value.item.Type.TypeId.Split('_')[1];
                    var subtypeId = item.Value.item.Type.SubtypeId;
                    CargoItemDefinition itemDefinition = FindCargoItemDefinition(typeId, subtypeId);

                    if (IgnoreDefinition(itemDefinition)) continue;
                    if (!surfaceData.showScrap && itemDefinition != null && itemDefinition.subtypeId.Equals("Scrap", StringComparison.OrdinalIgnoreCase)) continue;
                    if (cargo[itemDefinition.subtypeId].amount < minVisibleAmount) continue;
                    
                    allItems.Add(item);
                }

                // Calculate lines available for data (remaining screen height - header - blank line)
                // NOTE: Use remaining height (total - already used) since header/summary may have been drawn above
                float screenHeight = mySurface.SurfaceSize.Y;
                float lineHeight = 30f * surfaceData.textSize;  // Explicit float for precision
                float usedHeight = dataStartPosition.Y;  // Y position after drawing header (accounts for viewport offset + any content above)
                int totalScreenLines = (int)((screenHeight - usedHeight) / lineHeight);
                int headerLines = 0; // Header already accounted for in dataStartPosition.Y
                int availableDataLines = Math.Max(1, totalScreenLines - headerLines);

                // Apply scrolling if enabled and needed
                int totalDataLines = allItems.Count;
                int startIndex = 0;
                
                if (toggleScroll && totalDataLines > availableDataLines)
                {
                    // Normalize scroll offset to stay within bounds (use local variable)
                    int normalizedOffset = ((scrollOffset % totalDataLines) + totalDataLines) % totalDataLines;
                    startIndex = normalizedOffset;
                }

                // Draw items with scrolling/wrapping
                position = dataStartPosition;
                int linesDrawn = 0;
                
                for (int i = 0; i < totalDataLines && linesDrawn < availableDataLines; i++)
                {
                    int itemIndex = (startIndex + i) % totalDataLines;
                    var item = allItems[itemIndex];
                    
                    var typeId = item.Value.item.Type.TypeId.Split('_')[1];
                    var subtypeId = item.Value.item.Type.SubtypeId;
                    CargoItemDefinition itemDefinition = FindCargoItemDefinition(typeId, subtypeId);

                    string displayText = surfaceData.useSubtypeId ? itemDefinition.subtypeId : itemDefinition.displayName;

                    SurfaceDrawer.DrawItemSprite(ref frame, ref position, surfaceData,
                        itemDefinition.subtypeId,
                        displayText,
                        cargo.ContainsKey(itemDefinition.subtypeId) ? cargo[itemDefinition.subtypeId].amount : 0,
                        itemDefinition.minAmount,
                        true);
                    
                    linesDrawn++;
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDOresSummaryInfo: Caught Exception while DrawAllAvailableSprite: {e.ToString()}");
            }
        }

        bool IgnoreDefinition(CargoItemDefinition itemDefinition)
        {
            string sType = itemDefinition.subtypeId.ToLower();
            string dName = itemDefinition.displayName.ToLower();

            foreach (string s in excludeIds)
                if (sType.Contains(s.ToLower()) || dName.Contains(s.ToLower())) return true;

            return false;
        }

        CargoItemDefinition FindCargoItemDefinition(string typeId, string subtypeId)
        {
            // 3-tier matching strategy for robust item identification:
            // Tier 1: Exact match on both typeId and subtypeId (handles items with duplicate subtypeIds across different typeIds)
            // Tier 2: Contains match on typeId (handles "MyObjectBuilder_Ore" containing "Ore") + exact subtypeId
            // Tier 3: SubtypeId-only fallback (backward compatible, works for items with unique subtypeIds)
            
            foreach (CargoItemDefinition definition in MahDefinitions.OrderedCargoItems(itemDefinitions))
            {
                // Tier 1: Exact match
                if (definition.typeId == typeId && definition.subtypeId == subtypeId) return definition;
                
                // Tier 2: Contains match (handles MyObjectBuilder_ prefix variations)
                if (typeId.Contains(definition.typeId) && definition.subtypeId == subtypeId) return definition;
                
                // Tier 3: SubtypeId fallback (backward compatible)
                if (definition.subtypeId == subtypeId) return definition;
            }

            foreach (CargoItemDefinition definition in unknownItemDefinitions)
            {
                if (definition.typeId == typeId && definition.subtypeId == subtypeId) return definition;
                if (typeId.Contains(definition.typeId) && definition.subtypeId == subtypeId) return definition;
                if (definition.subtypeId == subtypeId) return definition;
            }

            return null;
        }
    }
}
