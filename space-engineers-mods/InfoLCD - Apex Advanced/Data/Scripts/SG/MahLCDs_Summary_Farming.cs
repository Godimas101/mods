using MahrianeIndustries.LCDInfo;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Utils;
using VRageMath;

namespace MahrianeIndustries.LCDInfo
{
    [MyTextSurfaceScript("LCDInfoScreenFarmingSummary", "$IOS LCD - Farming")]
    public class LCDFarmingSummaryInfo : MyTextSurfaceScriptBase
    {
        public static string CONFIG_SECTION_ID = "SettingsFarmingSummary";

        MyIni config = new MyIni();
        SurfaceDrawer.SurfaceData surfaceData;
        IMyTextSurface mySurface;
        IMyTerminalBlock myTerminalBlock;

    bool showNutrientPelletBar = true;
    bool showWaterBar = true;
    bool showWaterProduction = true;
    int nutrientPelletMinAmount = 5000;
    readonly string[] defaultWaterGasSubtypes = new string[] { "Water", "HydroSolution" };

    int totalFarmPlots = 0;
    int workingFarmPlots = 0;
    int totalIrrigationSystems = 0;
    int workingIrrigationSystems = 0;

    List<IMyTerminalBlock> farmPlots = new List<IMyTerminalBlock>();
    
    // Cached subgrid farm plots (persisted between main grid scans)
    List<IMyTerminalBlock> subgridFarmPlots = new List<IMyTerminalBlock>();

    // Reusable lists to avoid GC allocations
    List<VRage.Game.ModAPI.Ingame.MyInventoryItem> _cachedInventoryItems = new List<VRage.Game.ModAPI.Ingame.MyInventoryItem>();

    float currentNutrientPelletVolumeL = 0.0f;
    float targetNutrientPelletVolumeL = 0.0f;
    float currentWaterVolumeL = 0.0f;
    float targetWaterVolumeL = 0.0f;
    float gridWaterProductionLPerMin = 0.0f;
    float gridWaterConsumptionLPerMin = 0.0f;

        // Badge visuals (mirrors Weapons centering approach, but wider for Farming)
        const int BadgeInnerWidth = 25;          // inside of [                         ]
        const float BadgeCenterBiasChars = 2.55f; // nudge center a bit for visual centering

        string searchId = "*";
        List<string> excludeIds = new List<string>();
        int subgridScanTick = 0;
        bool compactMode = false;
        Sandbox.ModAPI.Ingame.MyShipMass gridMass;

        public LCDFarmingSummaryInfo(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            mySurface = surface;
            myTerminalBlock = block as IMyTerminalBlock;
        }

        public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update10;

        void TryCreateSurfaceData()
        {
            if (surfaceData != null) return;

            compactMode = mySurface.SurfaceSize.X / mySurface.SurfaceSize.Y > 4f;
            float textSize = compactMode ? 0.6f : 0.4f;
            
            // Debug: log compact mode detection
            MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDFarmingSummaryInfo: Surface size {mySurface.SurfaceSize.X}x{mySurface.SurfaceSize.Y}, ratio {mySurface.SurfaceSize.X / mySurface.SurfaceSize.Y}, compactMode={compactMode}");

            surfaceData = new SurfaceDrawer.SurfaceData
            {
                surface = mySurface,
                textSize = textSize,
                titleOffset = 240,
                ratioOffset = 82,
                viewPortOffsetX = compactMode ? 10 : 20,
                viewPortOffsetY = compactMode ? 5 : 10,
                newLine = new Vector2(0, 30 * textSize),
                showHeader = true,
                showSummary = true,
                showMissing = false,
                showRatio = false,
                showBars = false,
                showSubgrids = false,
                showDocked = false,
                useColors = true
            };
        }

        public override void Run()
        {
            // Prevent execution on a dedicated server to avoid server-side load
            if (Sandbox.ModAPI.MyAPIGateway.Utilities?.IsDedicated ?? false)
                return;

            if (myTerminalBlock.CustomData.Length <= 0 || !myTerminalBlock.CustomData.Contains(CONFIG_SECTION_ID))
                CreateConfig();

            LoadConfig();
            UpdateBlocks();
            Draw();
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
            sb.AppendLine("; [ FARMING - GENERAL OPTIONS ]");
            sb.AppendLine("SearchId=*");
            sb.AppendLine("ExcludeIds=");
            sb.AppendLine($"ShowHeader={surfaceData.showHeader}");
            sb.AppendLine($"ShowSummary={surfaceData.showSummary}");
            sb.AppendLine($"ShowSubgrids={surfaceData.showSubgrids}");
            sb.AppendLine($"SubgridUpdateFrequency={surfaceData.subgridUpdateFrequency}");
            sb.AppendLine("; Subgrid scan frequency: 1=fastest (60/sec), 10=normal (6/sec), 100=slowest (0.6/sec)");
            sb.AppendLine($"ShowDocked={surfaceData.showDocked}");
            sb.AppendLine($"UseColors={surfaceData.useColors}");

            sb.AppendLine();
            sb.AppendLine("; [ FARMING - LAYOUT OPTIONS ]");
            sb.AppendLine($"TextSize={surfaceData.textSize}");
            sb.AppendLine($"ViewPortOffsetX={surfaceData.viewPortOffsetX}");
            sb.AppendLine($"ViewPortOffsetY={surfaceData.viewPortOffsetY}");
            sb.AppendLine($"TitleFieldWidth={surfaceData.titleOffset}");
            sb.AppendLine($"RatioFieldWidth={surfaceData.ratioOffset}");

            sb.AppendLine();
            sb.AppendLine("; [ FARMING - SCREEN OPTIONS ]");
            sb.AppendLine($"ShowNutrientPelletBar={showNutrientPelletBar}");
            sb.AppendLine($"ShowWaterBar={showWaterBar}");
            sb.AppendLine($"ShowWaterProduction={showWaterProduction}");

            sb.AppendLine();
            sb.AppendLine("; [ FARMING - ITEM THRESHOLDS ]");
            var hydroPelletsDef = MahDefinitions.GetDefinition("Ore", "HydroPellets");
            nutrientPelletMinAmount = (nutrientPelletMinAmount > 0) ? nutrientPelletMinAmount : (hydroPelletsDef != null && hydroPelletsDef.minAmount > 0 ? hydroPelletsDef.minAmount : 5000);
            sb.AppendLine($"NutrientPelletMinAmount={nutrientPelletMinAmount}");

            sb.AppendLine();

            myTerminalBlock.CustomData = sb.ToString();
        }

