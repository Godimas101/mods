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
    [MyTextSurfaceScript("LCDInfoScreenProductionSummary", "$IOS LCD - Production")]
    public class LCDProductionSummary : MyTextSurfaceScriptBase
    {
        MyIni config = new MyIni();

        public static string CONFIG_SECTION_ID = "SettingsProductionStatus";

        string searchId = "*";
        bool showInactive = false;
        bool showRefineries = true;
        bool showAssemblers = true;
        bool showGenerators = true;
        bool showOxygenFarms = true;
    // New categories
    bool showFoodProcessors = true;
    bool showIrrigationSystems = true;
    bool showAlgaeFarms = true;

        void TryCreateSurfaceData()
        {
            if (surfaceData != null)
                return;

            compactMode = mySurface.SurfaceSize.X / mySurface.SurfaceSize.Y > 4;
            var textSize = compactMode ? .45f : .35f;

            // Initialize surface settings
            surfaceData = new SurfaceDrawer.SurfaceData
            {
                surface = mySurface,
                textSize = textSize,
                titleOffset = 104,
                ratioOffset = 104,
                viewPortOffsetX = 10,
                viewPortOffsetY = 10,
                newLine = new Vector2(0, 30 * textSize),
                showHeader = true,
                showSummary = true,
                showMissing = false,
                showRatio = true,
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
            sb.AppendLine("; [ PRODUCTION - GENERAL OPTIONS ]");
            ConfigHelpers.AppendSearchIdConfig(sb, searchId);
            ConfigHelpers.AppendExcludeIdsConfig(sb, excludeIds);
            ConfigHelpers.AppendShowHeaderConfig(sb, surfaceData.showHeader);
            ConfigHelpers.AppendShowSubgridsConfig(sb, surfaceData.showSubgrids);
            ConfigHelpers.AppendSubgridUpdateFrequencyConfig(sb, surfaceData.subgridUpdateFrequency);
            ConfigHelpers.AppendShowDockedConfig(sb, surfaceData.showDocked);
            ConfigHelpers.AppendUseColorsConfig(sb, surfaceData.useColors);

            sb.AppendLine();
            sb.AppendLine("; [ PRODUCTION - LAYOUT OPTIONS ]");
            sb.AppendLine($"TextSize={surfaceData.textSize}");
            sb.AppendLine($"ViewPortOffsetX={surfaceData.viewPortOffsetX}");
            sb.AppendLine($"ViewPortOffsetY={surfaceData.viewPortOffsetY}");
            sb.AppendLine($"TitleFieldWidth={surfaceData.titleOffset}");
            sb.AppendLine($"RatioFieldWidth={surfaceData.ratioOffset}");

            sb.AppendLine();
            sb.AppendLine("; [ PRODUCTION - SCREEN OPTIONS ]");
            sb.AppendLine($"ShowRefineries={showRefineries}");
            sb.AppendLine($"ShowAssemblers={showAssemblers}");
            sb.AppendLine($"ShowGenerators={showGenerators}");
            sb.AppendLine($"ShowOxygenFarms={showOxygenFarms}");
            sb.AppendLine($"ShowFoodProcessors={showFoodProcessors}");
            sb.AppendLine($"ShowIrrigationSystems={showIrrigationSystems}");
            sb.AppendLine($"ShowAlgaeFarms={showAlgaeFarms}");

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

                    if (config.ContainsKey(CONFIG_SECTION_ID, "SearchId"))
                    {
                        searchId = config.Get(CONFIG_SECTION_ID, "SearchId").ToString();
                        searchId = string.IsNullOrWhiteSpace(searchId) ? "*" : searchId;
                    }
                    else
                        configError = true;

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

                    surfaceData.newLine = new Vector2(0, 30 * surfaceData.textSize);

                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowRefineries", ref showRefineries, ref configError);
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowAssemblers", ref showAssemblers, ref configError);
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowGenerators", ref showGenerators, ref configError);
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowOxygenFarms", ref showOxygenFarms, ref configError);

                    // New optional toggles: do not flag config error if absent (maintains backward compatibility)
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowFoodProcessors"))
                        showFoodProcessors = config.Get(CONFIG_SECTION_ID, "ShowFoodProcessors").ToBoolean();
                    else
                        showFoodProcessors = true;

                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowIrrigationSystems"))
                        showIrrigationSystems = config.Get(CONFIG_SECTION_ID, "ShowIrrigationSystems").ToBoolean();
                    else
                        showIrrigationSystems = true;

                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowAlgaeFarms"))
                        showAlgaeFarms = config.Get(CONFIG_SECTION_ID, "ShowAlgaeFarms").ToBoolean();
                    else
                        showAlgaeFarms = true;

                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "UseColors", ref surfaceData.useColors, ref configError);

                    CreateExcludeIdsList();

                    // Is Corner LCD?
                    if (compactMode)
                    {
                        surfaceData.showHeader = true;
                        surfaceData.showSummary = true;
                        surfaceData.textSize = 0.45f;
                        surfaceData.titleOffset = 220;
                        surfaceData.ratioOffset = 180;
                        surfaceData.viewPortOffsetX = 10;
                        surfaceData.viewPortOffsetY = 5;
                    }
                }
                else
                {
                    MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenProductionSummary: Config Syntax error at Line {result}");
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenProductionSummary: Caught Exception while loading config: {e.ToString()}");
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
        List<IMyRefinery> refineries = new List<IMyRefinery>();
        List<IMyAssembler> assemblers = new List<IMyAssembler>();
        List<IMyGasGenerator> generators = new List<IMyGasGenerator>();

    List<IMyAssembler> foodProcessors = new List<IMyAssembler>();
    List<IMyGasGenerator> irrigationSystems = new List<IMyGasGenerator>();
    List<IMyTerminalBlock> algaeFarms = new List<IMyTerminalBlock>();

        // Cached subgrid collections
        List<IMyOxygenFarm> subgridOxygenFarms = new List<IMyOxygenFarm>();
        List<IMyRefinery> subgridRefineries = new List<IMyRefinery>();
        List<IMyAssembler> subgridAssemblers = new List<IMyAssembler>();
        List<IMyGasGenerator> subgridGenerators = new List<IMyGasGenerator>();
        List<IMyAssembler> subgridFoodProcessors = new List<IMyAssembler>();
        List<IMyGasGenerator> subgridIrrigationSystems = new List<IMyGasGenerator>();
        List<IMyTerminalBlock> subgridAlgaeFarms = new List<IMyTerminalBlock>();

        VRage.Collections.DictionaryValuesReader<MyDefinitionId, MyDefinitionBase> myDefinitions;
        MyDefinitionId myDefinitionId;
        SurfaceDrawer.SurfaceData surfaceData;
        string gridId = "Unknown grid";
        int subgridScanTick = 0;
        bool configError = false;
        bool compactMode = false;
        bool isStation = false;
        Sandbox.ModAPI.Ingame.MyShipMass gridMass;

        public LCDProductionSummary(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
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

            var myFrame = mySurface.DrawFrame();
            var myViewport = new RectangleF((mySurface.TextureSize - mySurface.SurfaceSize) / 2f, mySurface.SurfaceSize);
            var myPosition = new Vector2(surfaceData.viewPortOffsetX, surfaceData.viewPortOffsetY) + myViewport.Position;
            myDefinitions = MyDefinitionManager.Static.GetAllDefinitions();

            if (configError)
                SurfaceDrawer.DrawErrorSprite(ref myFrame, surfaceData, $"<< Config error. Please Delete CustomData >>", Color.Orange);
            else
            {
                DrawProductionMainSprite(ref myFrame, ref myPosition);
            }

            myFrame.Dispose();
        }

        void UpdateBlocks()
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

                // Periodically update subgrid cache
                if (scanSubgrids)
                {
                    var allBlocks = MahUtillities.GetBlocks(myCubeGrid, searchId, excludeIds, ref gridMass, surfaceData.showSubgrids, surfaceData.showDocked);
                    
                    // Extract subgrid-only blocks
                    var subgridOnlyBlocks = new List<IMyCubeBlock>();
                    foreach (var block in allBlocks)
                        if (!mainBlocks.Contains(block))
                            subgridOnlyBlocks.Add(block);

                    // Categorize subgrid blocks
                    subgridOxygenFarms.Clear();
                    subgridRefineries.Clear();
                    subgridAssemblers.Clear();
                    subgridGenerators.Clear();
                    subgridFoodProcessors.Clear();
                    subgridIrrigationSystems.Clear();
                    subgridAlgaeFarms.Clear();

                    foreach (var myBlock in subgridOnlyBlocks)
                    {
                        if (myBlock == null) continue;

                        // Detect Algae Farms first so they don't fall into other categories
                        if (IsAlgaeFarm(myBlock as IMyTerminalBlock))
                        {
                            subgridAlgaeFarms.Add((IMyTerminalBlock)myBlock);
                        }
                        else if (myBlock is IMyRefinery)
                        {
                            subgridRefineries.Add((IMyRefinery)myBlock);
                        }
                        else if (myBlock is IMyAssembler)
                        {
                            var asm = (IMyAssembler)myBlock;
                            if (IsFoodProcessor(myBlock as IMyTerminalBlock))
                                subgridFoodProcessors.Add(asm);
                            else
                                subgridAssemblers.Add(asm);
                        }
                        else if (myBlock is IMyGasGenerator)
                        {
                            var gen = (IMyGasGenerator)myBlock;
                            if (IsIrrigationSystem(myBlock as IMyTerminalBlock))
                                subgridIrrigationSystems.Add(gen);
                            else
                                subgridGenerators.Add(gen);
                        }
                        else if (myBlock is IMyOxygenFarm)
                        {
                            subgridOxygenFarms.Add((IMyOxygenFarm)myBlock);
                        }
                    }
                }

                // Categorize main grid blocks
                var mainOxygenFarms = new List<IMyOxygenFarm>();
                var mainRefineries = new List<IMyRefinery>();
                var mainAssemblers = new List<IMyAssembler>();
                var mainGenerators = new List<IMyGasGenerator>();
                var mainFoodProcessors = new List<IMyAssembler>();
                var mainIrrigationSystems = new List<IMyGasGenerator>();
                var mainAlgaeFarms = new List<IMyTerminalBlock>();

                foreach (var myBlock in mainBlocks)
                {
                    if (myBlock == null) continue;

                    // Detect Algae Farms first so they don't fall into other categories
                    if (IsAlgaeFarm(myBlock as IMyTerminalBlock))
                    {
                        mainAlgaeFarms.Add((IMyTerminalBlock)myBlock);
                    }
                    else if (myBlock is IMyRefinery)
                    {
                        mainRefineries.Add((IMyRefinery)myBlock);
                    }
                    else if (myBlock is IMyAssembler)
                    {
                        var asm = (IMyAssembler)myBlock;
                        if (IsFoodProcessor(myBlock as IMyTerminalBlock))
                            mainFoodProcessors.Add(asm);
                        else
                            mainAssemblers.Add(asm);
                    }
                    else if (myBlock is IMyGasGenerator)
                    {
                        var gen = (IMyGasGenerator)myBlock;
                        if (IsIrrigationSystem(myBlock as IMyTerminalBlock))
                            mainIrrigationSystems.Add(gen);
                        else
                            mainGenerators.Add(gen);
                    }
                    else if (myBlock is IMyOxygenFarm)
                    {
                        mainOxygenFarms.Add((IMyOxygenFarm)myBlock);
                    }
                }

                // Merge main (fresh) and subgrid (cached) collections
                oxygenFarms.Clear();
                oxygenFarms.AddRange(mainOxygenFarms);
                oxygenFarms.AddRange(subgridOxygenFarms);

                refineries.Clear();
                refineries.AddRange(mainRefineries);
                refineries.AddRange(subgridRefineries);

                assemblers.Clear();
                assemblers.AddRange(mainAssemblers);
                assemblers.AddRange(subgridAssemblers);

                generators.Clear();
                generators.AddRange(mainGenerators);
                generators.AddRange(subgridGenerators);

                foodProcessors.Clear();
                foodProcessors.AddRange(mainFoodProcessors);
                foodProcessors.AddRange(subgridFoodProcessors);

                irrigationSystems.Clear();
                irrigationSystems.AddRange(mainIrrigationSystems);
                irrigationSystems.AddRange(subgridIrrigationSystems);

                algaeFarms.Clear();
                algaeFarms.AddRange(mainAlgaeFarms);
                algaeFarms.AddRange(subgridAlgaeFarms);
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenProductionSummary: Caught Exception while updating blocks: {e.ToString()}");
            }
        }

        void DrawProductionMainSprite(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            try
            {
                if (compactMode)
                {
                    DrawCompactProductionSprite(ref frame, ref position);
                    return;
                }

                if (surfaceData.showHeader)
                    SurfaceDrawer.DrawHeader(ref frame, ref position, surfaceData, $"Production Summary [{(searchId == "*" ? "All" : searchId)} -{excludeIds.Count}]");

                if (showRefineries)
                    SurfaceDrawer.DrawRefinerySummarySprite(ref frame, ref position, surfaceData, refineries);
                if (showAssemblers)
                    SurfaceDrawer.DrawAssemblerSummarySprite(ref frame, ref position, surfaceData, assemblers);
                if (showFoodProcessors)
                    DrawFoodProcessorSummarySprite(ref frame, ref position);
                if (showGenerators)
                    SurfaceDrawer.DrawGasGeneratorSummarySprite(ref frame, ref position, surfaceData, generators);
                if (showIrrigationSystems)
                    DrawIrrigationSystemSummarySprite(ref frame, ref position);
                if (showOxygenFarms)
                    SurfaceDrawer.DrawOxygenFarmSummarySprite(ref frame, ref position, surfaceData, oxygenFarms);
                if (showAlgaeFarms)
                    DrawAlgaeFarmSummarySprite(ref frame, ref position);
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenProductionSummary: Caught Exception while DrawProductionMainSprite: {e.ToString()}");
            }
        }

        void DrawCompactProductionSprite(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            try
            {
                SurfaceDrawer.DrawHeader(ref frame, ref position, surfaceData, $"Production Summary [{(searchId == "*" ? "All" : searchId)} -{excludeIds.Count}]");
                position -= surfaceData.newLine;

                // Refineries
                var working = 0;
                if (refineries.Count > 0 && showRefineries)
                {
                    foreach(IMyRefinery refinery in refineries)
                        working += refinery.IsWorking ? 1 : 0;
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Refineries", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{working}          ", TextAlignment.RIGHT, surfaceData.useColors ? Color.GreenYellow : surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Active:              /      ", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"                       {refineries.Count}", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                    position += surfaceData.newLine;
                }

                // Assemblers
                if (assemblers.Count > 0 && showAssemblers)
                {
                    working = 0;
                    foreach (IMyAssembler assembler in assemblers)
                        working += assembler.IsWorking ? 1 : 0;
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Assemblers", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{working}          ", TextAlignment.RIGHT, surfaceData.useColors ? Color.GreenYellow : surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Active:              /      ", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"                       {assemblers.Count}", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                    position += surfaceData.newLine;
                }

                // H2/O2 Generators
                if (generators.Count > 0 && showGenerators)
                {
                    working = 0;
                    foreach (IMyGasGenerator generator in generators)
                        working += generator.IsWorking ? 1 : 0;
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"H2/O2 Generators", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{working}          ", TextAlignment.RIGHT, surfaceData.useColors ? Color.GreenYellow : surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Active:              /      ", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"                      {generators.Count}", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                    position += surfaceData.newLine;
                }

                // Irrigation Systems
                if (irrigationSystems.Count > 0 && showIrrigationSystems)
                {
                    working = 0;
                    foreach (IMyGasGenerator gen in irrigationSystems)
                        working += gen.IsWorking ? 1 : 0;
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Irrigation Systems", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{working}          ", TextAlignment.RIGHT, surfaceData.useColors ? Color.GreenYellow : surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Active:              /      ", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"                      {irrigationSystems.Count}", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                    position += surfaceData.newLine;
                }

                // OxygenFarms
                if (oxygenFarms.Count > 0 && showOxygenFarms)
                {
                    working = 0;
                    foreach (IMyOxygenFarm oxygenFarm in oxygenFarms)
                        working += oxygenFarm.IsWorking ? 1 : 0;
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Oxygen Farms", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{working}          ", TextAlignment.RIGHT, surfaceData.useColors ? Color.GreenYellow : surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Active:              /      ", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"                       {oxygenFarms.Count}", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                    position += surfaceData.newLine;
                }

                // Food Processors
                if (foodProcessors.Count > 0 && showFoodProcessors)
                {
                    working = 0;
                    foreach (IMyAssembler fp in foodProcessors)
                        working += fp.IsWorking ? 1 : 0;
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Food Processors", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{working}          ", TextAlignment.RIGHT, surfaceData.useColors ? Color.GreenYellow : surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Active:              /      ", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"                       {foodProcessors.Count}", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                    position += surfaceData.newLine;
                }

                // Algae Farms
                if (algaeFarms.Count > 0 && showAlgaeFarms)
                {
                    working = 0;
                    foreach (var af in algaeFarms)
                        working += af.IsWorking ? 1 : 0;
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Algae Farms", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{working}          ", TextAlignment.RIGHT, surfaceData.useColors ? Color.GreenYellow : surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Active:              /      ", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"                       {algaeFarms.Count}", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                    position += surfaceData.newLine;
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenProductionSummary: Caught Exception while DrawCompactProductionSprite: {e.ToString()}");
            }
        }

        // Category drawers
        void DrawFoodProcessorSummarySprite(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            if (foodProcessors.Count <= 0) return;
            try
            {
                // Sort food processors alphabetically by custom name
                MahSorting.SortBlocksByName(foodProcessors);

                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Food Processors [{foodProcessors.Count}]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Active Task      ", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine;

                foreach (var assembler in foodProcessors)
                {
                    if (assembler == null) continue;

                    var name = assembler.CustomName;
                    List<Sandbox.ModAPI.Ingame.MyProductionItem> queuedBlueprints = new List<Sandbox.ModAPI.Ingame.MyProductionItem>();
                    assembler.GetQueue(queuedBlueprints);
                    var blueprintId = queuedBlueprints.Count > 0 ? queuedBlueprints[0].BlueprintId.ToString().Split('/')[1] : "";
                    var blueprintAmount = queuedBlueprints.Count > 0 ? (int)queuedBlueprints[0].Amount : 0;

                    // Map blueprint display name using definitions if possible
                    if (blueprintId != "")
                    {
                        CargoItemDefinition itemDefinition = MahDefinitions.GetDefinition("ConsumableItem", blueprintId) ??
                                                             MahDefinitions.GetDefinition("PhysicalObject", blueprintId) ??
                                                             MahDefinitions.GetDefinition("Package", blueprintId) ??
                                                             MahDefinitions.GetDefinition("Component", blueprintId);
                        if (itemDefinition != null)
                            blueprintId = itemDefinition.displayName;
                    }

                    var queue = blueprintId == "" ? "-" : $"{blueprintId}";
                    var outputBlocked = assembler.OutputInventory.CurrentVolume > assembler.OutputInventory.MaxVolume * .9f;
                    var state = $"{(!assembler.IsWorking ? "    Off" : outputBlocked ? "   Full" : queuedBlueprints.Count > 0 && assembler.IsProducing ? "  Work" : "   Halt")}";

                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{state} ", TextAlignment.LEFT, !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : state.Contains("Off") ? Color.Orange : state.Contains("Halt") ? Color.Yellow : state.Contains("Full") ? Color.Red : Color.GreenYellow);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"[          ] {name}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{(blueprintAmount > 0 ? blueprintAmount.ToString("0") : "")} {queue}  +{(queuedBlueprints.Count > 0 ? queuedBlueprints.Count - 1 : 0).ToString("0").Replace("1", " 1")}", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);

                    position += surfaceData.newLine;
                }

                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenProductionSummary: Caught Exception while DrawFoodProcessorSummarySprite: {e.ToString()}");
            }
        }

        void DrawIrrigationSystemSummarySprite(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            if (irrigationSystems.Count <= 0) return;
            try
            {
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Irrigation Systems [{irrigationSystems.Count}]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Inventory      ", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine;

                float currentVolume = 0.0f;
                CargoItemDefinition iceDefinition = MahDefinitions.GetDefinition("Ore", "Ice");
                List<VRage.Game.ModAPI.Ingame.MyInventoryItem> inventoryItems = new List<VRage.Game.ModAPI.Ingame.MyInventoryItem>();

                // Sort irrigation systems alphabetically by custom name
                MahSorting.SortBlocksByName(irrigationSystems);

                foreach (var gen in irrigationSystems)
                {
                    if (gen == null) continue;

                    var name = gen.CustomName;
                    var amount = 0;
                    var inventory = gen.GetInventory(0);

                    if (iceDefinition != null)
                    {
                        inventory.GetItems(inventoryItems);
                        foreach (var item in inventoryItems.OrderBy(i => i.Type.SubtypeId))
                        {
                            if (item == null) continue;
                            var subtypeId = item.Type.SubtypeId;
                            if (subtypeId.IndexOf("Ice", StringComparison.OrdinalIgnoreCase) >= 0)
                                amount += item.Amount.ToIntSafe();
                        }
                        currentVolume = amount * iceDefinition.volume;
                    }
                    else
                    {
                        currentVolume = (float)inventory.CurrentVolume;
                    }

                    float maximumVolume = (float)inventory.MaxVolume * 1000;
                    var state = $"{(!gen.IsWorking ? "    Off" : currentVolume <= 0 ? "   Halt" : "  Work")}";

                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{state}", TextAlignment.LEFT, !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : state.Contains("Off") ? Color.Orange : state.Contains("Halt") ? Color.Yellow : Color.GreenYellow);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"[          ] {name}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.DrawHalfBar(ref frame, position, surfaceData, TextAlignment.RIGHT, currentVolume, maximumVolume, Unit.Percent, Color.Aquamarine);
                    position += surfaceData.newLine;
                }

                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenProductionSummary: Caught Exception while DrawIrrigationSystemSummarySprite: {e.ToString()}");
            }
        }

        void DrawAlgaeFarmSummarySprite(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            if (algaeFarms.Count <= 0) return;
            try
            {
                // Sort algae farms alphabetically by custom name
                MahSorting.SortBlocksByName(algaeFarms);

                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Algae Farms [{algaeFarms.Count}]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Current Progress      ", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine;

                foreach (var block in algaeFarms)
                {
                    if (block == null) continue;
                    var name = block.CustomName;
                    float progress = ParseProgressPercent(block.DetailedInfo);
                    var state = $"{(!block.IsWorking ? "    Off" : progress > 0f ? "  Work" : "   Idle")}";

                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{state}", TextAlignment.LEFT, !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : state.Contains("Off") ? Color.Orange : state.Contains("Idle") ? Color.Yellow : Color.GreenYellow);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"[          ] {name}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    // Right: progress half bar same size as generators
                    SurfaceDrawer.DrawHalfBar(ref frame, position, surfaceData, TextAlignment.RIGHT, progress, 1f, Unit.Percent, Color.GreenYellow);
                    position += surfaceData.newLine;
                }

                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenProductionSummary: Caught Exception while DrawAlgaeFarmSummarySprite: {e.ToString()}");
            }
        }

        // Helpers to detect modded blocks by subtype or custom name
        bool IsFoodProcessor(IMyTerminalBlock tb)
        {
            if (tb == null) return false;
            var id = (tb.BlockDefinition.SubtypeName ?? "") + " " + (tb.CustomName ?? "");
            var norm = Normalize(id);
            return norm.Contains("foodprocessor") || (norm.Contains("food") && norm.Contains("processor"));
        }

        bool IsIrrigationSystem(IMyTerminalBlock tb)
        {
            if (tb == null) return false;
            var id = (tb.BlockDefinition.SubtypeName ?? "") + " " + (tb.CustomName ?? "");
            var norm = Normalize(id);
            return norm.Contains("irrigation");
        }

        bool IsAlgaeFarm(IMyTerminalBlock tb)
        {
            if (tb == null) return false;
            var id = (tb.BlockDefinition.SubtypeName ?? "") + " " + (tb.CustomName ?? "");
            var norm = Normalize(id);
            return norm.Contains("algaefarm") || (norm.Contains("algae") && norm.Contains("farm"));
        }

        string Normalize(string s)
        {
            return (s ?? "").Replace(" ", "").Replace("_", "").Replace("-", "").ToLowerInvariant();
        }

        float ParseProgressPercent(string detailedInfo)
        {
            try
            {
                if (string.IsNullOrEmpty(detailedInfo)) return 0f;
                var lines = detailedInfo.Split('\n');
                foreach (var raw in lines)
                {
                    var line = raw.Trim();
                    if (line.Length == 0) continue;
                    if (line.IndexOf("progress", StringComparison.OrdinalIgnoreCase) >= 0 && line.IndexOf('%') >= 0)
                    {
                        int idx = line.IndexOf('%');
                        int start = idx - 1;
                        while (start >= 0 && (char.IsDigit(line[start]) || line[start] == '.' || line[start] == ',')) start--;
                        var num = line.Substring(start + 1, idx - (start + 1));
                        num = num.Replace(',', '.');
                        float val = 0f;
                        if (float.TryParse(num, out val))
                            return Math.Max(0f, Math.Min(1f, val / 100f));
                    }
                }
            }
            catch { }
            return 0f;
        }
    }
}
