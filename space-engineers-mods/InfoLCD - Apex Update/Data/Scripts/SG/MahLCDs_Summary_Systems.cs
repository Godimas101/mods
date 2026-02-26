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
    [MyTextSurfaceScript("LCDInfoScreenSystemsSummary", "$IOS LCD - Systems")]
    public class LCDSystemsSummary : MyTextSurfaceScriptBase
    {
        MyIni config = new MyIni();

        public static string CONFIG_SECTION_ID = "SettingsSystemsStatus";

        string searchId = "*";
    bool showIntegrity = true;
    bool showHealthyCategories = false; // new: show categories even when fully healthy

        void TryCreateSurfaceData()
        {
            if (surfaceData != null)
                return;

            compactMode = mySurface.SurfaceSize.X / mySurface.SurfaceSize.Y > 4;
            var textSize = compactMode ? .4f : .4f;

            // Initialize surface settings
            surfaceData = new SurfaceDrawer.SurfaceData
            {
                surface = mySurface,
                textSize = textSize,
                titleOffset = 96,
                ratioOffset = 128,
                viewPortOffsetX = 20,
                viewPortOffsetY = 10,
                newLine = new Vector2(0, 30 * textSize),
                showHeader = true,
                showSummary = true,
                showMissing = false,
                showRatio = true,
                showBars = true,
                showSubgrids = false,
                showDocked = true,
                useColors = true,
                // Default category toggles (user request: default all ON)
                showMovement = true,
                showCommunications = true,
                showTanks = true,
                showPower = true,
                showProduction = true,
                showContainers = true,
                showDoors = true,
                showControllers = true,
                showGyros = true,
                showMechanical = true,
                showJumpdrives = true,
                showWeapons = true,
                showTools = true,
                showAutomation = true,
                showMedical = true,
                showHealthyCategories = false
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
            sb.AppendLine("; [ SYSTEMS - GENERAL OPTIONS ]");
            ConfigHelpers.AppendSearchIdConfig(sb, searchId);
            ConfigHelpers.AppendExcludeIdsConfig(sb, excludeIds);
            ConfigHelpers.AppendShowHeaderConfig(sb, surfaceData.showHeader);
            ConfigHelpers.AppendShowSummaryConfig(sb, surfaceData.showSummary);
            ConfigHelpers.AppendShowRatioConfig(sb, surfaceData.showRatio);
            ConfigHelpers.AppendShowBarsConfig(sb, surfaceData.showBars);
            ConfigHelpers.AppendShowSubgridsConfig(sb, surfaceData.showSubgrids);
            ConfigHelpers.AppendSubgridUpdateFrequencyConfig(sb, surfaceData.subgridUpdateFrequency);
            ConfigHelpers.AppendShowDockedConfig(sb, surfaceData.showDocked);
            ConfigHelpers.AppendUseColorsConfig(sb, surfaceData.useColors);

            sb.AppendLine();
            sb.AppendLine("; [ SYSTEMS - LAYOUT OPTIONS ]");
            sb.AppendLine($"TextSize={surfaceData.textSize}");
            sb.AppendLine($"ViewPortOffsetX={surfaceData.viewPortOffsetX}");
            sb.AppendLine($"ViewPortOffsetY={surfaceData.viewPortOffsetY}");
            sb.AppendLine($"TitleFieldWidth={surfaceData.titleOffset}");
            sb.AppendLine($"RatioFieldWidth={surfaceData.ratioOffset}");

            sb.AppendLine();
            sb.AppendLine("; [ SYSTEMS - SCREEN OPTIONS ]");
            sb.AppendLine($"ShowIntegrity={showIntegrity}");
            sb.AppendLine($"ShowHealthyCategories={surfaceData.showHealthyCategories}");
            sb.AppendLine($"ShowMovement={surfaceData.showMovement}");
            sb.AppendLine($"ShowCommunications={surfaceData.showCommunications}");
            sb.AppendLine($"ShowTanks={surfaceData.showTanks}");
            sb.AppendLine($"ShowPower={surfaceData.showPower}");
            sb.AppendLine($"ShowProduction={surfaceData.showProduction}");
            sb.AppendLine($"ShowContainers={surfaceData.showContainers}");
            sb.AppendLine($"ShowDoors={surfaceData.showDoors}");
            sb.AppendLine($"ShowControllers={surfaceData.showControllers}");
            sb.AppendLine($"ShowGyros={surfaceData.showGyros}");
            sb.AppendLine($"ShowMechanical={surfaceData.showMechanical}");
            sb.AppendLine($"ShowJumpdrives={surfaceData.showJumpdrives}");
            sb.AppendLine($"ShowWeapons={surfaceData.showWeapons}");
            sb.AppendLine($"ShowTools={surfaceData.showTools}");
            sb.AppendLine($"ShowAutomation={surfaceData.showAutomation}");
            sb.AppendLine($"ShowMedical={surfaceData.showMedical}");

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
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowRatio", ref surfaceData.showRatio, ref configError);
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowBars", ref surfaceData.showBars, ref configError);
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowSubgrids", ref surfaceData.showSubgrids, ref configError);
                    if (config.ContainsKey(CONFIG_SECTION_ID, "SubgridUpdateFrequency")) surfaceData.subgridUpdateFrequency = config.Get(CONFIG_SECTION_ID, "SubgridUpdateFrequency").ToInt32();
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowDocked", ref surfaceData.showDocked, ref configError);

                    if (config.ContainsKey(CONFIG_SECTION_ID, "SearchId"))
                    {
                        searchId = config.Get(CONFIG_SECTION_ID, "SearchId").ToString();
                        searchId = string.IsNullOrWhiteSpace(searchId) ? "*" : searchId;
                    }
                    else
                        configError = true;

                    if (config.ContainsKey(CONFIG_SECTION_ID, "TextSize"))
                        surfaceData.textSize = config.Get(CONFIG_SECTION_ID, "TextSize").ToSingle(defaultValue: 1.0f);
                    else
                        configError = true;

                    MahUtillities.TryGetConfigFloat(config, CONFIG_SECTION_ID, "TitleFieldWidth", ref surfaceData.titleOffset, ref configError);
                    MahUtillities.TryGetConfigFloat(config, CONFIG_SECTION_ID, "RatioFieldWidth", ref surfaceData.ratioOffset, ref configError);
                    MahUtillities.TryGetConfigFloat(config, CONFIG_SECTION_ID, "ViewPortOffsetX", ref surfaceData.viewPortOffsetX, ref configError);
                    MahUtillities.TryGetConfigFloat(config, CONFIG_SECTION_ID, "ViewPortOffsetY", ref surfaceData.viewPortOffsetY, ref configError);

                    surfaceData.newLine = new Vector2(0, 30 * surfaceData.textSize);

                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowIntegrity", ref showIntegrity, ref configError);
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "UseColors", ref surfaceData.useColors, ref configError);

                    surfaceData.showHealthyCategories = config.ContainsKey(CONFIG_SECTION_ID, "ShowHealthyCategories") ? config.Get(CONFIG_SECTION_ID, "ShowHealthyCategories").ToBoolean() : false;

                    // Category toggles (optional keys; default true)
                    surfaceData.showMovement = config.ContainsKey(CONFIG_SECTION_ID, "ShowMovement") ? config.Get(CONFIG_SECTION_ID, "ShowMovement").ToBoolean() : true;
                    surfaceData.showCommunications = config.ContainsKey(CONFIG_SECTION_ID, "ShowCommunications") ? config.Get(CONFIG_SECTION_ID, "ShowCommunications").ToBoolean() : true;
                    surfaceData.showTanks = config.ContainsKey(CONFIG_SECTION_ID, "ShowTanks") ? config.Get(CONFIG_SECTION_ID, "ShowTanks").ToBoolean() : true;
                    surfaceData.showPower = config.ContainsKey(CONFIG_SECTION_ID, "ShowPower") ? config.Get(CONFIG_SECTION_ID, "ShowPower").ToBoolean() : true;
                    surfaceData.showProduction = config.ContainsKey(CONFIG_SECTION_ID, "ShowProduction") ? config.Get(CONFIG_SECTION_ID, "ShowProduction").ToBoolean() : true;
                    surfaceData.showContainers = config.ContainsKey(CONFIG_SECTION_ID, "ShowContainers") ? config.Get(CONFIG_SECTION_ID, "ShowContainers").ToBoolean() : true;
                    surfaceData.showDoors = config.ContainsKey(CONFIG_SECTION_ID, "ShowDoors") ? config.Get(CONFIG_SECTION_ID, "ShowDoors").ToBoolean() : true;
                    surfaceData.showControllers = config.ContainsKey(CONFIG_SECTION_ID, "ShowControllers") ? config.Get(CONFIG_SECTION_ID, "ShowControllers").ToBoolean() : true;
                    surfaceData.showGyros = config.ContainsKey(CONFIG_SECTION_ID, "ShowGyros") ? config.Get(CONFIG_SECTION_ID, "ShowGyros").ToBoolean() : true;
                    surfaceData.showMechanical = config.ContainsKey(CONFIG_SECTION_ID, "ShowMechanical") ? config.Get(CONFIG_SECTION_ID, "ShowMechanical").ToBoolean() : true;
                    surfaceData.showJumpdrives = config.ContainsKey(CONFIG_SECTION_ID, "ShowJumpdrives") ? config.Get(CONFIG_SECTION_ID, "ShowJumpdrives").ToBoolean() : true;
                    surfaceData.showWeapons = config.ContainsKey(CONFIG_SECTION_ID, "ShowWeapons") ? config.Get(CONFIG_SECTION_ID, "ShowWeapons").ToBoolean() : true;
                    surfaceData.showTools = config.ContainsKey(CONFIG_SECTION_ID, "ShowTools") ? config.Get(CONFIG_SECTION_ID, "ShowTools").ToBoolean() : true;
                    surfaceData.showAutomation = config.ContainsKey(CONFIG_SECTION_ID, "ShowAutomation") ? config.Get(CONFIG_SECTION_ID, "ShowAutomation").ToBoolean() : true;
                    surfaceData.showMedical = config.ContainsKey(CONFIG_SECTION_ID, "ShowMedical") ? config.Get(CONFIG_SECTION_ID, "ShowMedical").ToBoolean() : true;

                    CreateExcludeIdsList();

                    // Is Corner LCD?
                    if (compactMode)
                    {
                        surfaceData.showHeader = true;
                        surfaceData.showSummary = true;
                        surfaceData.showDocked = true;
                        surfaceData.textSize = 0.4f;
                        surfaceData.titleOffset = 200;
                        surfaceData.ratioOffset = 300;
                        surfaceData.viewPortOffsetX = 10;
                        surfaceData.viewPortOffsetY = 5;
                        surfaceData.newLine = new Vector2(0, 30 * surfaceData.textSize);
                    }
                }
                else
                {
                    MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenSystemsSummary: Config Syntax error at Line {result}");
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenSystemsSummary: Caught Exception while loading config: {e.ToString()}");
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
        List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
        List<IMyPowerProducer> hydrogenEngines = new List<IMyPowerProducer>();
        List<IMySolarPanel> solarPanels = new List<IMySolarPanel>();
        List<IMyReactor> reactors = new List<IMyReactor>();
        List<IMyGasTank> tanks = new List<IMyGasTank>();
        List<BlockStateData> blocks = new List<BlockStateData>();
        List<IMyPowerProducer> powerProducers = new List<IMyPowerProducer>();

        // Cached subgrid collections
        List<IMyBatteryBlock> subgridBatteries = new List<IMyBatteryBlock>();
        List<IMyPowerProducer> subgridHydrogenEngines = new List<IMyPowerProducer>();
        List<IMySolarPanel> subgridSolarPanels = new List<IMySolarPanel>();
        List<IMyReactor> subgridReactors = new List<IMyReactor>();
        List<IMyGasTank> subgridTanks = new List<IMyGasTank>();
        List<IMyCubeBlock> subgridBlocks = new List<IMyCubeBlock>();
        List<IMyPowerProducer> subgridPowerProducers = new List<IMyPowerProducer>();

        VRage.Collections.DictionaryValuesReader<MyDefinitionId, MyDefinitionBase> myDefinitions;
        MyDefinitionId myDefinitionId;
        SurfaceDrawer.SurfaceData surfaceData;

        string gridId = "Unknown grid";
        int subgridScanTick = 0;
        float timeRemaining = 0.0f;
        float reactorsCurrentVolume = 0.0f;
        float reactorsMaximumVolume = 0.0f;
        float reactorsCurrentLoad = 0.0f;
        float reactorsMaximumLoad = 0.0f;
        int damagedBlocksCounter = 0;
        bool configError = false;
        bool compactMode = false;
        bool isStation = false;
        Sandbox.ModAPI.Ingame.MyShipMass gridMass;

        public LCDSystemsSummary(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
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
                DrawSystemsMainSprite(ref myFrame, ref myPosition);
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
                var mainPowerBlocks = MahUtillities.GetPowerBlocks(mainBlocks);

                // Periodically update subgrid cache
                if (scanSubgrids)
                {
                    var allBlocks = MahUtillities.GetBlocks(myCubeGrid, searchId, excludeIds, ref gridMass, surfaceData.showSubgrids);
                    var allPowerBlocks = MahUtillities.GetPowerBlocks(allBlocks);

                    // Extract subgrid-only power blocks
                    subgridBatteries.Clear();
                    foreach (var bat in allPowerBlocks.Batteries)
                        if (!mainPowerBlocks.Batteries.Contains(bat))
                            subgridBatteries.Add(bat);

                    subgridHydrogenEngines.Clear();
                    foreach (var eng in allPowerBlocks.HydrogenEngines)
                        if (!mainPowerBlocks.HydrogenEngines.Contains(eng))
                            subgridHydrogenEngines.Add(eng);

                    subgridSolarPanels.Clear();
                    foreach (var sol in allPowerBlocks.SolarPanels)
                        if (!mainPowerBlocks.SolarPanels.Contains(sol))
                            subgridSolarPanels.Add(sol);

                    subgridReactors.Clear();
                    foreach (var rea in allPowerBlocks.Reactors)
                        if (!mainPowerBlocks.Reactors.Contains(rea))
                            subgridReactors.Add(rea);

                    subgridPowerProducers.Clear();
                    foreach (var pow in allPowerBlocks.AllPowerProducers)
                        if (!mainPowerBlocks.AllPowerProducers.Contains(pow))
                            subgridPowerProducers.Add(pow);

                    // Extract subgrid-only blocks for tanks and general processing
                    subgridBlocks.Clear();
                    foreach (var block in allBlocks)
                        if (!mainBlocks.Contains(block))
                            subgridBlocks.Add(block);

                    subgridTanks.Clear();
                    foreach (var block in subgridBlocks)
                        if (block is IMyGasTank)
                            subgridTanks.Add((IMyGasTank)block);
                }

                // Merge main (fresh) and subgrid (cached) collections
                batteries.Clear();
                batteries.AddRange(mainPowerBlocks.Batteries);
                batteries.AddRange(subgridBatteries);

                hydrogenEngines.Clear();
                hydrogenEngines.AddRange(mainPowerBlocks.HydrogenEngines);
                hydrogenEngines.AddRange(subgridHydrogenEngines);

                solarPanels.Clear();
                solarPanels.AddRange(mainPowerBlocks.SolarPanels);
                solarPanels.AddRange(subgridSolarPanels);

                reactors.Clear();
                reactors.AddRange(mainPowerBlocks.Reactors);
                reactors.AddRange(subgridReactors);

                powerProducers.Clear();
                powerProducers.AddRange(mainPowerBlocks.AllPowerProducers);
                powerProducers.AddRange(subgridPowerProducers);

                tanks.Clear();
                tanks.AddRange(subgridTanks);
                foreach (var block in mainBlocks)
                    if (block is IMyGasTank)
                        tanks.Add((IMyGasTank)block);

                // Process all blocks for state data and damage counting
                blocks.Clear();
                damagedBlocksCounter = 0;
                var allCurrentBlocks = new List<IMyCubeBlock>();
                allCurrentBlocks.AddRange(mainBlocks);
                allCurrentBlocks.AddRange(subgridBlocks);

                foreach (var myBlock in allCurrentBlocks)
                {
                    if (myBlock == null) continue;

                    IMyTerminalBlock block = (IMyTerminalBlock)myBlock;
                    BlockStateData blockData = new BlockStateData(block);
                    blocks.Add(blockData);

                    if (!blockData.IsFullIntegrity) damagedBlocksCounter++;
                }

                // Calculate reactor load
                reactorsCurrentVolume = 0.0f;
                reactorsMaximumVolume = 0.0f;

                if (reactors.Count > 0)
                {
                    foreach (IMyReactor reactor in reactors)
                    {
                        reactorsCurrentVolume += (float)reactor.GetInventory(0).CurrentVolume;
                        reactorsMaximumVolume += (float)reactor.GetInventory(0).MaxVolume;
                    }

                    reactorsCurrentLoad = (reactorsCurrentVolume / 0.052f) * 1000;
                    reactorsMaximumLoad = (reactorsMaximumVolume / 0.052f) * 1000;
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenSystemsSummary: Caught Exception while updating blocks: {e.ToString()}");
            }
        }

        void DrawSystemsMainSprite(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            try
            {
                if (surfaceData.showHeader)
                {
                    SurfaceDrawer.DrawPowerTimeHeaderSprite(ref frame, ref position, surfaceData, $"Systems [{(searchId == "*" ? "All" : searchId)} -{excludeIds.Count}]", powerProducers);
                    // Compact mode: pull content up by one line to remove the extra blank line under the header
                    if (compactMode) position -= surfaceData.newLine;
                }
                if (showIntegrity)
                    SurfaceDrawer.DrawIntegritySummarySprite(ref frame, ref position, surfaceData, blocks, true, compactMode);

                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenSystemsSummary: Caught Exception while drawing main sprite: {e.ToString()}");
            }
        }
    }
}
