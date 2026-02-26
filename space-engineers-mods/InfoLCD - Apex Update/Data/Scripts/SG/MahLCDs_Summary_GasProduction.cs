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
    [MyTextSurfaceScript("LCDInfoScreenGasGenerationSummary", "$IOS LCD - Gas Production")]
    public class LCDGasGenerationSummary : MyTextSurfaceScriptBase
    {
        MyIni config = new MyIni();

        public static string CONFIG_SECTION_ID = "SettingsGasGenerationStatus";

        string searchId = "*";
        bool showHydrogen = true;
        bool showOxygen = true;
        bool showIce = true;
        bool showGenerators = true;
        bool showOxygenFarms = true;
    // User-configurable Ice target (item count), defaults to 50000
    int iceMinAmount = 50000;

        void TryCreateSurfaceData()
        {
            if (surfaceData != null)
                return;

            compactMode = mySurface.SurfaceSize.X / mySurface.SurfaceSize.Y > 4;
            var textSize = compactMode ? .4f : .35f;

            surfaceData = new SurfaceDrawer.SurfaceData
            {
                surface = mySurface,
                textSize = textSize,
                titleOffset = compactMode ? 220 : 128,
                ratioOffset = compactMode ? 180 : 104,
                viewPortOffsetX = 10,
                viewPortOffsetY = 10,
                newLine = new Vector2(0, 30 * textSize),
                showHeader = true,
                showSummary = false,
                showMissing = false,
                showRatio = false,
                showBars = true,
                showSubgrids = false,
                showDocked = false,
                useColors = true
            };
        }

        void CreateConfig()
        {
            TryCreateSurfaceData();

            config.Clear();

            // Determine default IceMinAmount from definitions
            var iceDef = MahDefinitions.GetDefinition("Ore", "Ice");
            iceMinAmount = (iceMinAmount > 0) ? iceMinAmount : (iceDef != null && iceDef.minAmount > 0 ? iceDef.minAmount : 50000);

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
            sb.AppendLine("; [ GASPRODUCTION - GENERAL OPTIONS ]");
            ConfigHelpers.AppendSearchIdConfig(sb, searchId);
            ConfigHelpers.AppendExcludeIdsConfig(sb, excludeIds, "Airlock,");
            ConfigHelpers.AppendShowHeaderConfig(sb, surfaceData.showHeader);
            ConfigHelpers.AppendShowRatioConfig(sb, surfaceData.showRatio);
            ConfigHelpers.AppendShowBarsConfig(sb, surfaceData.showBars);
            ConfigHelpers.AppendShowSubgridsConfig(sb, surfaceData.showSubgrids);
            ConfigHelpers.AppendSubgridUpdateFrequencyConfig(sb, surfaceData.subgridUpdateFrequency);
            ConfigHelpers.AppendShowDockedConfig(sb, surfaceData.showDocked);
            ConfigHelpers.AppendUseColorsConfig(sb, surfaceData.useColors);

            sb.AppendLine();
            sb.AppendLine("; [ GASPRODUCTION - LAYOUT OPTIONS ]");
            sb.AppendLine($"TextSize={surfaceData.textSize}");
            sb.AppendLine($"ViewPortOffsetX={surfaceData.viewPortOffsetX}");
            sb.AppendLine($"ViewPortOffsetY={surfaceData.viewPortOffsetY}");
            sb.AppendLine($"TitleFieldWidth={surfaceData.titleOffset}");
            sb.AppendLine($"RatioFieldWidth={surfaceData.ratioOffset}");

            sb.AppendLine();
            sb.AppendLine("; [ GASPRODUCTION - SCREEN OPTIONS ]");
            sb.AppendLine($"ShowHydrogen={showHydrogen}");
            sb.AppendLine($"ShowOxygen={showOxygen}");
            sb.AppendLine($"ShowIce={showIce}");
            sb.AppendLine($"ShowGenerators={showGenerators}");
            sb.AppendLine($"ShowOxygenFarms={showOxygenFarms}");
            sb.AppendLine($"ShowIntakeVent={surfaceData.showIntakeVent}");

            sb.AppendLine();
            sb.AppendLine("; [ GASPRODUCTION - ITEM THRESHOLDS ]");
            sb.AppendLine($"IceMinAmount={iceMinAmount}");

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
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowRatio", ref surfaceData.showRatio, ref configError);
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowBars", ref surfaceData.showBars, ref configError);
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowSubgrids", ref surfaceData.showSubgrids, ref configError);
                    if (config.ContainsKey(CONFIG_SECTION_ID, "SubgridUpdateFrequency")) surfaceData.subgridUpdateFrequency = config.Get(CONFIG_SECTION_ID, "SubgridUpdateFrequency").ToInt32();
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowDocked", ref surfaceData.showDocked, ref configError);

                    if (config.ContainsKey(CONFIG_SECTION_ID, "TextSize"))
                        surfaceData.textSize = config.Get(CONFIG_SECTION_ID, "TextSize").ToSingle(defaultValue: 1.0f);
                    else
                        configError = true;

                    surfaceData.newLine = new Vector2(0, 30 * surfaceData.textSize);

                    MahUtillities.TryGetConfigFloat(config, CONFIG_SECTION_ID, "TitleFieldWidth", ref surfaceData.titleOffset, ref configError);
                    MahUtillities.TryGetConfigFloat(config, CONFIG_SECTION_ID, "RatioFieldWidth", ref surfaceData.ratioOffset, ref configError);
                    MahUtillities.TryGetConfigFloat(config, CONFIG_SECTION_ID, "ViewPortOffsetX", ref surfaceData.viewPortOffsetX, ref configError);
                    MahUtillities.TryGetConfigFloat(config, CONFIG_SECTION_ID, "ViewPortOffsetY", ref surfaceData.viewPortOffsetY, ref configError);

                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowHydrogen", ref showHydrogen, ref configError);
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowOxygen", ref showOxygen, ref configError);
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowIce", ref showIce, ref configError);
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowGenerators", ref showGenerators, ref configError);
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowOxygenFarms", ref showOxygenFarms, ref configError);

                    // Optional: show intake vents section (default true)
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowIntakeVent"))
                        surfaceData.showIntakeVent = config.Get(CONFIG_SECTION_ID, "ShowIntakeVent").ToBoolean();

                    // Optional: user-defined ice target (item count)
                    if (config.ContainsKey(CONFIG_SECTION_ID, "IceMinAmount"))
                        iceMinAmount = config.Get(CONFIG_SECTION_ID, "IceMinAmount").ToInt32();
                    else
                    {
                        var iceDef2 = MahDefinitions.GetDefinition("Ore", "Ice");
                        iceMinAmount = (iceDef2 != null && iceDef2.minAmount > 0) ? iceDef2.minAmount : iceMinAmount;
                    }

                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "UseColors", ref surfaceData.useColors, ref configError);

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
                        surfaceData.textSize = 0.4f;
                        surfaceData.titleOffset = 220;
                        surfaceData.ratioOffset = 180;
                        surfaceData.viewPortOffsetX = 10;
                        surfaceData.viewPortOffsetY = 5;
                    }
                }
                else
                {
                    MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenGasGenerationSummary: Config Syntax error at Line {result}");
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenGasGenerationSummary: Caught Exception while loading config: {e.ToString()}");
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

                if (String.IsNullOrEmpty(t) || t == "*" || t == "" || t.Length < 3) continue;

                excludeIds.Add(t);
            }
        }

        IMyTextSurface mySurface;
        IMyTerminalBlock myTerminalBlock;

        List<string> excludeIds = new List<string>();
    List<IMyOxygenFarm> oxygenFarms = new List<IMyOxygenFarm>();
    List<IMyGasGenerator> generators = new List<IMyGasGenerator>();
    List<IMyGasTank> tanks = new List<IMyGasTank>();
    List<IMyAirVent> airVents = new List<IMyAirVent>();

        // Cached subgrid collections
        List<IMyOxygenFarm> subgridOxygenFarms = new List<IMyOxygenFarm>();
        List<IMyGasGenerator> subgridGenerators = new List<IMyGasGenerator>();
        List<IMyGasTank> subgridTanks = new List<IMyGasTank>();
        List<IMyAirVent> subgridAirVents = new List<IMyAirVent>();

    // Reusable lists to avoid GC allocations
    List<VRage.Game.ModAPI.Ingame.MyInventoryItem> _cachedInventoryItems = new List<VRage.Game.ModAPI.Ingame.MyInventoryItem>();

        VRage.Collections.DictionaryValuesReader<MyDefinitionId, MyDefinitionBase> myDefinitions;
        MyDefinitionId myDefinitionId;
        SurfaceDrawer.SurfaceData surfaceData;
        string gridId = "Unknown grid";
        int subgridScanTick = 0;
        float textSize = 1.0f;

        float reactorsCurrentVolume = 0.0f;
        float reactorsMaximumVolume = 0.0f;
    // Ice volumes stored in liters (L) for both current and maximum
    float currentIceVolume = 0.0f;
    float maximumIceVolume = 0.0f;

        int hydrogenTanks = 0;
        int oxygenTanks = 0;
        bool configError = false;
        bool compactMode = false;
        bool isStation = false;
        Sandbox.ModAPI.Ingame.MyShipMass gridMass;

        public LCDGasGenerationSummary(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
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
            
            UpdateBlocks();
            UpdateIceContents();

            var myFrame = mySurface.DrawFrame();
            var myViewport = new RectangleF((mySurface.TextureSize - mySurface.SurfaceSize) / 2f, mySurface.SurfaceSize);
            var myPosition = new Vector2(surfaceData.viewPortOffsetX, surfaceData.viewPortOffsetY) + myViewport.Position;
            myDefinitions = MyDefinitionManager.Static.GetAllDefinitions();
            
            if (configError)
                SurfaceDrawer.DrawErrorSprite(ref myFrame, surfaceData, $"<< Config error. Please Delete CustomData >>", Color.Orange);
            else
            {
                DrawGasProductionMainSprite(ref myFrame, ref myPosition);
            }

            myFrame.Dispose();
        }

        void UpdateBlocks ()
        {
            try
            {
                var myCubeGrid = myTerminalBlock.CubeGrid as MyCubeGrid;
                if (myCubeGrid == null) return;

                IMyCubeGrid cubeGrid = myCubeGrid as IMyCubeGrid;
                isStation = cubeGrid.IsStatic;
                gridId = cubeGrid.CustomName;

                // Determine if we should scan subgrids on this tick
                bool scanSubgrids = false;
                if (surfaceData.showSubgrids)
                {
                    subgridScanTick++;
                    if (subgridScanTick >= surfaceData.subgridUpdateFrequency / 10)  // Divide by 10 for Update10 timing
                    {
                        subgridScanTick = 0;
                        scanSubgrids = true;
                    }
                }

                // Always scan main grid blocks (instant updates)
                var mainBlocks = MahUtillities.GetBlocks(myCubeGrid, searchId, excludeIds, ref gridMass, false);

                // Periodically update subgrid cache
                if (scanSubgrids)
                {
                    var allBlocks = MahUtillities.GetBlocks(myCubeGrid, searchId, excludeIds, ref gridMass, surfaceData.showSubgrids);
                    
                    // Extract subgrid-only blocks
                    subgridOxygenFarms.Clear();
                    subgridGenerators.Clear();
                    subgridTanks.Clear();
                    subgridAirVents.Clear();

                    foreach (var block in allBlocks)
                    {
                        if (!mainBlocks.Contains(block))
                        {
                            if (block is IMyGasGenerator)
                            {
                                var gen = (IMyGasGenerator)block;
                                string name = gen.CustomName ?? string.Empty;
                                string subtype = gen.BlockDefinition.SubtypeName ?? string.Empty;
                                if (name.IndexOf("irrigation", StringComparison.OrdinalIgnoreCase) >= 0 || subtype.IndexOf("irrigation", StringComparison.OrdinalIgnoreCase) >= 0)
                                    continue; // exclude irrigation system blocks
                                subgridGenerators.Add(gen);
                            }
                            else if (block is IMyGasTank)
                                subgridTanks.Add((IMyGasTank)block);
                            else if (block is IMyOxygenFarm)
                                subgridOxygenFarms.Add((IMyOxygenFarm)block);
                            else if (block is IMyAirVent)
                                subgridAirVents.Add((IMyAirVent)block);
                        }
                    }
                }

                // Categorize main grid blocks
                oxygenFarms.Clear();
                generators.Clear();
                tanks.Clear();
                airVents.Clear();

                foreach (var myBlock in mainBlocks)
                {
                    if (myBlock == null) continue;

                    if (myBlock is IMyGasGenerator)
                    {
                        var gen = (IMyGasGenerator)myBlock;
                        string name = gen.CustomName ?? string.Empty;
                        string subtype = gen.BlockDefinition.SubtypeName ?? string.Empty;
                        if (name.IndexOf("irrigation", StringComparison.OrdinalIgnoreCase) >= 0 || subtype.IndexOf("irrigation", StringComparison.OrdinalIgnoreCase) >= 0)
                            continue; // exclude irrigation system blocks
                        generators.Add(gen);
                    }
                    else if (myBlock is IMyGasTank)
                    {
                        tanks.Add((IMyGasTank)myBlock);
                    }
                    else if (myBlock is IMyOxygenFarm)
                    {
                        oxygenFarms.Add((IMyOxygenFarm)myBlock);
                    }
                    else if (myBlock is IMyAirVent)
                    {
                        airVents.Add((IMyAirVent)myBlock);
                    }
                }

                // Merge main (fresh) and subgrid (cached) collections
                oxygenFarms.AddRange(subgridOxygenFarms);
                generators.AddRange(subgridGenerators);
                tanks.AddRange(subgridTanks);
                airVents.AddRange(subgridAirVents);

                // Separate tank counts
                oxygenTanks = 0;
                hydrogenTanks = 0;
                var separatedTanks = MahUtillities.SeparateGasTanks(tanks);
                hydrogenTanks = separatedTanks.HydrogenCount;
                oxygenTanks = separatedTanks.OxygenCount;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenGasGenerationSummary: Caught Exception while updating blocks: {e.ToString()}");
            }
        }

        void UpdateIceContents ()
        {
            try
            {
                // Get definition (volume is in m^3) and convert to liters ( *1000 )
                CargoItemDefinition iceDefinition = MahDefinitions.GetDefinition("Ore", "Ice");
                float iceItemVolumeL = 0f;
                if (iceDefinition != null)
                    iceItemVolumeL = iceDefinition.volume; // volume is already in liters

                // Use consolidated ice collection utility
                var myCubeGrid = myTerminalBlock.CubeGrid as MyCubeGrid;
                if (myCubeGrid != null)
                {
                    Sandbox.ModAPI.Ingame.MyShipMass tmpMass = gridMass; // unused copy
                    var allBlocks = MahUtillities.GetBlocks(myCubeGrid, searchId, excludeIds, ref tmpMass, surfaceData.showSubgrids);
                    if (allBlocks != null)
                    {
                        var iceData = MahUtillities.GetGridIceData(allBlocks, iceItemVolumeL, _cachedInventoryItems);
                        currentIceVolume = iceData.currentIceVolumeL;
                        maximumIceVolume = iceData.maxIceVolumeL;
                        // iceData.iceItemCount = item count (unused here)
                    }
                    else
                    {
                        currentIceVolume = 0.0f;
                        maximumIceVolume = 0.0f;
                    }
                }
                else
                {
                    currentIceVolume = 0.0f;
                    maximumIceVolume = 0.0f;
                }

                // Fallback: if we somehow have generators but no capacity accumulated, use their inventories to establish maximum
                if (maximumIceVolume <= 0f && generators.Count > 0)
                {
                    foreach (var g in generators)
                    {
                        if (g == null) continue;
                        maximumIceVolume += (float)g.GetInventory(0).MaxVolume * 1000f;
                    }
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenGasGenerationSummary: Caught Exception while updating ice contents: {e.ToString()}");
            }
        }

        void DrawGasProductionMainSprite (ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            try
            {
                if (surfaceData.showHeader)
                {
                    SurfaceDrawer.DrawHeader(ref frame, ref position, surfaceData, $"Gas Production Summary: [{(searchId == "*" ? "All" : searchId)} -{excludeIds.Count}]");
                    if (compactMode) DrawGasProductionCompactSprite(ref frame, ref position);
                }

                if (tanks.Count > 0)
                {
                    tanks.RemoveAll(t => t == null);

                    if (showHydrogen)
                        SurfaceDrawer.DrawGasTankSprite(ref frame, ref position, surfaceData, "Hydrogen", $"HYD ({hydrogenTanks.ToString()})", tanks, true);
                    if (showOxygen)
                        SurfaceDrawer.DrawGasTankSprite(ref frame, ref position, surfaceData, "Oxygen", $"OXY ({oxygenTanks.ToString()})", tanks, true);
                }

                if (compactMode) return;

                if (generators.Count > 0)
                {
                    if (showIce)
                    {
                        // Use Ice minAmount (user-configurable) as the 100% target
                        float targetIceLiters = maximumIceVolume;
                        CargoItemDefinition iceDef = MahDefinitions.GetDefinition("Ore", "Ice");
                        int targetItems = (iceMinAmount > 0 ? iceMinAmount : (iceDef != null ? iceDef.minAmount : 50000));
                        if (iceDef != null && targetItems > 0)
                        {
                            float itemL = iceDef.volume; // volume is already in liters
                            targetIceLiters = targetItems * itemL;
                        }

                        if (surfaceData.showBars)
                        {
                            SurfaceDrawer.DrawBarFixedColor(ref frame, ref position, surfaceData, "Ice", currentIceVolume, targetIceLiters, Color.Aquamarine, Unit.Liters);
                        }
                        else
                        {
                            SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Ice", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                            SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{MahDefinitions.LiterFormat(currentIceVolume)}", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                            position += surfaceData.newLine;
                        }
                        position += surfaceData.newLine;
                    }

                    if (showGenerators)
                        SurfaceDrawer.DrawGasGeneratorSummarySprite(ref frame, ref position, surfaceData, generators);
                }

                if (showOxygenFarms)
                    SurfaceDrawer.DrawOxygenFarmSummarySprite(ref frame, ref position, surfaceData, oxygenFarms);

                // Intake Vents section
                if (surfaceData.showIntakeVent)
                    DrawIntakeVentsSummarySprite(ref frame, ref position);

                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenGasGenerationSummary: Caught Exception while DrawGasGenerationMainSprite: {e.ToString()}");
            }
        }

        void DrawGasProductionCompactSprite (ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            position -= surfaceData.newLine;

            if (generators.Count > 0 && showGenerators)
            {
                // Generators
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Generators [{generators.Count}]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                if (surfaceData.showBars)
                {
                    // Ice Bar
                    float targetIceLiters = maximumIceVolume;
                    CargoItemDefinition iceDef = MahDefinitions.GetDefinition("Ore", "Ice");
                    int targetItems = (iceMinAmount > 0 ? iceMinAmount : (iceDef != null ? iceDef.minAmount : 0));
                    if (iceDef != null && targetItems > 0)
                    {
                        float itemL = iceDef.volume; // volume is already in liters
                        targetIceLiters = targetItems * itemL;
                    }
                    SurfaceDrawer.DrawHalfBar(ref frame, position, surfaceData, TextAlignment.RIGHT, currentIceVolume, targetIceLiters, surfaceData.showRatio ? Unit.Percent : Unit.Liters, Color.Aquamarine);
                }
                else
                {
                    float targetIceLiters = maximumIceVolume;
                    CargoItemDefinition iceDef = MahDefinitions.GetDefinition("Ore", "Ice");
                    int targetItems = (iceMinAmount > 0 ? iceMinAmount : (iceDef != null ? iceDef.minAmount : 0));
                    if (iceDef != null && targetItems > 0)
                    {
                        float itemL = iceDef.volume; // volume is already in liters
                        targetIceLiters = targetItems * itemL;
                    }
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{(surfaceData.showRatio ? (currentIceVolume / (targetIceLiters <= 0 ? 1 : targetIceLiters) * 100).ToString("0.##") + " %" : (MahDefinitions.LiterFormat(currentIceVolume)))}", TextAlignment.RIGHT, surfaceData.useColors ? Color.GreenYellow : surfaceData.surface.ScriptForegroundColor);
                }
            }
            position += surfaceData.newLine;

            if (oxygenFarms.Count > 0 && showOxygenFarms)
            {
                // Oxygen Farms
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Oxygen Farms [{oxygenFarms.Count}]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                var outputOverall = 0.0f;
                foreach (var farm in oxygenFarms)
                {
                    if (farm == null) continue;

                    var name = farm.CustomName;
                    var currentOutputString = farm.DetailedInfo.Split('\n')[2].Replace("Oxygen Output:", "").Replace("L/min", "").Trim();

                    var currentOutput = 0.0f;
                    float.TryParse(currentOutputString, out currentOutput);
                    outputOverall += currentOutput;
                }

                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{MahDefinitions.LiterFormat(outputOverall)}/min", TextAlignment.RIGHT, surfaceData.useColors ? Color.GreenYellow : surfaceData.surface.ScriptForegroundColor);
            }

            position += surfaceData.newLine;
        }

        void DrawIntakeVentsSummarySprite(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            try
            {
                // Count intake vents without allocating new list
                int intakeCount = 0;
                foreach (var v in airVents)
                {
                    if (v != null && v.Depressurize) intakeCount++;
                }

                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Intake Vents [{intakeCount}]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"State        ", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine;

                if (intakeCount == 0)
                {
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, "- None", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    position += surfaceData.newLine + surfaceData.newLine;
                    return;
                }

                // Sort intake vents alphabetically by custom name
                MahSorting.SortBlocksByName(airVents);

                foreach (var vent in airVents)
                {
                    if (vent == null || !vent.Depressurize) continue;
                    var name = vent.CustomName;
                    // Left-side state badge (On/Off) like other Gas Production entries
                    var state = vent.IsWorking ? "    On" : "    Off";
                    var stateColor = !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : (vent.IsWorking ? Color.GreenYellow : Color.Orange);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, state, TextAlignment.LEFT, stateColor);
                    // Use the same badge+name pattern as other Gas Production rows (no special truncation here)
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"[          ] {name}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);

                    string label = "[  AIR INTAKE  ]";
                    // draw shell in white
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, label, TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                    // overlay colored inner text only
                    string overlay = label.Replace('[',' ').Replace(']',' ');
                    var color = surfaceData.useColors ? Color.Aqua : surfaceData.surface.ScriptForegroundColor;
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, overlay, TextAlignment.RIGHT, color);

                    position += surfaceData.newLine;
                }

                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenGasGenerationSummary: Caught Exception while DrawIntakeVentsSummarySprite: {e.ToString()}");
            }
        }
    }
}