        void LoadConfig()
        {
            try
            {
                MyIniParseResult result;
                TryCreateSurfaceData();
                if (config.TryParse(myTerminalBlock.CustomData, CONFIG_SECTION_ID, out result))
                {
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowHeader")) surfaceData.showHeader = config.Get(CONFIG_SECTION_ID, "ShowHeader").ToBoolean();
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowSummary")) surfaceData.showSummary = config.Get(CONFIG_SECTION_ID, "ShowSummary").ToBoolean();
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowSubgrids")) surfaceData.showSubgrids = config.Get(CONFIG_SECTION_ID, "ShowSubgrids").ToBoolean();
                    if (config.ContainsKey(CONFIG_SECTION_ID, "SubgridUpdateFrequency")) surfaceData.subgridUpdateFrequency = config.Get(CONFIG_SECTION_ID, "SubgridUpdateFrequency").ToInt32();
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowDocked")) surfaceData.showDocked = config.Get(CONFIG_SECTION_ID, "ShowDocked").ToBoolean();
                    if (config.ContainsKey(CONFIG_SECTION_ID, "TextSize")) surfaceData.textSize = config.Get(CONFIG_SECTION_ID, "TextSize").ToSingle(0.4f);
                    if (config.ContainsKey(CONFIG_SECTION_ID, "TitleFieldWidth")) surfaceData.titleOffset = config.Get(CONFIG_SECTION_ID, "TitleFieldWidth").ToInt32();
                    if (config.ContainsKey(CONFIG_SECTION_ID, "RatioFieldWidth")) surfaceData.ratioOffset = config.Get(CONFIG_SECTION_ID, "RatioFieldWidth").ToInt32();
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ViewPortOffsetX")) surfaceData.viewPortOffsetX = config.Get(CONFIG_SECTION_ID, "ViewPortOffsetX").ToInt32();
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ViewPortOffsetY")) surfaceData.viewPortOffsetY = config.Get(CONFIG_SECTION_ID, "ViewPortOffsetY").ToInt32();
                    if (config.ContainsKey(CONFIG_SECTION_ID, "UseColors")) surfaceData.useColors = config.Get(CONFIG_SECTION_ID, "UseColors").ToBoolean();
                    if (config.ContainsKey(CONFIG_SECTION_ID, "SearchId")) searchId = config.Get(CONFIG_SECTION_ID, "SearchId").ToString();
                    if (string.IsNullOrWhiteSpace(searchId)) searchId = "*";

                    if (config.ContainsKey(CONFIG_SECTION_ID, "ExcludeIds"))
                    {
                        excludeIds.Clear();
                        var raw = config.Get(CONFIG_SECTION_ID, "ExcludeIds").ToString();
                        if (!string.IsNullOrWhiteSpace(raw))
                        {
                            foreach (var part in raw.Split(','))
                            {
                                var t = part.Trim();
                                if (t.Length >= 3 && t != "*") excludeIds.Add(t);
                            }
                        }
                    }

                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowNutrientPelletBar")) showNutrientPelletBar = config.Get(CONFIG_SECTION_ID, "ShowNutrientPelletBar").ToBoolean(true);
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowWaterBar")) showWaterBar = config.Get(CONFIG_SECTION_ID, "ShowWaterBar").ToBoolean(true);
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowWaterProduction")) showWaterProduction = config.Get(CONFIG_SECTION_ID, "ShowWaterProduction").ToBoolean(true);
                    if (config.ContainsKey(CONFIG_SECTION_ID, "NutrientPelletMinAmount")) nutrientPelletMinAmount = config.Get(CONFIG_SECTION_ID, "NutrientPelletMinAmount").ToInt32();

                    surfaceData.newLine = new Vector2(0, 30 * surfaceData.textSize);

                    if (compactMode)
                    {
                        surfaceData.textSize = 0.6f;
                        surfaceData.titleOffset = 200;
                        surfaceData.viewPortOffsetX = 10;
                        surfaceData.viewPortOffsetY = 5;
                        surfaceData.newLine = new Vector2(0, 30 * surfaceData.textSize);
                    }
                }
                else
                {
                    MyLog.Default.WriteLine("MahrianeIndustries.LCDInfo.LCDFarmingSummaryInfo: Config syntax error: " + result.ToString());
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("MahrianeIndustries.LCDInfo.LCDFarmingSummaryInfo: Exception loading config: " + e.ToString());
            }
        }

        void UpdateBlocks()
        {
            // Determine if we should scan subgrids this cycle
            bool scanSubgrids = false;
            if (surfaceData.showSubgrids)
            {
                subgridScanTick++;
                if (subgridScanTick >= surfaceData.subgridUpdateFrequency / 10)
                {
                    subgridScanTick = 0;
                    scanSubgrids = true;
                }
            }

            try
            {
                totalFarmPlots = 0;
                workingFarmPlots = 0;
                totalIrrigationSystems = 0;
                workingIrrigationSystems = 0;
                farmPlots.Clear();

                var myCubeGrid = myTerminalBlock.CubeGrid as MyCubeGrid;
                if (myCubeGrid == null) return;

                // Always get main grid blocks
                var mainBlocks = MahUtillities.GetBlocks(myCubeGrid, searchId, excludeIds, ref gridMass, false, false);
                if (mainBlocks == null) return;

                // Process main grid blocks
                foreach (var b in mainBlocks)
                {
                    if (b == null) continue;
                        var subtype = b.BlockDefinition != null ? b.BlockDefinition.Id.SubtypeName : "";
                    var lower = subtype.ToLower();

                    // Irrigation System detection (separate block type)
                    if (lower.Contains("irrigation"))
                    {
                        totalIrrigationSystems++;
                        if (b is IMyTerminalBlock)
                        {
                            var termBlock = (IMyTerminalBlock)b;
                            // Count working irrigation systems (functional and actually working)
                            if (termBlock.IsFunctional && termBlock.IsWorking)
                                workingIrrigationSystems++;
                        }
                        continue;
                    }

                    // Farm plot detection (base game uses FarmBlock/FarmPlot patterns; modded we include vertical/inset variants)
                    if (lower.Contains("farmplot") || lower.Contains("farm_block") || lower.Contains("farmblock") || lower.Contains("flat_farm") || lower.Contains("verticalfarmplot") || lower.Contains("insetfarmplot"))
                    {
                        totalFarmPlots++;
                        if (b is IMyTerminalBlock)
                        {
                            var termBlock = (IMyTerminalBlock)b;
                            farmPlots.Add(termBlock);
                            // Count working farms (functional and actually working)
                            if (termBlock.IsFunctional && termBlock.IsWorking)
                                workingFarmPlots++;
                        }
                        continue;
                    }
                }

                // Periodically update subgrid cache
                if (scanSubgrids)
                {
                    var allBlocks = MahUtillities.GetBlocks(myCubeGrid, searchId, excludeIds, ref gridMass, true, false);
                    if (allBlocks != null)
                    {
                        subgridFarmPlots.Clear();
                        
                        // Extract subgrid-only blocks
                        foreach (var b in allBlocks)
                        {
                            if (mainBlocks.Contains(b)) continue;
                            if (b == null) continue;
                            var subtype = b.BlockDefinition != null ? b.BlockDefinition.Id.SubtypeName : "";
                            var lower = subtype.ToLower();

                            // Irrigation System detection (separate block type)
                            if (lower.Contains("irrigation"))
                            {
                                totalIrrigationSystems++;
                                if (b is IMyTerminalBlock)
                                {
                                    var termBlock = (IMyTerminalBlock)b;
                                    // Count working irrigation systems (functional and actually working)
                                    if (termBlock.IsFunctional && termBlock.IsWorking)
                                        workingIrrigationSystems++;
                                }
                                continue;
                            }

                            // Farm plot detection (base game uses FarmBlock/FarmPlot patterns; modded we include vertical/inset variants)
                            if (lower.Contains("farmplot") || lower.Contains("farm_block") || lower.Contains("farmblock") || lower.Contains("flat_farm") || lower.Contains("verticalfarmplot") || lower.Contains("insetfarmplot"))
                            {
                                totalFarmPlots++;
                                if (b is IMyTerminalBlock)
                                {
                                    var termBlock = (IMyTerminalBlock)b;
                                    subgridFarmPlots.Add(termBlock);
                                    // Count working farms (functional and actually working)
                                    if (termBlock.IsFunctional && termBlock.IsWorking)
                                        workingFarmPlots++;
                                }
                                continue;
                            }
                        }
                    }
                }
                
                // Merge cached subgrid farm plots
                farmPlots.AddRange(subgridFarmPlots);
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("MahrianeIndustries.LCDInfo.LCDFarmingSummaryInfo: Exception updating blocks: " + e.ToString());
            }
        }

        void UpdateResourceBars()
        {
            try
            {
                currentNutrientPelletVolumeL = 0f;
                targetNutrientPelletVolumeL = 0f;
                currentWaterVolumeL = 0f;
                targetWaterVolumeL = 0f;
                gridWaterProductionLPerMin = 0f;
                gridWaterConsumptionLPerMin = 0f;

                var myCubeGrid = myTerminalBlock.CubeGrid as MyCubeGrid;
                if (myCubeGrid == null) return;

                // Query grid-wide water availability from resource distributor (authoritative source)
                try
                {
                    var waterId = new MyDefinitionId(typeof(VRage.Game.ObjectBuilders.Definitions.MyObjectBuilder_GasProperties), "Water");
                    
                    // Get water stored in tanks (current amount available)
                    currentWaterVolumeL = GetGridWaterFromTanks(myCubeGrid);
                    
                    // Get water tank capacity (max available storage)
                    targetWaterVolumeL = GetGridWaterTankCapacity(myCubeGrid);
                    
                    // Calculate grid-wide water production and consumption by summing individual blocks
                    CalculateGridWaterFlow(myCubeGrid, out gridWaterProductionLPerMin, out gridWaterConsumptionLPerMin);
                }
                catch { }

                // Compute HydroPellets (Nutrient Pellets) volumes
                CargoItemDefinition hydroPelletsDef = MahDefinitions.GetDefinition("Ore", "HydroPellets");
                float hydroPelletsItemVolumeL = (hydroPelletsDef != null ? hydroPelletsDef.volume : 0f); // volume is already in liters
                int targetHydroPelletsItems = (nutrientPelletMinAmount > 0 ? nutrientPelletMinAmount : (hydroPelletsDef != null ? hydroPelletsDef.minAmount : 5000));
                if (hydroPelletsDef != null && targetHydroPelletsItems > 0)
                {
                    targetNutrientPelletVolumeL = targetHydroPelletsItems * hydroPelletsDef.volume; // volume is already in liters
                }

                Sandbox.ModAPI.Ingame.MyShipMass tmpMass = gridMass;
                var allBlocks = MahUtillities.GetBlocks(myCubeGrid, searchId, excludeIds, ref tmpMass, true, false);
                if (allBlocks != null && allBlocks.Count > 0)
                {
                    var hydroPelletsData = MahUtillities.GetGridHydroPelletsData(allBlocks, hydroPelletsItemVolumeL, _cachedInventoryItems);
                    currentNutrientPelletVolumeL = hydroPelletsData.currentHydroPelletsVolumeL;
                }

                if (targetNutrientPelletVolumeL <= 0f)
                {
                    targetNutrientPelletVolumeL = Math.Max(currentNutrientPelletVolumeL, 1f);
                }
                // Don't set fallback for water - if no tanks exist, targetWaterVolumeL stays 0 and bar is hidden
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("MahrianeIndustries.LCDInfo.LCDFarmingSummaryInfo: Exception updating resource bars: " + e.ToString());
            }
        }

        void Draw()
        {
            try
            {
                TryCreateSurfaceData();

                var frame = mySurface.DrawFrame();
                var viewport = new RectangleF((mySurface.TextureSize - mySurface.SurfaceSize) / 2f, mySurface.SurfaceSize);
                var position = new Vector2(surfaceData.viewPortOffsetX, surfaceData.viewPortOffsetY) + viewport.Position;

                if (surfaceData.showHeader)
                {
                    SurfaceDrawer.DrawHeader(ref frame, ref position, surfaceData, "Farming", "");
                }

                UpdateResourceBars();

                if (compactMode)
                {
                    position -= surfaceData.newLine;
                }

                if (!compactMode)
                {
                    if (showNutrientPelletBar)
                    {
                        SurfaceDrawer.DrawBarFixedColor(ref frame, ref position, surfaceData, "NutriP", currentNutrientPelletVolumeL, targetNutrientPelletVolumeL, Color.SandyBrown, surfaceData.showRatio ? Unit.Percent : Unit.Liters);
                    }
                    // Only show water bar if there are water tanks on the grid
                    if (showWaterBar && targetWaterVolumeL > 0f)
                    {
                        SurfaceDrawer.DrawBarFixedColor(ref frame, ref position, surfaceData, "HydS", currentWaterVolumeL, targetWaterVolumeL, Color.LightSkyBlue, surfaceData.showRatio ? Unit.Percent : Unit.Liters);
                    }
                }
                
                if (showWaterProduction)
                {
                    float netWaterFlow = gridWaterProductionLPerMin - gridWaterConsumptionLPerMin;
                    string netFlowSign = netWaterFlow >= 0 ? "+" : "";
                    string numberStr = $"{netFlowSign}{netWaterFlow.ToString("0.0")}";
                    Color netFlowColor = netWaterFlow >= 0 ? Color.GreenYellow : Color.Red;
                    if (!surfaceData.useColors) netFlowColor = surfaceData.surface.ScriptForegroundColor;
                    
                    // Left side: HydS Production (split into three colored parts)
                    // Part 1: "HydS Production: " (white)
                    var startPos = position;
                    SurfaceDrawer.WriteTextSprite(ref frame, startPos, surfaceData, "HydS Production: ", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    
                    // Part 2: "+1234.5" or "-123.4" (green or red)
                    float ppc = MahDefinitions.pixelPerChar * surfaceData.textSize;
                    float labelWidth = "HydS Production: ".Length * ppc;
                    float numberOffset = compactMode ? 65f : 50f;
                    var numberPos = new Vector2(startPos.X + labelWidth + numberOffset, startPos.Y);
                    SurfaceDrawer.WriteTextSprite(ref frame, numberPos, surfaceData, numberStr, TextAlignment.LEFT, netFlowColor);
                    
                    // Part 3: " L/min" (white)
                    float numberWidth = numberStr.Length * ppc;
                    var unitPos = new Vector2(numberPos.X + numberWidth + 30f, startPos.Y);
                    SurfaceDrawer.WriteTextSprite(ref frame, unitPos, surfaceData, " L/min", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    
                    // Right side: Irrigation Systems
                    string irrigationStr = $"Irrigation Systems: {workingIrrigationSystems}/{totalIrrigationSystems}";
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, irrigationStr, TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                    
                    position += surfaceData.newLine;
                }

                // In normal mode, add extra spacing after water production line
                if (!compactMode)
                {
                    position += surfaceData.newLine;
                }

                // Compact mode: simplified single-line summary
                if (compactMode)
                {
                    DrawCompactFarmingSummary(ref frame, ref position);
                }
                else
                {
                    // Sort farm plots alphabetically by custom name
                    MahSorting.SortBlocksByName(farmPlots);

                    // List farm plots with three-line entries
                    foreach (var plot in farmPlots)
                    {
                        if (plot == null) continue;
                        DrawFarmPlotEntry(ref frame, ref position, plot);
                    }
                }

                frame.Dispose();
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("MahrianeIndustries.LCDInfo.LCDFarmingSummaryInfo: Exception drawing: " + e.ToString());
            }
        }

        void DrawCompactFarmingSummary(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            try
            {
                // Count farms by state
                int idleCount = 0;
                int growingCount = 0;
                int harvestCount = 0;

                foreach (var plot in farmPlots)
                {
                    if (plot == null) continue;

                    string cropName; float growthPct; float hydrationPct; string statusTxt; float healthPct; bool cropSlotEmpty; float waterUsageLMin;
                    TryParseFarmDetails(plot, out cropName, out growthPct, out hydrationPct, out statusTxt, out healthPct, out cropSlotEmpty, out waterUsageLMin);

                    bool isEmpty = cropSlotEmpty;
                    if (isEmpty)
                    {
                        growthPct = 0f;
                        healthPct = 0f;
                    }

                    var badgeState = GetFarmBadgeState(plot.IsFunctional, plot.IsWorking, hydrationPct, growthPct, healthPct, isEmpty);

                    if (badgeState == "Harvest")
                        harvestCount++;
                    else if (badgeState == "Growing")
                        growingCount++;
                    else
                        idleCount++;
                }

                // Draw single line: Farm Plots: [ Idle: #/# ] [ Growing: #/# ] [ Harvest: #/# ]
                var startPos = position;
                float ppc = MahDefinitions.pixelPerChar * surfaceData.textSize;

                // Part 1: "Farm Plots: "
                SurfaceDrawer.WriteTextSprite(ref frame, startPos, surfaceData, "Farm Plots: ", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                float offset = "Farm Plots: ".Length * ppc + 50f; // Add spacing offset here if needed

                // Part 2: "[ Idle: #/# ]"
                var idlePos = new Vector2(startPos.X + offset, startPos.Y);
                string idleStr = $"[ Idle: {idleCount}/{totalFarmPlots} ]";
                SurfaceDrawer.WriteTextSprite(ref frame, idlePos, surfaceData, idleStr, TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                offset += idleStr.Length * ppc + 35f; 

                // Part 3: "[ Growing: #/# ]"
                var growingPos = new Vector2(startPos.X + offset, startPos.Y);
                string growingStr = $"[ Growing: {growingCount}/{totalFarmPlots} ]";
                SurfaceDrawer.WriteTextSprite(ref frame, growingPos, surfaceData, growingStr, TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                offset += growingStr.Length * ppc + 58f; 

                // Part 4: "[ Harvest: #/# ]"
                var harvestPos = new Vector2(startPos.X + offset, startPos.Y);
                string harvestStr = $"[ Harvest: {harvestCount}/{totalFarmPlots} ]";
                SurfaceDrawer.WriteTextSprite(ref frame, harvestPos, surfaceData, harvestStr, TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);

                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("MahrianeIndustries.LCDInfo.LCDFarmingSummaryInfo: Exception DrawCompactFarmingSummary: " + e.ToString());
            }
        }

    void DrawFarmPlotEntry(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTerminalBlock plot)
        {
            try
            {
                // Line 1: Name
                var name = plot.CustomName ?? "Farm Plot";
                name = MahUtillities.GetSubstring(name, surfaceData, true);
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, name, TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine;

                // Parse farm state using IMyFarmPlotLogic component for real-time crop detection
                string cropName; float growthPct; float hydrationPct; string statusTxt; float healthPct; bool cropSlotEmpty; float waterUsageLMin;
                TryParseFarmDetails(plot, out cropName, out growthPct, out hydrationPct, out statusTxt, out healthPct, out cropSlotEmpty, out waterUsageLMin);
                
                // Trust IMyFarmPlotLogic.IsPlantPlanted as the authoritative real-time source
                bool isEmpty = cropSlotEmpty;
                
                // If the slot is empty, override any stale data from DetailedInfo
                if (isEmpty)
                {
                    cropName = string.Empty;
                    growthPct = 0f;
                    healthPct = 0f;
                    waterUsageLMin = 0f;
                }

                // Determine badge state (simplified logic - no memory/latching)
                var badgeState = GetFarmBadgeState(plot.IsFunctional, plot.IsWorking, hydrationPct, growthPct, healthPct, isEmpty);
                var badgeColor = !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor :
                    (badgeState == "Off" ? Color.Red :
                     badgeState == "Damaged" ? Color.Red :
                     badgeState == "Dead" ? Color.Red :
                     badgeState == "LowHealth" ? Color.Orange :
                     badgeState == "NoHydration" ? Color.Red :
                     badgeState == "LowHydration" ? Color.Magenta :
                     badgeState == "Harvest" ? Color.GreenYellow :
                     badgeState == "Growing" ? Color.GreenYellow :
                     badgeState == "Idle" ? Color.Yellow :
                     Color.GreenYellow);

                // Line 2: [   BADGE   ] Hydration:   |-----------> (right bar)
                string badgeShell = "[" + new string(' ', BadgeInnerWidth) + "]";
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{badgeShell}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                
                // Anchor "Hydration:" label on the right
                {
                    float pixelPerChar = MahDefinitions.pixelPerChar * surfaceData.textSize;
                    int barLength = (int)((surfaceData.ratioOffset / pixelPerChar) * .8f);
                    int barShellChars = barLength + 2;
                    int gapChars = 2;
                    int labelGapChars = 2;
                    float labelShiftChars = 13.5f;
                    float totalLeftChars = barShellChars + gapChars + labelGapChars + labelShiftChars;
                    var hydrationLabelRightPos = new Vector2((position.X - (totalLeftChars * pixelPerChar)), position.Y);
                    SurfaceDrawer.WriteTextSprite(ref frame, hydrationLabelRightPos, surfaceData, "Hydration:", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                }
                
                // Center the badge text inside the brackets
                {
                    float ppc = MahDefinitions.pixelPerChar * surfaceData.textSize;
                    float desiredCenterX = position.X + ppc * (1f + (BadgeInnerWidth * 0.5f) + BadgeCenterBiasChars);
                    var centerPos = new Vector2(desiredCenterX - (surfaceData.surface.SurfaceSize.X * 0.5f), position.Y);
                    SurfaceDrawer.WriteTextSprite(ref frame, centerPos, surfaceData, badgeState, TextAlignment.CENTER, badgeColor);
                }
                
                // Display crop name after the badge (two spaces after the closing bracket)
                {
                    float ppc = MahDefinitions.pixelPerChar * surfaceData.textSize;
                    // Badge starts at position.X and is: '[' (1 char) + BadgeInnerWidth + ']' (1 char)
                    // Add 5 more spaces to shift it further right
                    float badgeWidthInPixels = (1f + BadgeInnerWidth + 1f) * ppc;
                    var cropNamePos = new Vector2(position.X + badgeWidthInPixels + (7f * ppc), position.Y); // 2 spaces + 5 extra = 7 spaces after ']'
                    var displayCropName = string.IsNullOrWhiteSpace(cropName) ? "None" : cropName;
                    SurfaceDrawer.WriteTextSprite(ref frame, cropNamePos, surfaceData, displayCropName, TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                }
                
                // Hydration bar (right-aligned)
                SurfaceDrawer.DrawHalfBar(ref frame, position, surfaceData, TextAlignment.RIGHT, hydrationPct, 100f, Unit.Percent, Color.LightSkyBlue);
                position += surfaceData.newLine;

                // Line 3: Hydro Usage and Crop Health (left) and "Growth: xx% [bar]" (right)
                string healthStr = $"{Math.Max(0f, Math.Min(100f, healthPct)).ToString("0")}%";
                string waterUsageStr = waterUsageLMin.ToString("0.0");
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Hydro Usage: {waterUsageStr} L/min   Crop Health: {healthStr}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                DrawRightHalfBarWithLabel(ref frame, position, "Growth:", growthPct, 100f, Color.GreenYellow, 0, 20f);
                position += surfaceData.newLine + surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("MahrianeIndustries.LCDInfo.LCDFarmingSummaryInfo: Exception DrawFarmPlotEntry: " + e.ToString());
            }
        }

        void TryParseFarmDetails(IMyTerminalBlock plot, out string cropName, out float growthPct, out float hydrationPct, out string statusTxt, out float healthPct, out bool cropSlotEmpty, out float waterUsageLMin)
        {
            cropName = ""; 
            growthPct = 0f; 
            hydrationPct = 0f; 
            statusTxt = plot.IsWorking ? "    On" : "    Off"; 
            healthPct = 0f; 
            cropSlotEmpty = false;
            waterUsageLMin = 0f;
            
            try
            {
                // Get real-time crop state from IMyFarmPlotLogic component
                var cubeBlock = plot as MyCubeBlock;
                if (cubeBlock != null && cubeBlock.Components != null)
                {
                    foreach (var comp in cubeBlock.Components)
                    {
                        if (comp == null) continue;
                        
                        var farmLogic = comp as Sandbox.ModAPI.IMyFarmPlotLogic;
                        if (farmLogic != null)
                        {
                            // IsPlantPlanted updates in real-time - this is our authoritative source
                            cropSlotEmpty = !farmLogic.IsPlantPlanted;
                            break;
                        }
                    }
                }
                
                // Check for hydration from resource storage component (internal water reservoir)
                try
                {
                    var cubeBlock2 = plot as MyCubeBlock;
                    if (cubeBlock2 != null)
                    {
                        foreach (var comp in cubeBlock2.Components)
                        {
                            if (comp == null) continue;
                            var storage = comp as Sandbox.ModAPI.IMyResourceStorageComponent;
                            if (storage != null)
                            {
                                // If storage component exists with a filled ratio, farm has internal water storage
                                double fr = storage.FilledRatio;
                                if (fr >= 0)
                                {
                                    hydrationPct = (float)(fr * 100f);
                                    break;
                                }
                            }
                        }
                    }
                }
                catch { }

                // Check for gas tank interface (modded water storage)
                var asTank = plot as IMyGasTank;
                if (asTank != null)
                {
                    try
                    {
                        hydrationPct = Math.Max(hydrationPct, (float)(asTank.FilledRatio * 100f));
                    }
                    catch { }
                }

                // Parse DetailedInfo for crop name, growth%, health%, and hydration (if not found via component)
                var info = plot.DetailedInfo ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(info))
                {
                    var lines = info.Split('\n');
                    foreach (var line in lines)
                    {
                        var l = line.Trim();
                        if (l.Length == 0) continue;
                        var low = l.ToLower();

                        // Crop name
                        if (string.IsNullOrWhiteSpace(cropName))
                        {
                            if (low.Contains("current crop type") || low.StartsWith("current crop") || low.Contains("crop:"))
                            {
                                var parts = l.Split(':');
                                if (parts.Length > 1)
                                {
                                    cropName = parts[1].Trim();
                                }
                            }
                        }

                        // Growth percentage
                        if (growthPct <= 0f && (low.Contains("growth") || low.Contains("progress") || low.Contains("completion")))
                        {
                            growthPct = ParsePercentFromLine(l);
                            if (growthPct <= 0f)
                            {
                                growthPct = ParseBareNumeric0to100(l);
                            }
                        }

                        // Hydration (only if not found via component)
                        if (hydrationPct <= 0f && (low.Contains("hydration") || low.Contains("water") || low.Contains("moisture")))
                        {
                            hydrationPct = ParsePercentFromLine(l);
                            if (hydrationPct <= 0f)
                            {
                                hydrationPct = ParseLitersPercentFromLine(l);
                            }
                        }

                        // Health percentage
                        if (healthPct <= 0f && (low.Contains("health") || low.Contains("condition")))
                        {
                            healthPct = ParsePercentFromLine(l);
                        }
                        
                        // Water usage (Required Input section contains "Water: X.X L/min")
                        if (waterUsageLMin <= 0f && low.Contains("water") && low.Contains("l/min"))
                        {
                            // Extract numeric value before "l/min"
                            var match = System.Text.RegularExpressions.Regex.Match(l, @"(\d+\.?\d*)\s*l/min", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            if (match.Success)
                            {
                                float usage;
                                if (float.TryParse(match.Groups[1].Value, out usage))
                                {
                                    waterUsageLMin = usage;
                                }
                            }
                        }

                        // Status text override
                        if (low.Contains("status:") || low.Contains("state:"))
                        {
                            var parts = l.Split(':');
                            if (parts.Length > 1)
                            {
                                statusTxt = $"   {parts[1].Trim()}";
                            }
                        }
                    }

                    // Fallback hydration from generic "Filled %" line
                    if (hydrationPct <= 0f)
                    {
                        foreach (var line in lines)
                        {
                            var l = line.Trim();
                            if (l.Length == 0) continue;
                            if (l.IndexOf("filled", StringComparison.OrdinalIgnoreCase) >= 0 && l.IndexOf('%') >= 0)
                            {
                                var p = ParsePercentFromLine(l);
                                if (p > 0f)
                                {
                                    hydrationPct = p;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        string GetFarmBadgeState(bool isFunctional, bool isWorking, float hydrationPct, float growthPct, float healthPct, bool isEmpty)
        {
            // Priority 1: Off state
            if (!isFunctional) return "Off";
            
            // Priority 2: Damaged (on but not working)
            if (isFunctional && !isWorking) return "Damaged";
            
            // Priority 3: Dead crop (0% health)
            if (!isEmpty && healthPct <= 0f) return "Dead";
            
            // Priority 4: Low health crop (<30%)
            if (!isEmpty && healthPct < 30f) return "LowHealth";
            
            // Priority 5: No hydration (0%)
            if (hydrationPct <= 0f) return "NoHydration";
            
            // Priority 6: Low hydration (<40%)
            if (hydrationPct < 40f) return "LowHydration";
            
            // Priority 7: Harvest ready (crop at 100% growth with good health)
            if (!isEmpty && healthPct >= 30f && growthPct >= 99.5f) return "Harvest";
            
            // Priority 8: Growing (crop actively growing with good health)
            if (!isEmpty && healthPct >= 30f && growthPct > 0f) return "Growing";
            
            // Priority 9: Idle (empty slot with good hydration)
            if (isEmpty) return "Idle";
            
            // Default fallback
            return "Idle";
        }

        // Helper: draw a right-aligned half bar at the fixed right anchor.
        // Also draw the percentage right-aligned just to the left of the bar with a fixed gap,
        // and draw the label text (e.g., "Growth:") as a separate element further left with its own fixed gap.
        // This keeps bar, percent, and label visually stable with no sliding as digits change.
        // labelShiftChars: additional fine-grained left shift for the label, in character widths (can be fractional, e.g., 8.5f)
        void DrawRightHalfBarWithLabel(ref MySpriteDrawFrame frame, Vector2 position, string labelPrefix, float current, float total, Color barColor, int gapChars = 2, float labelShiftChars = 0f)
        {
            try
            {
                // Avoid division by 0
                total = total <= 0 ? 1 : total;

                float pixelPerChar = MahDefinitions.pixelPerChar * surfaceData.textSize;
                int barLength = (int)((surfaceData.ratioOffset / pixelPerChar) * .8f);
                float ratio = current / total;
                if (ratio < 0f) ratio = 0f; if (ratio > 1f) ratio = 1f;
                int currentValue = (int)(ratio * barLength);

                string backgroundBar = "";
                string bar = "";
                for (int i = 0; i < barLength; i++) backgroundBar += "'";
                for (int i = 0; i < barLength; i++) if (i < currentValue) bar += "|";

                // 1) Draw the bar background at the fixed right anchor
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"[{backgroundBar}]", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                if (current > 0)
                {
                    // 2) Overlay the filled portion inside the brackets (aligned to the same right anchor)
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $" {bar} ", TextAlignment.RIGHT, surfaceData.useColors ? barColor : surfaceData.surface.ScriptForegroundColor);
                }

                // 3) Draw the percent just to the left of the bar with a consistent gap (right-aligned to prevent sliding)
                int barShellChars = barLength + 2; // '[' + background + ']'
                float percentRightChars = barShellChars + Math.Max(0, gapChars);
                var percentRightPos = new Vector2(position.X - (percentRightChars * pixelPerChar), position.Y);

                float percent = total == 100f ? Math.Max(0f, Math.Min(100f, current)) : Math.Max(0f, Math.Min(100f, (current / total) * 100f));
                string percentStr = percent.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "%";
                SurfaceDrawer.WriteTextSprite(ref frame, percentRightPos, surfaceData, percentStr, TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);

                // 4) Draw the label (e.g., "Growth:") further left as its own element, with a fixed gap from the percent
                if (!string.IsNullOrWhiteSpace(labelPrefix))
                {
                    int labelGapChars = 2;
                    var labelRightPos = new Vector2(percentRightPos.X - ((labelGapChars + labelShiftChars) * pixelPerChar), position.Y);
                    SurfaceDrawer.WriteTextSprite(ref frame, labelRightPos, surfaceData, labelPrefix, TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDFarmingSummaryInfo: Exception DrawRightHalfBarWithLabel: {e.ToString()}");
            }
        }

        float ParsePercentFromLine(string l)
        {
            try
            {
                int p = l.IndexOf('%');
                if (p > 0)
                {
                    // find digits before '%'
                    string num = "";
                    for (int i = p - 1; i >= 0; i--)
                    {
                        char c = l[i];
                        if ((c >= '0' && c <= '9') || c == '.' || c == ',') num = c + num; else if (num.Length > 0) break;
                    }
                    float v = 0f;
                    if (float.TryParse(num.Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v))
                        return v;
                }
            }
            catch { }
            return 0f;
        }

        // Extract the last numeric token in the line and treat it as a percent (0..100) if no '%' is present
        float ParseBareNumeric0to100(string l)
        {
            try
            {
                if (l.IndexOf('%') >= 0) return 0f; // handled elsewhere
                float last = 0f;
                bool found = false;
                string acc = "";
                for (int i = 0; i < l.Length; i++)
                {
                    char c = l[i];
                    if ((c >= '0' && c <= '9') || c == '.' || c == ',')
                    {
                        acc += c;
                    }
                    else
                    {
                        if (acc.Length > 0)
                        {
                            float v = 0f;
                            if (float.TryParse(acc.Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v))
                            {
                                last = v; found = true;
                            }
                            acc = "";
                        }
                    }
                }
                if (acc.Length > 0)
                {
                    float v = 0f;
                    if (float.TryParse(acc.Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v))
                    {
                        last = v; found = true;
                    }
                }
                if (found && last >= 0f && last <= 100f) return last;
            }
            catch { }
            return 0f;
        }

        // Try to parse patterns like "Water: 120 L / 500 L" and return percent (0..100)
        float ParseLitersPercentFromLine(string l)
        {
            try
            {
                // Normalize
                var s = l.Replace("\\", "/");
                if (s.IndexOf('L') < 0 && s.IndexOf('l') < 0) return 0f;
                // Find two numbers around a '/'
                int slash = s.IndexOf('/');
                if (slash <= 0) return 0f;
                // Extract left number
                float left = ExtractTrailingNumber(s, slash - 1);
                // Extract right number
                float right = ExtractLeadingNumber(s, slash + 1);
                if (left > 0f && right > 0f)
                {
                    float pct = (left / right) * 100f;
                    if (pct >= 0f && pct <= 10000f) return pct; // guard
                }
            }
            catch { }
            return 0f;
        }

        float ExtractTrailingNumber(string s, int start)
        {
            string num = "";
            for (int i = start; i >= 0; i--)
            {
                char c = s[i];
                if ((c >= '0' && c <= '9') || c == '.' || c == ',') num = c + num;
                else if (num.Length > 0) break;
            }
            float v = 0f;
            float.TryParse(num.Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v);
            return v;
        }

        float ExtractLeadingNumber(string s, int start)
        {
            string num = "";
            for (int i = start; i < s.Length; i++)
            {
                char c = s[i];
                if ((c >= '0' && c <= '9') || c == '.' || c == ',') num += c;
                else if (num.Length > 0) break;
            }
            float v = 0f;
            float.TryParse(num.Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v);
            return v;
        }

        // Parse water liters from DetailedInfo like "Water: 120 L / 500 L".
        // Returns true if both current and max liters were found.
        bool TryParseWaterLitersFromInfo(string info, out float currentL, out float maxL)
        {
            currentL = 0f; maxL = 0f;
            try
            {
                if (string.IsNullOrWhiteSpace(info)) return false;
                var lines = info.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    var l = lines[i].Trim();
                    if (l.Length == 0) continue;
                    var low = l.ToLower();
                    if ((low.Contains("water") || low.Contains("h2o") || low.Contains("liquid") || low.Contains("reservoir") || low.Contains("fluid"))
                        && (l.IndexOf('L') >= 0 || l.IndexOf('l') >= 0)
                        && l.IndexOf('/') >= 0)
                    {
                        // Extract numbers around '/'
                        var s = l.Replace("\\", "/");
                        int slash = s.IndexOf('/');
                        if (slash > 0)
                        {
                            float left = ExtractTrailingNumber(s, slash - 1);
                            float right = ExtractLeadingNumber(s, slash + 1);
                            if (right > 0f)
                            {
                                currentL = Math.Max(0f, left);
                                maxL = Math.Max(0f, right);
                                return true;
                            }
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        // Get grid-wide water/HydroSolution stored in gas tanks (supports both vanilla Water and Apex Advanced HydroSolution)
        float GetGridWaterFromTanks(MyCubeGrid grid)
        {
            float totalWater = 0f;
            try
            {
                var fatBlocks = grid.GetFatBlocks();
                foreach (var block in fatBlocks)
                {
                    var tank = block as IMyGasTank;
                    if (tank == null) continue;

                    // Check if this tank stores Water gas
                    try
                    {
                        var cubeDef = MyDefinitionManager.Static.GetCubeBlockDefinition(tank.BlockDefinition) as MyGasTankDefinition;
                        if (cubeDef != null)
                        {
                            var gasSubtype = cubeDef.StoredGasId.SubtypeName ?? string.Empty;
                            if (gasSubtype.Equals("Water", StringComparison.OrdinalIgnoreCase) || 
                                gasSubtype.Equals("HydroSolution", StringComparison.OrdinalIgnoreCase))
                            {
                                totalWater += (float)(tank.FilledRatio * tank.Capacity);
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
            return totalWater;
        }

        // Get grid-wide water/HydroSolution tank capacity (supports both vanilla Water and Apex Advanced HydroSolution)
        float GetGridWaterTankCapacity(MyCubeGrid grid)
        {
            float totalCapacity = 0f;
            try
            {
                var fatBlocks = grid.GetFatBlocks();
                foreach (var block in fatBlocks)
                {
                    var tank = block as IMyGasTank;
                    if (tank == null) continue;

                    // Check if this tank stores Water gas
                    try
                    {
                        var cubeDef = MyDefinitionManager.Static.GetCubeBlockDefinition(tank.BlockDefinition) as MyGasTankDefinition;
                        if (cubeDef != null)
                        {
                            var gasSubtype = cubeDef.StoredGasId.SubtypeName ?? string.Empty;
                            if (gasSubtype.Equals("Water", StringComparison.OrdinalIgnoreCase) || 
                                gasSubtype.Equals("HydroSolution", StringComparison.OrdinalIgnoreCase))
                            {
                                totalCapacity += (float)tank.Capacity;
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
            return totalCapacity;
        }

        // Calculate grid-wide water/HydroSolution production and consumption by summing individual blocks
        void CalculateGridWaterFlow(MyCubeGrid grid, out float productionLPerMin, out float consumptionLPerMin)
        {
            productionLPerMin = 0f;
            consumptionLPerMin = 0f;
            
            try
            {
                var fatBlocks = grid.GetFatBlocks();
                foreach (var block in fatBlocks)
                {
                    if (block == null) continue;
                    var termBlock = block as IMyTerminalBlock;
                    if (termBlock == null || !termBlock.IsFunctional || !termBlock.IsWorking) continue;

                    var blockDef = MyDefinitionManager.Static.GetCubeBlockDefinition(termBlock.BlockDefinition);
                    if (blockDef == null) continue;
                    
                    var subtype = blockDef.Id.SubtypeName ?? "";
                    var lower = subtype.ToLower();

                    // Check for water/HydroSolution producers (O2/H2 generators, Irrigation Systems)
                    var oxygenGen = block as IMyGasGenerator;
                    if (oxygenGen != null)
                    {
                        var genDef = blockDef as MyOxygenGeneratorDefinition;
                        if (genDef != null && genDef.ProducedGases != null)
                        {
                            foreach (var gasInfo in genDef.ProducedGases)
                            {
                                // Support both vanilla Water and Apex Advanced HydroSolution
                                if (gasInfo.Id.SubtypeName.Equals("Water", StringComparison.OrdinalIgnoreCase) ||
                                    gasInfo.Id.SubtypeName.Equals("HydroSolution", StringComparison.OrdinalIgnoreCase))
                                {
                                    // Calculate production rate: ice/pellets consumption * ice-to-gas ratio
                                    float icePerSec = genDef.IceConsumptionPerSecond;
                                    float ratio = gasInfo.IceToGasRatio;
                                    productionLPerMin += (icePerSec * ratio) * 60f;
                                    break;
                                }
                            }
                        }
                    }

                    // Check for water/HydroSolution consumers (farm plots)
                    if (lower.Contains("farmplot") || lower.Contains("farm_block") || lower.Contains("farmblock"))
                    {
                        // Parse water/HydroSolution usage from farm plot's DetailedInfo
                        var info = termBlock.DetailedInfo ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(info))
                        {
                            var lines = info.Split('\n');
                            foreach (var line in lines)
                            {
                                var l = line.Trim().ToLower();
                                // Match both "water" and "hydrosolution" consumption lines
                                if ((l.Contains("water") || l.Contains("hydrosolution")) && l.Contains("l/min"))
                                {
                                    var match = System.Text.RegularExpressions.Regex.Match(line, @"(\d+\.?\d*)\s*l/min", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                                    if (match.Success)
                                    {
                                        float usage;
                                        if (float.TryParse(match.Groups[1].Value, out usage))
                                        {
                                            consumptionLPerMin += usage;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        public override void Dispose()
        {
        }
    }
}
