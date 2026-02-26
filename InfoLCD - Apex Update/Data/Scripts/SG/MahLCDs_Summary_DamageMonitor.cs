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
    [MyTextSurfaceScript("LCDInfoScreenDamageMonitorSummary", "$IOS LCD - Damage Control")]
    public class LCDDamageMonitorSummary : MyTextSurfaceScriptBase
    {
        MyIni config = new MyIni();

        public static string CONFIG_SECTION_ID = "SettingsDamageMonitorStatus";

        string searchId = "*";

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
                titleOffset = 128,
                ratioOffset = 64,
                viewPortOffsetX = 10,
                viewPortOffsetY = 10,
                newLine = new Vector2(0, 30 * textSize),
                showHeader = true,
                showSummary = false,
                showMissing = false,
                showRatio = true,
                showBars = true,
                showSubgrids = false,
                showDocked = false,
                useColors = true,
                showStructuralDetails = false,
                showStructural = false,
                showMovement = false,
                showCommunications = true,
                showTanks = true,
                showPower = true,
                showProduction = false,
                showContainers = false,
                showDoors = false,
                showControllers = true,
                showGyros = false,
                showMechanical = false,
                showJumpdrives = false,
                showWeapons = true,
                showTools = false,
                showAutomation = true,
                showMedical = false
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
            sb.AppendLine("; [ DAMAGEMONITOR - GENERAL OPTIONS ]");
            ConfigHelpers.AppendSearchIdConfig(sb, searchId);
            ConfigHelpers.AppendExcludeIdsConfig(sb, excludeIds);
            ConfigHelpers.AppendShowHeaderConfig(sb, surfaceData.showHeader);
            ConfigHelpers.AppendShowSubgridsConfig(sb, surfaceData.showSubgrids);
            ConfigHelpers.AppendSubgridUpdateFrequencyConfig(sb, surfaceData.subgridUpdateFrequency);
            ConfigHelpers.AppendShowDockedConfig(sb, surfaceData.showDocked);
            ConfigHelpers.AppendUseColorsConfig(sb, surfaceData.useColors);

            sb.AppendLine();
            sb.AppendLine("; [ DAMAGEMONITOR - LAYOUT OPTIONS ]");
            sb.AppendLine($"TextSize={surfaceData.textSize}");
            sb.AppendLine($"ViewPortOffsetX={surfaceData.viewPortOffsetX}");
            sb.AppendLine($"ViewPortOffsetY={surfaceData.viewPortOffsetY}");
            sb.AppendLine($"TitleFieldWidth={surfaceData.titleOffset}");
            sb.AppendLine($"RatioFieldWidth={surfaceData.ratioOffset}");
            sb.AppendLine($"ShowRatio={surfaceData.showRatio}");
            sb.AppendLine($"ShowBars={surfaceData.showBars}");

            sb.AppendLine();
            sb.AppendLine("; [ DAMAGEMONITOR - SCREEN OPTIONS ]");
            sb.AppendLine($"ShowStructuralDetails={surfaceData.showStructuralDetails}");
            sb.AppendLine($"ShowStructural={surfaceData.showStructural}");
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

                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowHeader", ref surfaceData.showHeader, ref configError);
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowRatio", ref surfaceData.showRatio, ref configError);
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowBars", ref surfaceData.showBars, ref configError);
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowSubgrids", ref surfaceData.showSubgrids, ref configError);
                    if (config.ContainsKey(CONFIG_SECTION_ID, "SubgridUpdateFrequency")) surfaceData.subgridUpdateFrequency = config.Get(CONFIG_SECTION_ID, "SubgridUpdateFrequency").ToInt32();
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowDocked", ref surfaceData.showDocked, ref configError);
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "UseColors", ref surfaceData.useColors, ref configError);

                    // Category toggles (optional; default false)
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowStructural")) surfaceData.showStructural = config.Get(CONFIG_SECTION_ID, "ShowStructural").ToBoolean();
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowMovement")) surfaceData.showMovement = config.Get(CONFIG_SECTION_ID, "ShowMovement").ToBoolean();
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowCommunications")) surfaceData.showCommunications = config.Get(CONFIG_SECTION_ID, "ShowCommunications").ToBoolean();
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowTanks")) surfaceData.showTanks = config.Get(CONFIG_SECTION_ID, "ShowTanks").ToBoolean();
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowPower")) surfaceData.showPower = config.Get(CONFIG_SECTION_ID, "ShowPower").ToBoolean();
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowProduction")) surfaceData.showProduction = config.Get(CONFIG_SECTION_ID, "ShowProduction").ToBoolean();
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowContainers")) surfaceData.showContainers = config.Get(CONFIG_SECTION_ID, "ShowContainers").ToBoolean();
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowDoors")) surfaceData.showDoors = config.Get(CONFIG_SECTION_ID, "ShowDoors").ToBoolean();
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowControllers")) surfaceData.showControllers = config.Get(CONFIG_SECTION_ID, "ShowControllers").ToBoolean();
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowGyros")) surfaceData.showGyros = config.Get(CONFIG_SECTION_ID, "ShowGyros").ToBoolean();
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowMechanical")) surfaceData.showMechanical = config.Get(CONFIG_SECTION_ID, "ShowMechanical").ToBoolean();
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowJumpdrives")) surfaceData.showJumpdrives = config.Get(CONFIG_SECTION_ID, "ShowJumpdrives").ToBoolean();
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowWeapons")) surfaceData.showWeapons = config.Get(CONFIG_SECTION_ID, "ShowWeapons").ToBoolean();
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowTools")) surfaceData.showTools = config.Get(CONFIG_SECTION_ID, "ShowTools").ToBoolean();
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowAutomation")) surfaceData.showAutomation = config.Get(CONFIG_SECTION_ID, "ShowAutomation").ToBoolean();
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowMedical")) surfaceData.showMedical = config.Get(CONFIG_SECTION_ID, "ShowMedical").ToBoolean();
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowStructuralDetails")) surfaceData.showStructuralDetails = config.Get(CONFIG_SECTION_ID, "ShowStructuralDetails").ToBoolean();

                    CreateExcludeIdsList();

                    // Is Corner LCD?
                    if (compactMode)
                    {
                        surfaceData.showHeader = true;
                        surfaceData.showSummary = true;
                        surfaceData.textSize = 0.6f;
                        surfaceData.titleOffset = 200;
                        surfaceData.ratioOffset = 104;
                        surfaceData.viewPortOffsetX = 10;
                        surfaceData.viewPortOffsetY = 5;
                        surfaceData.newLine = new Vector2(0, 30 * surfaceData.textSize);
                    }
                }
                else
                {
                    MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenDamageMonitorSummary: Config Syntax error at Line {result}");
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenDamageMonitorSummary: Caught Exception while loading config: {e.ToString()}");
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
        List<BlockStateData> blocks = new List<BlockStateData>();

        VRage.Collections.DictionaryValuesReader<MyDefinitionId, MyDefinitionBase> myDefinitions;
        MyDefinitionId myDefinitionId;
        SurfaceDrawer.SurfaceData surfaceData;
        string gridId = "Unknown grid";
        bool configError = false;
        bool compactMode = false;
        bool isStation = false;
        int damagedBlocks = 0;
        Sandbox.ModAPI.Ingame.MyShipMass gridMass;

    // Structural (slim-block) scanning state (cadence-based)
    int structuralScanTick = 0;
    double structuralCurrentIntegrity = 0d;
    double structuralMaxIntegrity = 0d;
    int structuralDamagedCount = 0;
    int structuralBlockCount = 0;
    List<StructuralBlockInfo> structuralDamagedList = new List<StructuralBlockInfo>();

    struct StructuralBlockInfo
    {
        public string subtype;
        public double cur;
        public double max;
        public double Ratio => max <= 0 ? 0 : cur / max;
    }

        public LCDDamageMonitorSummary(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
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
                if (surfaceData.showHeader)
                    SurfaceDrawer.DrawHeader(ref myFrame, ref myPosition, surfaceData, $"Damage Control [{(searchId.Trim().Length <= 0 || searchId == "*" ? "All" : searchId)} -{excludeIds.Count}]");

                DrawDamageMonitorSprite(ref myFrame, ref myPosition);
            }
            myFrame.Dispose();
        }

        void UpdateBlocks ()
        {
            try
            {
                blocks.Clear();
                damagedBlocks = 0;

                var myCubeGrid = myTerminalBlock.CubeGrid as MyCubeGrid;

                if (myCubeGrid == null) return;

                IMyCubeGrid cubeGrid = myCubeGrid as IMyCubeGrid;
                isStation = cubeGrid.IsStatic;
                gridId = cubeGrid.CustomName;

                var myFatBlocks = MahUtillities.GetBlocks(myCubeGrid, searchId, excludeIds, ref gridMass, surfaceData.showSubgrids);

                foreach (var myBlock in myFatBlocks)
                {
                    if (myBlock == null) continue;

                    IMyTerminalBlock block = (IMyTerminalBlock)myBlock;
                    BlockStateData blockData = new BlockStateData(block);
                    blocks.Add(blockData);

                    if (!blockData.IsFullIntegrity) damagedBlocks++;
                }

                // Update structural aggregates with cadence, only when enabled
                UpdateStructuralAggregates(cubeGrid);
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenDamageMonitorSummary: Caught Exception while updating blocks: {e.ToString()}");
            }
        }

        // Scan slim blocks (non-fat) across relevant grids to compute structural integrity aggregates
        void UpdateStructuralAggregates(IMyCubeGrid rootGrid)
        {
            try
            {
                if (!surfaceData.showStructural || rootGrid == null)
                {
                    // When disabled, keep zero impact and avoid any overhead
                    structuralCurrentIntegrity = 0d;
                    structuralMaxIntegrity = 0d;
                    structuralDamagedCount = 0;
                    structuralBlockCount = 0;
                    return;
                }

                structuralScanTick++;
                // Recompute based on SubgridUpdateFrequency config; compute immediately on first activation
                // Divide by 10 to account for Update10 timing (runs every 10 ticks, not every tick)
                if (structuralScanTick % (surfaceData.subgridUpdateFrequency / 10) != 0 && structuralBlockCount > 0)
                    return;

                double cur = 0d;
                double max = 0d;
                int dmg = 0;
                int cnt = 0;

                // Collect relevant grids based on settings
                var grids = new HashSet<IMyCubeGrid>();
                grids.Add(rootGrid);

                if (surfaceData.showSubgrids)
                {
                    var mech = new List<IMyCubeGrid>();
                    MyAPIGateway.GridGroups.GetGroup(rootGrid, VRage.Game.ModAPI.GridLinkTypeEnum.Mechanical, mech);
                    foreach (var g in mech) grids.Add(g);
                }

                if (surfaceData.showDocked)
                {
                    var phys = new List<IMyCubeGrid>();
                    MyAPIGateway.GridGroups.GetGroup(rootGrid, VRage.Game.ModAPI.GridLinkTypeEnum.Physical, phys);
                    foreach (var g in phys) grids.Add(g);
                }

                var tmp = new List<IMySlimBlock>(512);
                structuralDamagedList.Clear();
                foreach (var g in grids)
                {
                    if (g == null) continue;
                    tmp.Clear();
                    g.GetBlocks(tmp, s => s != null && (s.FatBlock == null || !(s.FatBlock is IMyTerminalBlock)));
                    foreach (var s in tmp)
                    {
                        double mi = s.MaxIntegrity;
                        if (mi <= 0) continue;
                        double ci = s.Integrity;
                        cnt++;
                        max += mi;
                        cur += ci > mi ? mi : ci;
                        if (ci < mi) dmg++;
                        if (ci < mi && structuralDamagedList.Count < 200)
                        {
                            structuralDamagedList.Add(new StructuralBlockInfo { subtype = s.BlockDefinition.Id.SubtypeName, cur = ci, max = mi });
                        }
                    }
                }


                // Keep only worst damaged up to 40 entries for display
                if (structuralDamagedList.Count > 0)
                {
                    structuralDamagedList = structuralDamagedList
                        .OrderBy(i => i.Ratio)
                        .ThenBy(i => i.subtype)
                        .Take(40)
                        .ToList();
                }
                structuralCurrentIntegrity = cur;
                structuralMaxIntegrity = max <= 0 ? 0d : max;
                structuralDamagedCount = dmg;
                structuralBlockCount = cnt;

                // Sort blocks once here instead of using LINQ OrderBy in draw loop
                blocks.Sort((a, b) => a.priority.CompareTo(b.priority));
            }
            catch (Exception e)
            {
                // Fail safe: never block UI if something goes wrong
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenDamageMonitorSummary: Structural scan error: {e.ToString()}");
            }
        }

        void DrawDamageMonitorSprite (ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            if (blocks.Count <= 0) return;

            try
            {
                float pixelPerChar = MahDefinitions.pixelPerChar * surfaceData.textSize;
                Vector2 stateOffset = new Vector2(pixelPerChar * 15, 0);

                // Pre-calculate aggregate integrity and counts for enabled categories (terminal blocks)
                double termCurrentIntegrity = 0d;
                double termMaxIntegrity = 0d;
                int termEnabledCount = 0;
                int termEnabledDamaged = 0;
                foreach (var b in blocks)
                {
                    if (b.IsNull) continue;
                    var tb = b.block as IMyTerminalBlock;
                    if (tb == null) continue;
                    if (!IncludeByCategory(tb)) continue;
                    termEnabledCount++;
                    if (!b.IsFullIntegrity) termEnabledDamaged++;
                    termCurrentIntegrity += b.CurrentIntegrity;
                    termMaxIntegrity += b.MaxIntegrity > 0 ? b.MaxIntegrity : 0;
                }

                // Combine with structural aggregates when enabled
                double totalCurrentIntegrity = termCurrentIntegrity + (surfaceData.showStructural ? structuralCurrentIntegrity : 0d);
                double totalMaxIntegrity = termMaxIntegrity + (surfaceData.showStructural ? structuralMaxIntegrity : 0d);
                if (totalMaxIntegrity <= 0) totalMaxIntegrity = 1; // avoid div by zero

                int displayDamaged = termEnabledDamaged + (surfaceData.showStructural ? structuralDamagedCount : 0);
                int displayTotal = termEnabledCount + (surfaceData.showStructural ? structuralBlockCount : 0);

                if (compactMode)
                {
                    // Tighten spacing (header already advanced two lines)
                    position -= surfaceData.newLine;
                    // Line 2: damaged blocks counter (always shown, even if zero)
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Damaged Blocks [{displayDamaged}/{displayTotal}]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    position += surfaceData.newLine;
                    // Optional: show structural contribution line
                    if (surfaceData.showStructural)
                    {
                        SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Structural: {structuralDamagedCount}/{structuralBlockCount}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                        position += surfaceData.newLine;
                    }
                    // Line 3: average integrity bar (full if no damaged blocks)
                    if (surfaceData.showBars)
                    {
                        // Enable standard green?yellow?orange?red threshold coloring (invertColors = true, ignoreColors = false)
                        SurfaceDrawer.DrawBar(ref frame, ref position, surfaceData, "Avg Integrity", totalCurrentIntegrity, totalMaxIntegrity, Unit.Percent, true, false);
                    }
                    else
                    {
                        double avgRatio = (totalCurrentIntegrity / totalMaxIntegrity) * 100.0;
                        SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Avg Integrity", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                        SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{avgRatio.ToString("0.0")} %", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                        position += surfaceData.newLine;
                    }
                    return; // compact mode done (no per-block listing)
                }

                if (displayDamaged <= 0)
                {
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"No damaged blocks found.", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    return;
                }

                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Damaged Blocks [{displayDamaged}/{displayTotal}]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Integrity", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine;

                // Optional: show structural contribution line
                if (surfaceData.showStructural)
                {
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Structural: {structuralDamagedCount}/{structuralBlockCount}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    position += surfaceData.newLine;
                    if (!compactMode && surfaceData.showStructuralDetails && structuralDamagedList.Count > 0)
                    {
                        foreach (var sb in structuralDamagedList)
                        {
                            string name = sb.subtype.Length > 32 ? sb.subtype.Substring(0,32) : sb.subtype;
                            SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"  [S] {name}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                            if (surfaceData.showBars)
                            {
                                SurfaceDrawer.DrawHalfBar(ref frame, position, surfaceData, TextAlignment.RIGHT, (float)sb.cur, (float)sb.max, Unit.Percent, sb.Ratio > .9 ? Color.GreenYellow : sb.Ratio > .6 ? Color.Yellow : sb.Ratio > .2 ? Color.Orange : Color.Red);
                            }
                            else
                            {
                                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{(sb.Ratio*100).ToString("0.0")}%", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                            }
                            position += surfaceData.newLine;
                        }
                    }
                }

                // Sort damaged blocks alphabetically by custom name
                MahSorting.SortBlockStateData(blocks, MahSorting.BlockSortMode.CustomName);

                foreach (var block in blocks)
                {
                    if (block.IsNull) continue;
                    if (block.IsFullIntegrity) continue;
                    // Respect enabled categories for listing
                    var tb = block.block as IMyTerminalBlock;
                    if (tb == null || !IncludeByCategory(tb)) continue;

                    var maxNameLength = (int)(mySurface.SurfaceSize.X > 300 ? 35 : 20);
                    var state = block.IsFunctional ? block.IsWorking ? "   On" : "   Off" : "Broken";
                    var blockId = block.CustomName.Length > maxNameLength ? block.CustomName.Substring(0, maxNameLength) : block.CustomName;
                    var maxIntegrity = block.MaxIntegrity;
                    var curIntegrity = block.CurrentIntegrity;
                    var ratio = curIntegrity / maxIntegrity;

                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"  {state}", TextAlignment.LEFT, !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : state.Contains("Off") ? Color.Orange : state.Contains("On") ? Color.GreenYellow : Color.Red);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"[             ] {blockId}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                
                    if (surfaceData.showBars)
                    {
                        SurfaceDrawer.DrawHalfBar(ref frame, position, surfaceData, TextAlignment.RIGHT, curIntegrity, maxIntegrity, Unit.Percent, ratio > .9f ? Color.GreenYellow : ratio > .6f ? Color.Yellow : ratio > .2f ? Color.Orange : Color.Red);
                    }
                    else
                    {
                        ratio *= 100;
                        SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{ratio.ToString("0.0")}%", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                    }

                    position += surfaceData.newLine;
                    
                    // If compactMode, only show the first damaged block, then quit.
                    if (compactMode) return;
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenDamageMonitorSummary: Caught Exception while DrawDamageMonitorSprite: {e.ToString()}");
            }
        }

        // Terminal-block category filter (Phase 1): map interfaces to toggles. Structural (slim) handled in Phase 2.
        bool IncludeByCategory(IMyTerminalBlock tb)
        {
            try
            {
                // If no categories enabled at all, treat as 'none' (show nothing)
                bool any = surfaceData.showStructural || surfaceData.showMovement || surfaceData.showCommunications || surfaceData.showTanks ||
                           surfaceData.showPower || surfaceData.showProduction || surfaceData.showContainers || surfaceData.showDoors ||
                           surfaceData.showControllers || surfaceData.showGyros || surfaceData.showMechanical || surfaceData.showJumpdrives ||
                           surfaceData.showWeapons || surfaceData.showTools || surfaceData.showAutomation || surfaceData.showMedical;
                if (!any) return false;

                // Controllers
                if (tb is IMyShipController || tb is IMyCockpit)
                    return surfaceData.showControllers;
                // Movement (thrusters + wheels)
                if (tb is IMyThrust || tb is IMyWheel)
                    return surfaceData.showMovement;
                // Communications (sensors + comms)
                if (tb is IMySensorBlock || tb is IMyOreDetector || tb is IMyCameraBlock || tb is IMyRadioAntenna || tb is IMyLaserAntenna || tb is IMyBeacon || tb.BlockDefinition.SubtypeName.IndexOf("Beacon", StringComparison.OrdinalIgnoreCase) >= 0)
                    return surfaceData.showCommunications;
                // Tanks
                if (tb is IMyGasTank)
                    return surfaceData.showTanks;
                // Power
                if (tb is IMyBatteryBlock || tb is IMyReactor || tb is IMySolarPanel || tb.BlockDefinition.SubtypeName.IndexOf("Wind", StringComparison.OrdinalIgnoreCase) >= 0 || tb.BlockDefinition.SubtypeName.IndexOf("HydrogenEngine", StringComparison.OrdinalIgnoreCase) >= 0)
                    return surfaceData.showPower;
                // Production
                if (tb is IMyAssembler || tb is IMyRefinery || tb is IMyGasGenerator || tb is IMyOxygenFarm)
                    return surfaceData.showProduction;
                // Containers
                if (tb is IMyCargoContainer)
                    return surfaceData.showContainers;
                // Doors
                if (tb is IMyDoor)
                    return surfaceData.showDoors;
                // Gyros
                if (tb is IMyGyro)
                    return surfaceData.showGyros;
                // Mechanical
                if (tb is IMyPistonBase || tb is IMyMotorAdvancedStator || tb is IMyMotorStator || tb is IMyLandingGear || tb is IMyShipConnector || tb is IMyAdvancedDoor)
                    return surfaceData.showMechanical;
                // Jumpdrives
                if (tb is IMyJumpDrive)
                    return surfaceData.showJumpdrives;
                // Weapons
                if (tb is IMyUserControllableGun)
                    return surfaceData.showWeapons;
                // Tools
                if (tb is IMyShipDrill || tb is IMyShipWelder || tb is IMyShipGrinder)
                    return surfaceData.showTools;
                // Medical (explicit interfaces)
                if (tb is IMyCryoChamber || tb is IMyMedicalRoom)
                    return surfaceData.showMedical;
                // Automation (specific block subtypes/controllers)
                var sub = tb.BlockDefinition.SubtypeName;
                if (sub.IndexOf("Programmable", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    sub.IndexOf("RemoteControl", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    sub.IndexOf("EventController", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    sub.IndexOf("EmotionController", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    sub.IndexOf("TurretControl", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    sub.IndexOf("TurretController", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    sub.IndexOf("CustomTurret", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    sub.IndexOf("AIFlight", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    sub.IndexOf("AIBasic", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    sub.IndexOf("AIRecorder", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    sub.IndexOf("AIDefensive", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    sub.IndexOf("AIOffensive", StringComparison.OrdinalIgnoreCase) >= 0)
                    return surfaceData.showAutomation;
                // Medical
                if (sub.IndexOf("Cryo", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    sub.IndexOf("Cryopod", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    sub.IndexOf("Medical", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    sub.IndexOf("MedBay", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    sub.IndexOf("Refill", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    sub.IndexOf("SurvivalKit", StringComparison.OrdinalIgnoreCase) >= 0)
                    return surfaceData.showMedical;

                // Structural (terminal blocks typically excluded; will be covered via slim blocks later)
                if (surfaceData.showStructural)
                {
                    // Avoid misclassifying interactive panels/furniture if they are terminal blocks
                    if (sub.IndexOf("Window", StringComparison.OrdinalIgnoreCase) >= 0 || sub.IndexOf("Catwalk", StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }

                return false;
            }
            catch { return false; }
        }
    }
}