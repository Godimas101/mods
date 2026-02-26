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
    [MyTextSurfaceScript("LCDInfoScreenContainerSummary", "$IOS LCD - Container Summary")]
    public class LCDContainerSummaryInfo : MyTextSurfaceScriptBase
    {
        MyIni config = new MyIni();

        public static string CONFIG_SECTION_ID = "SettingsContainerSummary";

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
                titleOffset = 300,
                ratioOffset = 82,
                viewPortOffsetX = 20,
                viewPortOffsetY = 10,
                newLine = new Vector2(0, 30 * textSize),
                showHeader = true,
                showSummary = true,
                showMissing = false,
                showRatio = false,
                showBars = true,
                showSubgrids = false,
                showDocked = false,
                useColors = true,
                showProduction = true,
                showContainers = true
            };
        }

        void CreateConfig()
        {
            TryCreateSurfaceData();

            config.Clear();

            // Build custom formatted config with section headers
            StringBuilder sb = new StringBuilder();
            
            // Always preserve existing CustomData (from other mods/apps)
            string existing = myTerminalBlock.CustomData ?? "";
            if (!string.IsNullOrWhiteSpace(existing))
            {
                sb.Append(existing);
                if (!existing.EndsWith("\n"))
                    sb.AppendLine();
            }

            sb.AppendLine($"[{CONFIG_SECTION_ID}]");
            sb.AppendLine();
            sb.AppendLine("; [ CONTAINER - GENERAL OPTIONS ]");
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
            sb.AppendLine("; [ CONTAINER - LAYOUT OPTIONS ]");
            sb.AppendLine($"TextSize={surfaceData.textSize}");
            sb.AppendLine($"ViewPortOffsetX={surfaceData.viewPortOffsetX}");
            sb.AppendLine($"ViewPortOffsetY={surfaceData.viewPortOffsetY}");
            sb.AppendLine($"TitleFieldWidth={surfaceData.titleOffset}");
            sb.AppendLine($"RatioFieldWidth={surfaceData.ratioOffset}");

            sb.AppendLine();
            sb.AppendLine("; [ CONTAINER - SCREEN OPTIONS ]");
            sb.AppendLine($"HideEmpty={hideEmpty}");
            sb.AppendLine($"ShowContainers={surfaceData.showContainers}");
            sb.AppendLine($"ShowProduction={surfaceData.showProduction}");

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

                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "UseColors", ref surfaceData.useColors, ref configError);
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "HideEmpty", ref hideEmpty, ref configError);

                    // New category toggles (optional)
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowContainers"))
                        surfaceData.showContainers = config.Get(CONFIG_SECTION_ID, "ShowContainers").ToBoolean();
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowProduction"))
                        surfaceData.showProduction = config.Get(CONFIG_SECTION_ID, "ShowProduction").ToBoolean();

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
                    MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDContainerSummaryInfo: Config Syntax error at Line {result}");
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDContainerSummaryInfo: Caught Exception while loading config: {e.ToString()}");
            }
        }

        void CreateExcludeIdsList()
        {
            if (!config.ContainsKey(CONFIG_SECTION_ID, "ExcludeIds")) return;

            string[] exclude = config.Get(CONFIG_SECTION_ID, "ExcludeIds").ToString().Split(',');
            excludeIds.Clear();

            foreach (string s in exclude)
            {
                string t = s.Trim();

                if (t.Contains("<")) continue;
                if (String.IsNullOrEmpty(t) || t == "*" || t == "" || t.Length < 3) continue;

                excludeIds.Add(t);
            }
        }

        IMyTextSurface mySurface;
        IMyTerminalBlock myTerminalBlock;

        List<string> excludeIds = new List<string>();
        List<string> searchIds = new List<string>();
        List<BlockInventoryData> blockInventoriDataList = new List<BlockInventoryData>();
        List<IMyCubeBlock> subgridBlocks = new List<IMyCubeBlock>();  // Cached subgrid blocks

        VRage.Collections.DictionaryValuesReader<MyDefinitionId, MyDefinitionBase> myDefinitions;
        MyDefinitionId myDefinitionId;
        SurfaceDrawer.SurfaceData surfaceData;
        string gridId = "";
        string searchId = "";
        int subgridScanTick = 0;
        bool configError = false;
        bool compactMode = false;
        bool hideEmpty = false;
        bool isStation = false;
        Sandbox.ModAPI.Ingame.MyShipMass gridMass;

        public LCDContainerSummaryInfo(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
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

            if (myTerminalBlock.CustomData.Length <= 0 || !myTerminalBlock.CustomData.Contains(CONFIG_SECTION_ID))
                CreateConfig();

            LoadConfig();

            UpdateInventories();

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

                // Always scan main grid blocks (instant updates)
                var mainBlocks = MahUtillities.GetBlocks(myCubeGrid, searchId, excludeIds, ref gridMass, false, false);

                // Periodically update subgrid/docked block cache
                if (scanSubgrids)
                {
                    var allBlocks = MahUtillities.GetBlocks(myCubeGrid, searchId, excludeIds, ref gridMass, surfaceData.showSubgrids, surfaceData.showDocked);
                    subgridBlocks.Clear();
                    foreach (var block in allBlocks)
                    {
                        if (!mainBlocks.Contains(block))
                            subgridBlocks.Add(block);
                    }
                }

                // Merge main (fresh) and subgrid (cached) blocks
                var myFatBlocks = new List<IMyCubeBlock>();
                myFatBlocks.AddRange(mainBlocks);
                myFatBlocks.AddRange(subgridBlocks);

                blockInventoriDataList.Clear();

                foreach (var myBlock in myFatBlocks)
                {
                    if (myBlock == null) continue;
                    if (!myBlock.HasInventory) continue;
                    if (myBlock is IMyTerminalBlock)
                    {
                        IMyTerminalBlock block = myBlock as IMyTerminalBlock;

                        if (block == null) continue;

                        bool isContainer = block is IMyCargoContainer;
                        bool isProduction = block is IMyProductionBlock; // assembler, refinery, etc.

                        // If neither toggle is enabled, show nothing (hard filter) to avoid noise.
                        bool anyToggle = surfaceData.showContainers || surfaceData.showProduction;
                        if (!anyToggle) continue;

                        // Apply category filtering
                        if (isContainer && !surfaceData.showContainers) continue;
                        if (isProduction && !surfaceData.showProduction) continue;
                        // If block isn't container or production, skip (since this screen should only care about those now)
                        if (!isContainer && !isProduction) continue;

                        BlockInventoryData blockInventoryData = new BlockInventoryData();
                        blockInventoryData.block = block;
                        blockInventoryData.inventories = new IMyInventory[block.InventoryCount];

                        for (int i = 0; i < block.InventoryCount; i++)
                        {
                            blockInventoryData.inventories[i] = block.GetInventory(i);
                        }

                        blockInventoriDataList.Add(blockInventoryData);
                    }
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDContainerSummaryInfo: Caught Exception while updating inventories: {e.ToString()}");
            }
        }

        void DrawMainSprite(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            try
            {
                if (surfaceData.showHeader)
                {
                    SurfaceDrawer.DrawHeader(ref frame, ref position, surfaceData, $"Containers [{(searchId == "*" ? "All" : searchId)} -{excludeIds.Count}][{blockInventoriDataList.Count}] {(surfaceData.showContainers ? "C" : "")} {(surfaceData.showProduction ? "P" : "")}:");
                }

                // Compact (corner LCD) mode: only show total cargo container count, then exit early
                if (compactMode)
                {
                    int cargoContainerCount = 0;
                    foreach (var data in blockInventoriDataList)
                    {
                        // BlockInventoryData appears to be a struct (value type), so null-conditional is invalid.
                        if (data.block is IMyCargoContainer)
                            cargoContainerCount++;
                    }
                    // Header routine advances two line-heights; pull back one to avoid extra vertical gap in compact mode.
                    position -= surfaceData.newLine;
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Cargo Containers Available: {cargoContainerCount}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    position += surfaceData.newLine;
                    return; // stop here for compact layout
                }

                if (blockInventoriDataList.Count == 0)
                {
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"No blocks (enable ShowContainers / ShowProduction)", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    position += surfaceData.newLine;
                    return;
                }

                // Sort blocks alphabetically by custom name
                blockInventoriDataList.Sort((a, b) => 
                {
                    if (a.block == null) return b.block == null ? 0 : 1;
                    if (b.block == null) return -1;
                    return string.Compare(a.block.CustomName, b.block.CustomName, StringComparison.OrdinalIgnoreCase);
                });

                foreach (BlockInventoryData blockInventoryData in blockInventoriDataList)
                {
                    if (blockInventoryData.block == null) continue;
                    if (blockInventoryData.inventories.Length <= 0) continue;
                    if (hideEmpty && blockInventoryData.CurrentVolume <= 0) continue;

                    // If there is only 1 inventory
                    if (blockInventoryData.inventories.Length == 1)
                    {
                        double current = (double)blockInventoryData.inventories[0].CurrentVolume;
                        double total = (double)blockInventoryData.inventories[0].MaxVolume;

                        string blockId = blockInventoryData.block.CustomName;
                        
                        if (surfaceData.showBars)
                        {
                            SurfaceDrawer.DrawBar(ref frame, ref position, surfaceData, $"{blockId}", current, total, Unit.Percent, false, !surfaceData.useColors);
                        }
                        else
                        {
                            SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{blockId}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                            SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{(current / total * 100).ToString("0.0")}%", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                            position += surfaceData.newLine;
                        }
                    }
                    // If this is a production block with multiple inventories
                    else
                    {
                        SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{blockInventoryData.block.CustomName} [{blockInventoryData.inventories.Length.ToString("0")}]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                        position += surfaceData.newLine;

                        for (int i = 0; i < blockInventoryData.inventories.Length; i++)
                        {
                            double current = (double)blockInventoryData.inventories[i].CurrentVolume;
                            double total = (double)blockInventoryData.inventories[i].MaxVolume;

                            if (surfaceData.showBars)
                            {
                                string inventoryId = i == 0 ? "Input" : "Output";

                                SurfaceDrawer.DrawBar(ref frame, ref position, surfaceData, $"- {inventoryId}:", current, total, Unit.Percent, false, !surfaceData.useColors);
                            }
                            else
                            {
                                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{(current / total * 100).ToString("0.0")}%", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                                position += surfaceData.newLine;
                            }
                        }
                    }
                }

                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDContainerSummaryInfo: Caught Exception while DrawMainSprite: {e.ToString()}");
            }
        }
    }
}
