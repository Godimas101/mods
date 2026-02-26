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
    [MyTextSurfaceScript("LCDInfoScreenCargoSummary", "$IOS LCD - Cargo Summary")]
    public class LCDCargoSummaryInfo : MyTextSurfaceScriptBase
    {
        MyIni config = new MyIni();

        public static string CONFIG_SECTION_ID = "SettingsCargoSummary";

        // Each known sortId gets a visibility flag. New external sortIds default to true when first encountered.
        Dictionary<string, bool> showCategory = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
        {
            { "ore", true },
            { "ingot", true },
            { "component", true },
            { "protoComponent", true },
            { "ammo", true },
            { "handAmmo", true },
            { "rifle", true },
            { "pistol", true },
            { "launcher", true },
            { "tool", true },
            { "kit", true },
            { "bottle", true },
            { "rawFood", true },
            { "cookedFood", true },
            { "drink", true },
            { "seed", true },
            { "misc", true },
        };
        // Preferred display order
        readonly string[] categoryOrder = new string[] { "ore","ingot","component","protoComponent","ammo","handAmmo","rifle","pistol","launcher","tool","kit","bottle","rawFood","cookedFood","drink","seed","misc" };

        // Friendly display names (added)
        void TryCreateSurfaceData()
        {
            if (surfaceData != null)
                return;

            compactMode = mySurface.SurfaceSize.X / mySurface.SurfaceSize.Y > 4;
            var textSize = compactMode ? .6f : .45f;

            // Extra padding so cargo bars start further right than other screens (prevents overlap with longer category titles)
            const int CargoTitleOffsetDefault = 260; // was 140 / 160

            // Initialize surface settings
            surfaceData = new SurfaceDrawer.SurfaceData
            {
                surface = mySurface,
                textSize = textSize,
                titleOffset = CargoTitleOffsetDefault,
                ratioOffset = 82,
                viewPortOffsetX = 10,
                viewPortOffsetY = compactMode ? 5 : 10,
                newLine = new Vector2(0, 30 * textSize),
                showHeader = true,
                showSummary = true,
                showMissing = false,
                showRatio = false,
                showBars = true,
                showSubgrids = false,
                showDocked = false,
                useColors = true
            };
        }
                readonly Dictionary<string, string> categoryDisplayNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "ore", "Ore" },
            { "ingot", "Ingots" },
            { "component", "Components" },
            { "protoComponent", "Prototech Components" },
            { "ammo", "Grid Ammo" },
            { "handAmmo", "Hand Weapon Ammo" },
            { "rifle", "Rifles" },
            { "pistol", "Pistols" },
            { "launcher", "Launchers" },
            { "tool", "Tools" },
            { "kit", "Personal Kits" },
            { "bottle", "Bottles" },
            { "rawFood", "Raw Food" },
            { "cookedFood", "Cooked Food" },
            { "drink", "Drinks" },
            { "seed", "Seeds" },
            { "misc", "Miscellaneous" },
        };

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
            sb.AppendLine("; [ CARGO - GENERAL OPTIONS ]");
            sb.AppendLine("SearchId=*");
            sb.AppendLine("; Block name filter: Use '*' for all, or text to match block names (case-insensitive substring match)");
            sb.AppendLine("; Examples: 'Cargo' matches 'Main Cargo', 'Engineering,Medical' matches blocks containing either word");
            sb.AppendLine("ExcludeIds=");
            sb.AppendLine("; Exclude blocks containing these words (comma-separated, case-insensitive)");
            sb.AppendLine("; Example: 'Airlock,Backup' excludes blocks with 'Airlock' or 'Backup' in their names");
            ConfigHelpers.AppendShowHeaderConfig(sb, surfaceData.showHeader);
            ConfigHelpers.AppendShowSummaryConfig(sb, surfaceData.showSummary);
            ConfigHelpers.AppendShowMissingConfig(sb, surfaceData.showMissing);
            ConfigHelpers.AppendShowBarsConfig(sb, surfaceData.showBars);
            ConfigHelpers.AppendShowSubgridsConfig(sb, surfaceData.showSubgrids);
            ConfigHelpers.AppendSubgridUpdateFrequencyConfig(sb, surfaceData.subgridUpdateFrequency);
            ConfigHelpers.AppendShowDockedConfig(sb, surfaceData.showDocked);
            ConfigHelpers.AppendUseColorsConfig(sb, surfaceData.useColors);

            sb.AppendLine();
            sb.AppendLine("; [ CARGO - LAYOUT OPTIONS ]");
            sb.AppendLine($"TextSize={surfaceData.textSize}");
            sb.AppendLine($"ViewPortOffsetX={surfaceData.viewPortOffsetX}");
            sb.AppendLine($"ViewPortOffsetY={surfaceData.viewPortOffsetY}");
            sb.AppendLine($"TitleFieldWidth={surfaceData.titleOffset}");
            sb.AppendLine($"RatioFieldWidth={surfaceData.ratioOffset}");

            sb.AppendLine();
            sb.AppendLine("; [ CARGO - SCREEN OPTIONS ]");
            foreach (var kv in showCategory)
            {
                string keyName = ToConfigKey(kv.Key);
                sb.AppendLine($"{keyName}={kv.Value}");
            }

            sb.AppendLine();

            CreateCargoItemDefinitionList();

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
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowSubgrids", ref surfaceData.showSubgrids, ref configError);
                    if (config.ContainsKey(CONFIG_SECTION_ID, "SubgridUpdateFrequency")) surfaceData.subgridUpdateFrequency = config.Get(CONFIG_SECTION_ID, "SubgridUpdateFrequency").ToInt32();
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowDocked", ref surfaceData.showDocked, ref configError);
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowMissing", ref surfaceData.showMissing, ref configError);
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowBars", ref surfaceData.showBars, ref configError);

                    if (config.ContainsKey(CONFIG_SECTION_ID, "TextSize"))
                        surfaceData.textSize = config.Get(CONFIG_SECTION_ID, "TextSize").ToSingle(defaultValue: 1.0f);
                    else
                        configError = true;

                    MahUtillities.TryGetConfigFloat(config, CONFIG_SECTION_ID, "TitleFieldWidth", ref surfaceData.titleOffset, ref configError);
                    MahUtillities.TryGetConfigFloat(config, CONFIG_SECTION_ID, "RatioFieldWidth", ref surfaceData.ratioOffset, ref configError);
                    MahUtillities.TryGetConfigFloat(config, CONFIG_SECTION_ID, "ViewPortOffsetX", ref surfaceData.viewPortOffsetX, ref configError);
                    MahUtillities.TryGetConfigFloat(config, CONFIG_SECTION_ID, "ViewPortOffsetY", ref surfaceData.viewPortOffsetY, ref configError);

                    // Load per-category visibility flags (do not mark configError if missing; treat as new migration)
                    foreach (var key in categoryOrder)
                    {
                        string cfgKey = ToConfigKey(key);
                        if (config.ContainsKey(CONFIG_SECTION_ID, cfgKey))
                        {
                            bool val = config.Get(CONFIG_SECTION_ID, cfgKey).ToBoolean();
                            showCategory[key] = val;
                        }
                    }

                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "UseColors", ref surfaceData.useColors, ref configError);

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
                        surfaceData.textSize = surfaceData.showHeader ? 0.8f : 1.2f;
                        surfaceData.titleOffset = 82;
                        surfaceData.ratioOffset = 82;
                        surfaceData.viewPortOffsetX = 10;
                        surfaceData.viewPortOffsetY = surfaceData.showHeader ? 10 : 15;
                        surfaceData.newLine = new Vector2(0, 30 * surfaceData.textSize);
                    }

                    // Enforce minimum padding for cargo bars (only non-compact) so brackets never overlap title
                    if (!compactMode && surfaceData.titleOffset < 280)
                        surfaceData.titleOffset = 280;
                }
                else
                {
                    MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDCargoSummaryInfo: Config Syntax error at Line {result}");
                }

                CreateCargoItemDefinitionList();                    

                // Check Cargo config
                foreach (CargoItemDefinition definition in MahDefinitions.OrderedCargoItems(itemDefinitions))
                {
                    if (config.ContainsKey(CONFIG_SECTION_ID, definition.subtypeId))
                        definition.minAmount = config.Get(CONFIG_SECTION_ID, definition.subtypeId).ToInt32();
                }

                // Check unknownCargo config
                foreach (CargoItemDefinition definition in unknownItemDefinitions)
                {
                    if (config.ContainsKey(CONFIG_SECTION_ID, definition.subtypeId))
                        definition.minAmount = config.Get(CONFIG_SECTION_ID, definition.subtypeId).ToInt32();
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDCargoSummaryInfo: Caught Exception while loading config: {e.ToString()}");
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
                itemDefinitions.Add(new CargoItemDefinition { typeId = definition.typeId, subtypeId = definition.subtypeId, displayName = definition.displayName, volume = definition.volume, minAmount = 0, sortId = definition.sortId });

            foreach (CargoItemDefinition definition in unknownItemDefinitions)
                itemDefinitions.Add(new CargoItemDefinition { typeId = definition.typeId, subtypeId = definition.subtypeId, displayName = definition.displayName, volume = definition.volume, minAmount = 0, sortId = definition.sortId });
        }

        IMyTextSurface mySurface;
        IMyTerminalBlock myTerminalBlock;

        List<string> excludeIds = new List<string>();
        List<CargoItemDefinition> itemDefinitions = new List<CargoItemDefinition>();
        List<CargoItemDefinition> unknownItemDefinitions = new List<CargoItemDefinition>();
        List<IMyInventory> inventories = new List<IMyInventory>();
        List<IMyInventory> subgridInventories = new List<IMyInventory>();  // Cached subgrid inventories

    // Dynamic category item storage keyed by sortId
    Dictionary<string, Dictionary<string, CargoItemType>> categoryItems = new Dictionary<string, Dictionary<string, CargoItemType>>(StringComparer.OrdinalIgnoreCase);

        VRage.Collections.DictionaryValuesReader<MyDefinitionId, MyDefinitionBase> myDefinitions;
        MyDefinitionId myDefinitionId;
        SurfaceDrawer.SurfaceData surfaceData;
        string searchId = "";
        int minVisibleAmount = 0;
        string gridId = "Unknown grid";
        int subgridScanTick = 0;
        bool configError = false;
        bool compactMode = false;
        bool isStation = false;
        Sandbox.ModAPI.Ingame.MyShipMass gridMass;

        public LCDCargoSummaryInfo(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
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
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDCargoSummaryInfo: Caught Exception while updating inventories: {e.ToString()}");
            }
        }

        void UpdateContents()
        {
            try
            {
                foreach (var bucket in categoryItems.Values)
                    bucket.Clear();
                unknownItemDefinitions.Clear();

                foreach (var inventory in inventories)
                {
                    if (inventory == null) continue;
                    if (inventory.ItemCount == 0)
                        continue;

                    List<VRage.Game.ModAPI.Ingame.MyInventoryItem> inventoryItems = new List<VRage.Game.ModAPI.Ingame.MyInventoryItem>();
                    inventory.GetItems(inventoryItems);

                    foreach (var item in inventoryItems)
                    {
                        if (item == null) continue;

                        var typeId = item.Type.TypeId.Split('_')[1];
                        var subtypeId = item.Type.SubtypeId;

                        // Determine sortId from known definitions (fallback to misc)
                        string sortId = "misc";
                        var def = FindCargoItemDefinition(typeId, subtypeId);
                        if (def != null && !string.IsNullOrWhiteSpace(def.sortId))
                            sortId = def.sortId;

                        // Ensure category bucket exists
                        Dictionary<string, CargoItemType> catDict;
                        if (!categoryItems.TryGetValue(sortId, out catDict))
                        {
                            catDict = new Dictionary<string, CargoItemType>();
                            categoryItems[sortId] = catDict;
                            if (!showCategory.ContainsKey(sortId))
                                showCategory[sortId] = true; // new external category becomes visible by default
                        }

                        AddCargoItemDefinition(item, catDict);
                    }
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDCargoSummaryInfo: Caught Exception while updating contents: {e.ToString()}");
            }
        }

        void DrawMainSprite(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            try
            {
                float total = inventories.Sum(inventory => (float)inventory.MaxVolume);
                float current = inventories.Sum(inventory => (float)inventory.CurrentVolume);

                if (surfaceData.showHeader)
                {
                    SurfaceDrawer.DrawHeader(ref frame, ref position, surfaceData, $"Cargo [{(searchId == "*" ? "All" : searchId)} -{excludeIds.Count}]");
                    if (compactMode) position -= surfaceData.newLine;
                }

                // Stationary objects have, for whatever reason, no mass calculation.
                if (!isStation && !compactMode)
                {
                    SurfaceDrawer.DrawShipMassSprite(ref frame, ref position, surfaceData, gridMass, compactMode);
                }
                
                if (surfaceData.showBars)
                {
                    // Renamed from "Total" to "Cargo" to match Ammo screen naming while retaining same ratio: current / total capacity.
                    SurfaceDrawer.DrawBar(ref frame, ref position, surfaceData, "Cargo", current, total, Unit.Percent, false, false);
                }
                else
                {
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Cargo", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{(current / total * 100).ToString("0.0")}%", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                    position += surfaceData.newLine;
                }
                position += surfaceData.newLine;

                // If this is a corner LCD, no more data will be visible.
                if (compactMode) return;

                if (surfaceData.showSummary)
                {
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Summary", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    position += surfaceData.newLine;
                
                    foreach (var cat in categoryOrder)
                    {
                        bool visible;
                        if (!showCategory.TryGetValue(cat, out visible) || !visible) continue;
                        Dictionary<string, CargoItemType> dict;
                        if (!categoryItems.TryGetValue(cat, out dict) || dict.Count == 0) continue;
                        SurfaceDrawer.DrawCargoItemBar(ref frame, ref position, surfaceData, dict, CategoryLabel(cat), total);
                    }
                    if (current <= 0)
                        SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $" - Cargo hold is empty.", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                }

                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDCargoSummaryInfo: Caught Exception while DrawMainSprite: {e.ToString()}");
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

        void AddCargoItemDefinition (VRage.Game.ModAPI.Ingame.MyInventoryItem item, Dictionary<string, CargoItemType> dict)
        {
            try
            {
                if (item == null) return;

                var typeId = item.Type.TypeId.Split('_')[1];
                var subtypeId = item.Type.SubtypeId;
                var currentAmount = item.Amount.ToIntSafe();

                if (!dict.ContainsKey(subtypeId))
                {
                    dict.Add(subtypeId, new CargoItemType { item = item, amount = currentAmount });
                    CargoItemDefinition itemDefinition = FindCargoItemDefinition(typeId, subtypeId);

                    if (itemDefinition == null)
                    {
                        itemDefinition = new CargoItemDefinition();
                        itemDefinition.typeId = typeId;
                        itemDefinition.subtypeId = subtypeId;
                        itemDefinition.displayName = subtypeId.Length >= 15 ? subtypeId.Substring(0, 15) : subtypeId;
                        itemDefinition.volume = .1f;
                        itemDefinition.minAmount = 0;
                        itemDefinition.sortId = "misc"; // default category (will be used for grouping)

                        itemDefinitions.Add(itemDefinition);
                        unknownItemDefinitions.Add(itemDefinition);
                    }

                    dict[subtypeId].definition = itemDefinition;
                }
                else
                {
                    dict[subtypeId].amount += currentAmount;
                }

                dict[subtypeId].item = item;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDCargoSummaryInfo: Caught Exception while AddCargoItemDefinition: {e.ToString()}");
            }
        }
        
        CargoItemDefinition FindCargoItemDefinition(string typeId, string subtypeId)
        {
            // First try: exact match on both typeId and subtypeId
            foreach (CargoItemDefinition definition in itemDefinitions)
            {
                if (definition.typeId == typeId && definition.subtypeId == subtypeId) return definition;
            }

            // Second try: match typeId if it contains our definition's typeId (handles MyObjectBuilder_ prefix)
            foreach (CargoItemDefinition definition in itemDefinitions)
            {
                if (typeId.Contains(definition.typeId) && definition.subtypeId == subtypeId) return definition;
            }

            // Third try: subtypeId only (for items without duplicate subtypeIds)
            foreach (CargoItemDefinition definition in itemDefinitions)
            {
                if (definition.subtypeId == subtypeId) return definition;
            }

            // Check unknown items
            foreach (CargoItemDefinition definition in unknownItemDefinitions)
            {
                if (definition.typeId == typeId && definition.subtypeId == subtypeId) return definition;
            }

            return null;
        }

        string ToConfigKey(string sortId)
        {
            // PascalCase key (Show + capitalized segments)
            var sb = new StringBuilder("Show");
            bool upperNext = true;
            foreach (char c in sortId)
            {
                if (c == '_' || c == '-') { upperNext = true; continue; }
                sb.Append(upperNext ? char.ToUpperInvariant(c) : c);
                upperNext = false;
            }
            return sb.ToString();
        }

        string CategoryLabel(string sortId)
        {
            if (string.IsNullOrEmpty(sortId))
                return "Miscellaneous";

            string name;
            if (categoryDisplayNames.TryGetValue(sortId, out name))
                return name;

            if (sortId.Length == 1)
                return sortId.ToUpperInvariant();

            return char.ToUpperInvariant(sortId[0]) + sortId.Substring(1);
        }
    }
}
