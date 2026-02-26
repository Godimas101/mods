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
    [MyTextSurfaceScript("LCDInfoScreenPowerSummary", "$IOS LCD - Power")]
    public class LCDPowerSummary : MyTextSurfaceScriptBase
    {
        MyIni config = new MyIni();

        public static string CONFIG_SECTION_ID = "SettingsPowerStatus";

        string searchId = "*";
        bool showBatteries = true;
        bool showSolar = true;
        bool showWind = true;
        bool showReactors = true;
        bool showEngines = true;
        bool showInactive = true;
        bool summaryOnly = false;
        int uraniumMinAmount = 500;

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
                titleOffset = 140,
                ratioOffset = 104,
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

        void CreateConfig()
        {
            TryCreateSurfaceData();

            config.Clear();

            // Build custom formatted config with INI section headers
            StringBuilder sb = new StringBuilder();
            
            // Always preserve existing CustomData (from other mods/apps)
            string existing = myTerminalBlock.CustomData ?? "";
            if (!string.IsNullOrWhiteSpace(existing))
            {
                sb.Append(existing);
                if (!existing.EndsWith("\n"))
                    sb.AppendLine();
            }

            // Write config with INI section header for proper parsing
            sb.AppendLine($"[{CONFIG_SECTION_ID}]");
            sb.AppendLine();
            sb.AppendLine("; [ POWER - GENERAL OPTIONS ]");
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
            ConfigHelpers.AppendScrollingConfig(sb);

            sb.AppendLine("; [ POWER - LAYOUT OPTIONS ]");
            sb.AppendLine($"TextSize={surfaceData.textSize}");
            sb.AppendLine($"ViewPortOffsetX={surfaceData.viewPortOffsetX}");
            sb.AppendLine($"ViewPortOffsetY={surfaceData.viewPortOffsetY}");
            sb.AppendLine($"TitleFieldWidth={surfaceData.titleOffset}");
            sb.AppendLine($"RatioFieldWidth={surfaceData.ratioOffset}");

            sb.AppendLine();
            sb.AppendLine("; [ POWER - SCREEN OPTIONS ]");
            sb.AppendLine($"ShowSolar={showSolar}");
            sb.AppendLine($"ShowWind={showWind}");
            sb.AppendLine($"ShowBatteries={showBatteries}");
            sb.AppendLine($"ShowReactors={showReactors}");
            sb.AppendLine($"ShowEngines={showEngines}");
            sb.AppendLine($"ShowInactive={showInactive}");
            sb.AppendLine($"SummaryOnly={summaryOnly}");

            sb.AppendLine();
            sb.AppendLine("; [ POWER - ITEM THRESHOLDS ]");
            sb.AppendLine($"UraniumMinAmount={uraniumMinAmount}");
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
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowInactive", ref showInactive, ref configError);
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

                    surfaceData.newLine = new Vector2(0, 30 * surfaceData.textSize);

                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowBatteries", ref showBatteries, ref configError);
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowSolar", ref showSolar, ref configError);
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowWind", ref showWind, ref configError);
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowReactors", ref showReactors, ref configError);
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowEngines", ref showEngines, ref configError);

                    if (config.ContainsKey(CONFIG_SECTION_ID, "SummaryOnly"))
                        summaryOnly = config.Get(CONFIG_SECTION_ID, "SummaryOnly").ToBoolean();
                    else
                        summaryOnly = false;
                    surfaceData.summaryOnly = summaryOnly;

                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "UseColors", ref surfaceData.useColors, ref configError);
                    if (config.ContainsKey(CONFIG_SECTION_ID, "UraniumMinAmount"))
                        uraniumMinAmount = config.Get(CONFIG_SECTION_ID, "UraniumMinAmount").ToInt32();
                    else
                        uraniumMinAmount = 500;

                    // Scrolling options (optional; default false/60/1)
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ToggleScroll"))
                        toggleScroll = config.Get(CONFIG_SECTION_ID, "ToggleScroll").ToBoolean(false);
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ReverseDirection"))
                        reverseDirection = config.Get(CONFIG_SECTION_ID, "ReverseDirection").ToBoolean(false);
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ScrollSpeed"))
                        scrollSpeed = Math.Max(1, config.Get(CONFIG_SECTION_ID, "ScrollSpeed").ToInt32(60));
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ScrollLines"))
                        scrollLines = Math.Max(1, config.Get(CONFIG_SECTION_ID, "ScrollLines").ToInt32(1));

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
                        surfaceData.titleOffset = 200;
                        surfaceData.ratioOffset = 104;
                        surfaceData.viewPortOffsetX = 10;
                        surfaceData.viewPortOffsetY = 5;
                        surfaceData.newLine = new Vector2(0, 30 * surfaceData.textSize);
                    }
                }
                else
                {
                    MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenPowerSummary: Config Syntax error at Line {result}");
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenPowerSummary: Caught Exception while loading config: {e.ToString()}");
            }
        }

        string ExtractOurConfigSection(string customData)
        {
            // Extract only the lines between our category headers
            string startMarker = "; [ POWER - GENERAL OPTIONS ]";
            string endMarker = "; ["; // Next section starts with this
            
            int startIndex = customData.IndexOf(startMarker);
            if (startIndex < 0) return ""; // Our config not found
            
            // Find the next app's section or end of string
            int endIndex = customData.IndexOf(endMarker, startIndex + startMarker.Length);
            if (endIndex < 0) endIndex = customData.Length; // No next section, go to end
            
            return customData.Substring(startIndex, endIndex - startIndex).Trim();
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
        List<IMyPowerProducer> windTurbines = new List<IMyPowerProducer>();
        List<IMyPowerProducer> hydrogenEngines = new List<IMyPowerProducer>();
        List<IMySolarPanel> solarPanels = new List<IMySolarPanel>();
        List<IMyReactor> reactors = new List<IMyReactor>();
        List<IMyGasTank> tanks = new List<IMyGasTank>();
        List<IMyPowerProducer> powerProducers = new List<IMyPowerProducer>();
        
        // Cached subgrid blocks (persisted between main grid scans)
        List<IMyBatteryBlock> subgridBatteries = new List<IMyBatteryBlock>();
        List<IMyPowerProducer> subgridWindTurbines = new List<IMyPowerProducer>();
        List<IMyPowerProducer> subgridHydrogenEngines = new List<IMyPowerProducer>();
        List<IMySolarPanel> subgridSolarPanels = new List<IMySolarPanel>();
        List<IMyReactor> subgridReactors = new List<IMyReactor>();
        List<IMyGasTank> subgridTanks = new List<IMyGasTank>();
        List<IMyPowerProducer> subgridPowerProducers = new List<IMyPowerProducer>();

        VRage.Collections.DictionaryValuesReader<MyDefinitionId, MyDefinitionBase> myDefinitions;
        MyDefinitionId myDefinitionId;
        SurfaceDrawer.SurfaceData surfaceData;
        float timeRemaining = 0.0f;
        int subgridScanTick = 0;
        
        float reactorsCurrentVolume = 0.0f;
        float reactorsMaximumVolume = 0.0f;
        float reactorsCurrentLoad = 0.0f;
        float reactorsMaximumLoad = 0.0f;
        string gridId = "Unknown grid";
        bool configError = false;
        bool compactMode = false;
        bool isStation = false;
        Sandbox.ModAPI.Ingame.MyShipMass gridMass;
        
        // Scrolling state
        bool toggleScroll = false;
        bool reverseDirection = false;
        int scrollSpeed = 60;
        int scrollLines = 1;
        int scrollOffset = 0;
        int ticksSinceLastScroll = 0;

        public LCDPowerSummary(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
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

            // Check if our app's config exists by looking for our section header
            if (myTerminalBlock.CustomData.Length <= 0 || !myTerminalBlock.CustomData.Contains(CONFIG_SECTION_ID))
                CreateConfig();

            LoadConfig();

            UpdateBlocks();
            
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
            {
                DrawPowerSummaryMainSprite(ref myFrame, ref myPosition);
            }

            myFrame.Dispose();
        }

        void UpdateBlocks ()
        {
            try
            {
                // Determine if we should scan subgrids this cycle
                bool scanSubgrids = false;
                if (surfaceData.showSubgrids)
                {
                    subgridScanTick++;
                    if (subgridScanTick >= surfaceData.subgridUpdateFrequency / 10)
                    {
                        subgridScanTick = 0;  // Reset counter after scan
                        scanSubgrids = true;
                    }
                }

                batteries.Clear();
                windTurbines.Clear();
                hydrogenEngines.Clear();
                solarPanels.Clear();
                reactors.Clear();
                powerProducers.Clear();
                tanks.Clear();

                var myCubeGrid = myTerminalBlock.CubeGrid as MyCubeGrid;

                if (myCubeGrid == null) return;

                IMyCubeGrid cubeGrid = myCubeGrid as IMyCubeGrid;
                isStation = cubeGrid.IsStatic;
                gridId = cubeGrid.CustomName;

                // Always scan main grid for instant updates
                var mainGridBlocks = MahUtillities.GetBlocks(myCubeGrid, searchId, excludeIds, ref gridMass, false);
                
                // If it's time for a subgrid scan, update the cached subgrid lists
                if (scanSubgrids)
                {
                    var allBlocks = MahUtillities.GetBlocks(myCubeGrid, searchId, excludeIds, ref gridMass, true);
                    
                    // Clear cached subgrid collections
                    subgridBatteries.Clear();
                    subgridWindTurbines.Clear();
                    subgridHydrogenEngines.Clear();
                    subgridSolarPanels.Clear();
                    subgridReactors.Clear();
                    subgridTanks.Clear();
                    subgridPowerProducers.Clear();
                    
                    // Get power blocks from full scan (main + subgrids)
                    var allPowerBlocks = MahUtillities.GetPowerBlocks(allBlocks);
                    
                    // Get main grid power blocks to subtract
                    var mainPowerBlocks = MahUtillities.GetPowerBlocks(mainGridBlocks);
                    
                    // Store only subgrid blocks (difference between all and main)
                    foreach (var battery in allPowerBlocks.Batteries)
                        if (!mainPowerBlocks.Batteries.Contains(battery))
                            subgridBatteries.Add(battery);
                    
                    foreach (var turbine in allPowerBlocks.WindTurbines)
                        if (!mainPowerBlocks.WindTurbines.Contains(turbine))
                            subgridWindTurbines.Add(turbine);
                    
                    foreach (var engine in allPowerBlocks.HydrogenEngines)
                        if (!mainPowerBlocks.HydrogenEngines.Contains(engine))
                            subgridHydrogenEngines.Add(engine);
                    
                    foreach (var panel in allPowerBlocks.SolarPanels)
                        if (!mainPowerBlocks.SolarPanels.Contains(panel))
                            subgridSolarPanels.Add(panel);
                    
                    foreach (var reactor in allPowerBlocks.Reactors)
                        if (!mainPowerBlocks.Reactors.Contains(reactor))
                            subgridReactors.Add(reactor);
                    
                    foreach (var producer in allPowerBlocks.AllPowerProducers)
                        if (!mainPowerBlocks.AllPowerProducers.Contains(producer))
                            subgridPowerProducers.Add(producer);
                    
                    // Handle tanks separately
                    foreach (var block in allBlocks)
                    {
                        if (block is IMyGasTank && !mainGridBlocks.Contains(block))
                            subgridTanks.Add((IMyGasTank)block);
                    }
                }
                
                // Get main grid power blocks
                var powerBlocks = MahUtillities.GetPowerBlocks(mainGridBlocks);
                
                // Merge main grid with cached subgrid blocks
                batteries.AddRange(powerBlocks.Batteries);
                batteries.AddRange(subgridBatteries);
                
                windTurbines.AddRange(powerBlocks.WindTurbines);
                windTurbines.AddRange(subgridWindTurbines);
                
                hydrogenEngines.AddRange(powerBlocks.HydrogenEngines);
                hydrogenEngines.AddRange(subgridHydrogenEngines);
                
                solarPanels.AddRange(powerBlocks.SolarPanels);
                solarPanels.AddRange(subgridSolarPanels);
                
                reactors.AddRange(powerBlocks.Reactors);
                reactors.AddRange(subgridReactors);
                
                powerProducers.AddRange(powerBlocks.AllPowerProducers);
                powerProducers.AddRange(subgridPowerProducers);
                
                // Handle main grid tanks
                foreach (var block in mainGridBlocks)
                {
                    if (block is IMyGasTank)
                        tanks.Add((IMyGasTank)block);
                }
                // Add cached subgrid tanks
                tanks.AddRange(subgridTanks);

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

                // Sort lists once here instead of using LINQ OrderBy in draw loops
                windTurbines.Sort((a, b) => string.Compare(((IMyTerminalBlock)a).CustomName, ((IMyTerminalBlock)b).CustomName, StringComparison.Ordinal));
                solarPanels.Sort((a, b) => string.Compare(((IMyTerminalBlock)a).CustomName, ((IMyTerminalBlock)b).CustomName, StringComparison.Ordinal));
                reactors.Sort((a, b) => string.Compare(((IMyTerminalBlock)a).CustomName, ((IMyTerminalBlock)b).CustomName, StringComparison.Ordinal));
                hydrogenEngines.Sort((a, b) => string.Compare(((IMyTerminalBlock)a).CustomName, ((IMyTerminalBlock)b).CustomName, StringComparison.Ordinal));
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenPowerSummary: Caught Exception while updating blocks: {e.ToString()}");
            }
        }

        void DrawPowerSummaryMainSprite(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            if (surfaceData.showHeader)
            {
                SurfaceDrawer.DrawPowerTimeHeaderSprite(ref frame, ref position, surfaceData, $"Power [{(searchId == "*" ? "All" : searchId)} -{excludeIds.Count}]", powerProducers);
                if (compactMode) position -= surfaceData.newLine;
            }
            if (surfaceData.showSummary)
            {
                var currentOutput = MahUtillities.GetCurrentOutput(powerProducers);
                var maxOutput = MahUtillities.GetMaxOutput(powerProducers);

                // Power overall
                SurfaceDrawer.DrawOutputSprite(ref frame, ref position, surfaceData, "PWR", currentOutput, maxOutput, showInactive, Unit.Watt, false);
                
                if (batteries.Count > 0 && showBatteries)
                    DrawBatterySprite(ref frame, ref position);
                // Spacing after batteries before category sections (omit when summaryOnly to keep bars tight)
                if (!compactMode && !summaryOnly)
                    position += surfaceData.newLine;
                // If this is a corner LCD, no more data will be visible.
                if (compactMode) return;

                // Categories
                // Reactors (show category only if there are reactors on the grid)
                if (showReactors && reactors.Count > 0)
                {
                    if (!summaryOnly)
                    {
                        // Category heading (single-line header)
                        SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, "Reactors", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                        position += surfaceData.newLine;
                    }
                    // Fuel load (Uranium) � average fill of all reactors vs UraniumMinAmount target
                    {
                        float target = uraniumMinAmount > 0 ? uraniumMinAmount : 500;
                        float totalRatio = 0f;
                        int counted = 0;
                        var itemsTmp = new List<VRage.Game.ModAPI.Ingame.MyInventoryItem>();
                        foreach (var rx in reactors)
                        {
                            try
                            {
                                var inv = rx.GetInventory(0);
                                itemsTmp.Clear();
                                float ingots = 0f;
                                if (inv != null && inv.ItemCount > 0)
                                {
                                    inv.GetItems(itemsTmp);
                                    foreach (var it in itemsTmp)
                                    {
                                        if (it == null) continue;
                                        var typeId = it.Type.TypeId.Split('_')[1];
                                        var subtypeId = it.Type.SubtypeId;
                                        if ((typeId == "Ingot" || typeId.IndexOf("Ingot", StringComparison.OrdinalIgnoreCase) >= 0) &&
                                            (subtypeId == "Uranium" || subtypeId.IndexOf("Uranium", StringComparison.OrdinalIgnoreCase) >= 0))
                                        {
                                            ingots += it.Amount.ToIntSafe();
                                        }
                                    }
                                }
                                float ratio = Math.Max(0f, Math.Min(1f, target <= 0f ? 0f : (ingots / target)));
                                totalRatio += ratio;
                                counted++;
                            }
                            catch { }
                        }
                        float avg = counted > 0 ? (totalRatio / counted) : 0f;
                        SurfaceDrawer.DrawBarFixedColor(ref frame, ref position, surfaceData, "U", avg, 1f, Color.Gold, Unit.Percent);
                    }
                    // Power output aggregate
                    SurfaceDrawer.DrawOutputSprite(ref frame, ref position, surfaceData, "REA", reactors.Sum(block => block.CurrentOutput), reactors.Sum(block => block.MaxOutput), showInactive, Unit.Watt, false);
                    // Detailed list only when not in summaryOnly mode
                    if (!summaryOnly)
                    {
                        DrawReactorsListSprite(ref frame, ref position);
                    }
                    // Blank line between categories (omit when summaryOnly)
                    if (!summaryOnly)
                        position += surfaceData.newLine;
                }

                // Solar Panels
                if (showSolar && solarPanels.Count > 0)
                {
                    if (!summaryOnly)
                    {
                        // Category heading (single-line header)
                        SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, "Solar Panels", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                        position += surfaceData.newLine;
                    }
                    // Aggregates (SOL/EXP)
                    DrawSolarSprite(ref frame, ref position);
                    // Detailed list only when not in summaryOnly mode
                    if (!summaryOnly)
                        DrawSolarPanelsListSprite(ref frame, ref position);
                    // Blank line between categories (omit when summaryOnly)
                    if (!summaryOnly)
                        position += surfaceData.newLine;
                }

                // Wind Turbines
                if (showWind && windTurbines.Count > 0)
                {
                    if (!summaryOnly)
                    {
                        // Category heading (single-line header)
                        SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, "Wind Turbines", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                        position += surfaceData.newLine;
                    }
                    // Aggregates (WND/EFF)
                    DrawWindTurbinesSprite(ref frame, ref position);
                    // Detailed list only when not in summaryOnly mode
                    if (!summaryOnly)
                        DrawWindTurbinesListSprite(ref frame, ref position);
                    // Blank line between categories (omit when summaryOnly)
                    if (!summaryOnly)
                        position += surfaceData.newLine;
                }

                // Engines (Hydrogen) � show category when engines or tanks exist
                if (showEngines && (hydrogenEngines.Count > 0 || tanks.Count > 0))
                {
                    if (!summaryOnly)
                    {
                        // Category heading (single-line header)
                        SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, "Engines", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                        position += surfaceData.newLine;
                    }
                    // Engine power output aggregate
                    if (hydrogenEngines.Count > 0)
                        DrawHydrogenEnginesSprite(ref frame, ref position);
                    // Hydrogen fuel (tanks) � only relevant when engines exist
                    if (tanks.Count > 0)
                        SurfaceDrawer.DrawGasTankSprite(ref frame, ref position, surfaceData, "Hydrogen", "HYD", tanks, true);
                    // Per-engine rows only when not in summaryOnly mode
                    if (!summaryOnly && hydrogenEngines.Count > 0)
                        DrawHydrogenEnginesListSprite(ref frame, ref position);
                    // Blank line after category (omit when summaryOnly)
                    if (!summaryOnly)
                        position += surfaceData.newLine;
                }
            }

            if (!summaryOnly)
                position += surfaceData.newLine;
        }

        void DrawBatterySprite (ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            try
            {
                SurfaceDrawer.DrawOutputSprite(ref frame, ref position, surfaceData, "BAT", batteries.Sum(block => block.CurrentStoredPower), batteries.Sum(block => block.MaxStoredPower), showInactive, Unit.WattHours, true);

                // Always show battery output/input bars, even in compact mode
                SurfaceDrawer.DrawOutputSprite(ref frame, ref position, surfaceData, " <<", batteries.Sum(block => block.CurrentOutput), batteries.Sum(block => block.MaxOutput), showInactive, Unit.Watt, false);
                SurfaceDrawer.DrawOutputSprite(ref frame, ref position, surfaceData, " >>", batteries.Sum(block => block.CurrentInput), batteries.Sum(block => block.MaxInput), showInactive, Unit.Watt, false);

                // If this is a corner LCD, no more data will be visible after the battery section.
                if (compactMode) return;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenPowerSummary: Caught Exception while DrawBatterySprite: {e.ToString()}");
            }
        }

        void DrawSolarSprite (ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            try
            {
                if (solarPanels == null || solarPanels.Count <= 0) return;
                var currentOuput = solarPanels.Sum(block => block.CurrentOutput);

                if (currentOuput > 0 || showInactive)
                {
                    // Current Output
                    SurfaceDrawer.DrawOutputSprite(ref frame, ref position, surfaceData, "SOL", currentOuput, solarPanels.Sum(block => block.MaxOutput), showInactive, Unit.Watt, true);
                    // Exposure to sunlight (current max to absolute max)
                    SurfaceDrawer.DrawOutputSprite(ref frame, ref position, surfaceData, "EXP", solarPanels.Sum(block => block.MaxOutput), solarPanels.Sum(block => block.Components.Get<MyResourceSourceComponent>().DefinedOutput), showInactive, Unit.Percent, true);
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenPowerSummary: Caught Exception while DrawSolarSprite: {e.ToString()}");
            }
        }

        void DrawWindTurbinesSprite (ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            try
            {
                if (windTurbines == null || windTurbines.Count <= 0) return;
                var currentOuput = windTurbines.Sum(block => block.CurrentOutput);
                var outputMax = windTurbines.Sum(block => block.MaxOutput);
                var definedMax = windTurbines.Sum(block => block.Components.Get<MyResourceSourceComponent>().DefinedOutput);

                if ((currentOuput > 0 || showInactive) && outputMax > 0)
                {
                    // Current Output
                    SurfaceDrawer.DrawOutputSprite(ref frame, ref position, surfaceData, "WND", currentOuput, outputMax, showInactive, Unit.Watt, false);
                    // Current max (depending on wind) to absolute max
                    SurfaceDrawer.DrawOutputSprite(ref frame, ref position, surfaceData, "EFF", outputMax, definedMax, showInactive, Unit.Percent, false);
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenPowerSummary: Caught Exception while DrawWindTurbinesSprite: {e.ToString()}");
            }
        }

        // New: per-turbine rows with on/off badge, name, and efficiency bar
        void DrawWindTurbinesListSprite (ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            try
            {
                if (windTurbines.Count <= 0) return;

                // Sort wind turbines alphabetically by custom name
                MahSorting.SortBlocksByName(windTurbines);

                int maxNameLength = (int)(mySurface.SurfaceSize.X > 300 ? 35 : 20);
                
                // Calculate available lines for data
                float screenHeight = mySurface.SurfaceSize.Y;
                float lineHeight = 30 * surfaceData.textSize;
                int totalScreenLines = (int)((screenHeight - surfaceData.viewPortOffsetY) / lineHeight);
                int availableDataLines = Math.Max(1, totalScreenLines);
                
                // Apply scrolling if enabled
                int totalDataLines = windTurbines.Count;
                int startIndex = 0;
                
                if (toggleScroll && totalDataLines > 0)
                {
                    // Normalize scroll offset to stay within bounds (use local variable)
                    int normalizedOffset = ((scrollOffset % totalDataLines) + totalDataLines) % totalDataLines;
                    startIndex = normalizedOffset;
                }

                // Draw turbines with scrolling/wrapping
                int linesDrawn = 0;
                for (int i = 0; i < totalDataLines && linesDrawn < availableDataLines; i++)
                {
                    int turbineIndex = (startIndex + i) % totalDataLines;
                    var wt = windTurbines[turbineIndex];
                    
                    var tb = wt as IMyTerminalBlock;
                    if (tb == null) continue;

                    string state = tb.IsFunctional ? (tb.IsWorking ? "   On" : "   Off") : "Broken";
                    var stateColor = !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : state.Contains("Off") ? Color.Orange : state.Contains("On") ? Color.GreenYellow : Color.Red;

                    string name = tb.CustomName;
                    if (name.Length > maxNameLength) name = name.Substring(0, maxNameLength);

                    var rsc = wt.Components.Get<MyResourceSourceComponent>();
                    float definedMax = rsc != null ? (float)rsc.DefinedOutput : 0f;
                    float current = (float)wt.CurrentOutput; // actual current power output
                    float total = definedMax <= 0f ? 1f : definedMax; // absolute maximum
                    float ratio = definedMax <= 0f ? 0f : current / definedMax; // efficiency represented by bar

                    // Left: on/off badge + name shell
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"  {state}", TextAlignment.LEFT, stateColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"[             ] {name}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);

                    // Right: efficiency bar (outputMax vs definedMax)
                    var barColor = !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor :
                        ratio > .95f ? Color.GreenYellow : ratio > .75f ? Color.Yellow : ratio > .25f ? Color.Orange : ratio > 0f ? Color.Red : surfaceData.surface.ScriptForegroundColor;
                    // Show power output as the number, while the bar shows efficiency
                    SurfaceDrawer.DrawHalfBar(ref frame, position, surfaceData, TextAlignment.RIGHT, current, total, Unit.Watt, barColor);

                    position += surfaceData.newLine;
                    linesDrawn++;
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenPowerSummary: Caught Exception while DrawWindTurbinesListSprite: {e.ToString()}");
            }
        }

        // New: per-solar-panel rows with on/off badge, name, and exposure bar
        void DrawSolarPanelsListSprite (ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            try
            {
                if (solarPanels.Count <= 0) return;

                // Sort solar panels alphabetically by custom name
                MahSorting.SortBlocksByName(solarPanels);

                int maxNameLength = (int)(mySurface.SurfaceSize.X > 300 ? 35 : 20);
                
                // Calculate available lines for data
                float screenHeight = mySurface.SurfaceSize.Y;
                float lineHeight = 30 * surfaceData.textSize;
                int totalScreenLines = (int)((screenHeight - surfaceData.viewPortOffsetY) / lineHeight);
                int availableDataLines = Math.Max(1, totalScreenLines);
                
                // Apply scrolling if enabled
                int totalDataLines = solarPanels.Count;
                int startIndex = 0;
                
                if (toggleScroll && totalDataLines > 0)
                {
                    // Normalize scroll offset to stay within bounds (use local variable)
                    int normalizedOffset = ((scrollOffset % totalDataLines) + totalDataLines) % totalDataLines;
                    startIndex = normalizedOffset;
                }

                // Draw panels with scrolling/wrapping
                int linesDrawn = 0;
                for (int i = 0; i < totalDataLines && linesDrawn < availableDataLines; i++)
                {
                    int panelIndex = (startIndex + i) % totalDataLines;
                    var sp = solarPanels[panelIndex];
                    
                    var tb = sp as IMyTerminalBlock;
                    if (tb == null) continue;

                    string state = tb.IsFunctional ? (tb.IsWorking ? "   On" : "   Off") : "Broken";
                    var stateColor = !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : state.Contains("Off") ? Color.Orange : state.Contains("On") ? Color.GreenYellow : Color.Red;

                    string name = tb.CustomName;
                    if (name.Length > maxNameLength) name = name.Substring(0, maxNameLength);

                    var rsc = sp.Components.Get<MyResourceSourceComponent>();
                    float definedMax = rsc != null ? (float)rsc.DefinedOutput : 0f;
                    float exposureCurrent = (float)sp.MaxOutput; // exposure represented by current max
                    float exposureTotal = definedMax <= 0f ? 1f : definedMax; // absolute maximum
                    float ratio = definedMax <= 0f ? 0f : exposureCurrent / definedMax;

                    // Left: on/off badge + name shell
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"  {state}", TextAlignment.LEFT, stateColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"[             ] {name}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);

                    // Right: exposure half bar (MaxOutput vs DefinedOutput) with watt number at the same position as wind rows
                    var barColor = !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor :
                        ratio > .95f ? Color.GreenYellow : ratio > .75f ? Color.Yellow : ratio > .25f ? Color.Orange : ratio > 0f ? Color.Red : surfaceData.surface.ScriptForegroundColor;
                    DrawRightHalfBarWithLabel(ref frame, position, MahDefinitions.WattFormat(sp.CurrentOutput), exposureCurrent, exposureTotal, barColor);

                    position += surfaceData.newLine;
                    linesDrawn++;
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenPowerSummary: Caught Exception while DrawSolarPanelsListSprite: {e.ToString()}");
            }
        }

        // New: per-reactor rows with on/off badge, name, and fuel fill bar
        void DrawReactorsListSprite (ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            try
            {
                if (reactors.Count <= 0) return;

                // Sort reactors alphabetically by custom name
                MahSorting.SortBlocksByName(reactors);

                int maxNameLength = (int)(mySurface.SurfaceSize.X > 300 ? 35 : 20);
                
                // Calculate available lines for data
                float screenHeight = mySurface.SurfaceSize.Y;
                float lineHeight = 30 * surfaceData.textSize;
                int totalScreenLines = (int)((screenHeight - surfaceData.viewPortOffsetY) / lineHeight);
                int availableDataLines = Math.Max(1, totalScreenLines);
                
                // Apply scrolling if enabled
                int totalDataLines = reactors.Count;
                int startIndex = 0;
                
                if (toggleScroll && totalDataLines > 0)
                {
                    // Normalize scroll offset to stay within bounds (use local variable)
                    int normalizedOffset = ((scrollOffset % totalDataLines) + totalDataLines) % totalDataLines;
                    startIndex = normalizedOffset;
                }

                // Draw reactors with scrolling/wrapping
                int linesDrawn = 0;
                for (int i = 0; i < totalDataLines && linesDrawn < availableDataLines; i++)
                {
                    int reactorIndex = (startIndex + i) % totalDataLines;
                    var rx = reactors[reactorIndex];
                    
                    var tb = rx as IMyTerminalBlock;
                    if (tb == null) continue;

                    string state = tb.IsFunctional ? (tb.IsWorking ? "   On" : "   Off") : "Broken";
                    var stateColor = !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : state.Contains("Off") ? Color.Orange : state.Contains("On") ? Color.GreenYellow : Color.Red;

                    string name = tb.CustomName;
                    if (name.Length > maxNameLength) name = name.Substring(0, maxNameLength);

                    // Compute reactor uranium ingot count and map to configured target per reactor (UraniumMinAmount)
                    float targetIngots = uraniumMinAmount > 0 ? (float)uraniumMinAmount : 500f;
                    float ingotCount = 0f;
                    var inv = rx.GetInventory(0);
                    if (inv != null && inv.ItemCount > 0)
                    {
                        var items = new List<VRage.Game.ModAPI.Ingame.MyInventoryItem>();
                        inv.GetItems(items);
                        foreach (var item in items.OrderBy(itm => itm.Type.SubtypeId))
                        {
                            if (item == null) continue;
                            var typeId = item.Type.TypeId.Split('_')[1];
                            var subtypeId = item.Type.SubtypeId;
                            if ((typeId == "Ingot" || typeId.Contains("Ingot")) && (subtypeId == "Uranium" || subtypeId.Contains("Uranium")))
                            {
                                ingotCount += item.Amount.ToIntSafe();
                            }
                        }
                    }
                    float fill = Math.Max(0f, Math.Min(1f, targetIngots <= 0f ? 0f : (ingotCount / targetIngots)));

                    // Left: on/off badge + name shell
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"  {state}", TextAlignment.LEFT, stateColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"[             ] {name}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);

                    // Right: fill% bar (fuel) with power output number placed like wind rows
                    float cur = fill; // 0..1
                    float tot = 1f;
                    var barColor = !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor :
                        cur > .95f ? Color.GreenYellow : cur > .75f ? Color.Yellow : cur > .25f ? Color.Orange : cur > 0f ? Color.Red : surfaceData.surface.ScriptForegroundColor;
                    DrawRightHalfBarWithLabel(ref frame, position, MahDefinitions.WattFormat(rx.CurrentOutput), cur, tot, barColor);

                    position += surfaceData.newLine;
                    linesDrawn++;
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenPowerSummary: Caught Exception while DrawReactorsListSprite: {e.ToString()}");
            }
        }

        float ParseReactorTimeLeftSeconds(string detailedInfo)
        {
            try
            {
                if (string.IsNullOrEmpty(detailedInfo)) return 0f;
                string[] lines = detailedInfo.Split('\n');
                foreach (var raw in lines)
                {
                    var line = raw.Trim();
                    if (line.Length == 0) continue;
                    // Look for a line indicating time remaining
                    if (line.IndexOf("time", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        // Extract total seconds from patterns like "X h", "Y min", "Z sec"
                        float seconds = 0f;
                        seconds += ExtractUnit(line, 'h') * 3600f;
                        seconds += ExtractUnit(line, 'm') * 60f;
                        seconds += ExtractUnit(line, 's');
                        if (seconds > 0f) return seconds;
                    }
                }
            }
            catch { }
            return 0f;
        }

        float ExtractUnit(string line, char unit)
        {
            try
            {
                // Find tokens ending with unit, e.g., "2 h", "30 min", "45 s"
                var parts = line.Split(' ');
                for (int i = 0; i < parts.Length - 0; i++)
                {
                    var p = parts[i].Trim();
                    if (p.Length == 0) continue;
                    if (unit == 'h' && (p.Equals("h", StringComparison.OrdinalIgnoreCase) || p.StartsWith("h")))
                    {
                        // previous token expected to be number
                        if (i > 0)
                        {
                            float v = 0f;
                            if (float.TryParse(parts[i - 1].Trim(), out v)) return v;
                        }
                    }
                    if (unit == 'm' && (p.StartsWith("min", StringComparison.OrdinalIgnoreCase) || p.Equals("m", StringComparison.OrdinalIgnoreCase)))
                    {
                        if (i > 0)
                        {
                            float v = 0f;
                            if (float.TryParse(parts[i - 1].Trim(), out v)) return v;
                        }
                    }
                    if (unit == 's' && (p.StartsWith("sec", StringComparison.OrdinalIgnoreCase) || p.Equals("s", StringComparison.OrdinalIgnoreCase)))
                    {
                        if (i > 0)
                        {
                            float v = 0f;
                            if (float.TryParse(parts[i - 1].Trim(), out v)) return v;
                        }
                    }
                }
            }
            catch { }
            return 0f;
        }

        // New: per-engine rows with on/off badge, name, and filled% bar
        void DrawHydrogenEnginesListSprite (ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            try
            {
                var engines = hydrogenEngines;
                if (engines.Count <= 0) return;

                // Sort hydrogen engines alphabetically by custom name
                MahSorting.SortBlocksByName(engines);

                int maxNameLength = (int)(mySurface.SurfaceSize.X > 300 ? 35 : 20);
                
                // Calculate available lines for data
                float screenHeight = mySurface.SurfaceSize.Y;
                float lineHeight = 30 * surfaceData.textSize;
                int totalScreenLines = (int)((screenHeight - surfaceData.viewPortOffsetY) / lineHeight);
                int availableDataLines = Math.Max(1, totalScreenLines);
                
                // Apply scrolling if enabled
                int totalDataLines = engines.Count;
                int startIndex = 0;
                
                if (toggleScroll && totalDataLines > 0)
                {
                    // Normalize scroll offset to stay within bounds (use local variable)
                    int normalizedOffset = ((scrollOffset % totalDataLines) + totalDataLines) % totalDataLines;
                    startIndex = normalizedOffset;
                }

                // Draw engines with scrolling/wrapping
                int linesDrawn = 0;
                for (int i = 0; i < totalDataLines && linesDrawn < availableDataLines; i++)
                {
                    int engineIndex = (startIndex + i) % totalDataLines;
                    var eng = engines[engineIndex];
                    
                    var tb = eng as IMyTerminalBlock;
                    if (tb == null) continue;
                    if (tb.BlockDefinition.SubtypeName.IndexOf("Hydrogen", StringComparison.OrdinalIgnoreCase) < 0) continue;

                    string state = tb.IsFunctional ? (tb.IsWorking ? "   On" : "   Off") : "Broken";
                    var stateColor = !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : state.Contains("Off") ? Color.Orange : state.Contains("On") ? Color.GreenYellow : Color.Red;

                    string name = tb.CustomName;
                    if (name.Length > maxNameLength) name = name.Substring(0, maxNameLength);

                    // Parse 'Filled %' from DetailedInfo
                    float filledRatio = ParseFilledPercent(tb.DetailedInfo);

                    // Left: on/off badge + name shell
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"  {state}", TextAlignment.LEFT, stateColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"[             ] {name}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);

                    // Right: filled% bar with engine power output number placed like wind rows
                    float cur = filledRatio; // 0..1
                    float tot = 1f;
                    var barColor = !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor :
                        cur > .95f ? Color.GreenYellow : cur > .75f ? Color.Yellow : cur > .25f ? Color.Orange : cur > 0f ? Color.Red : surfaceData.surface.ScriptForegroundColor;
                    DrawRightHalfBarWithLabel(ref frame, position, MahDefinitions.WattFormat(((IMyPowerProducer)eng).CurrentOutput), cur, tot, barColor);

                    position += surfaceData.newLine;
                    linesDrawn++;
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenPowerSummary: Caught Exception while DrawHydrogenEnginesListSprite: {e.ToString()}");
            }
        }

        float ParseFilledPercent(string detailedInfo)
        {
            try
            {
                if (string.IsNullOrEmpty(detailedInfo)) return 0f;
                var lines = detailedInfo.Split('\n');
                foreach (var raw in lines)
                {
                    var line = raw.Trim();
                    if (line.IndexOf("Filled", StringComparison.OrdinalIgnoreCase) >= 0 && line.IndexOf('%') >= 0)
                    {
                        // Extract number before %
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

        void DrawHydrogenEnginesSprite (ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            try
            {
                var currentOuput = hydrogenEngines.Sum(block => block.CurrentOutput);

                if (currentOuput > 0 || showInactive)
                {
                    SurfaceDrawer.DrawOutputSprite(ref frame, ref position, surfaceData, "ENG", hydrogenEngines.Sum(block => block.CurrentOutput), hydrogenEngines.Sum(block => block.MaxOutput), showInactive, Unit.Watt, false);
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenPowerSummary: Caught Exception while DrawHydrogenEnginesSprite: {e.ToString()}");
            }
        }

        // Helper: draw a right-aligned half bar with a custom right-side label (e.g., watt string)
        // This mirrors SurfaceDrawer.DrawHalfBar(TextAlignment.RIGHT) but lets us supply our own label text
        void DrawRightHalfBarWithLabel(ref MySpriteDrawFrame frame, Vector2 position, string label, float current, float total, Color barColor)
        {
            try
            {
                // Avoid division by 0
                total = total <= 0 ? 1 : total;

                float pixelPerChar = MahDefinitions.pixelPerChar * surfaceData.textSize;
                int barLength = (int)((surfaceData.ratioOffset / pixelPerChar) * .8f);
                float ratio = current / total;
                int currentValue = (int)(ratio * barLength);

                string backgroundBar = "";
                string bar = "";

                for (int i = 0; i < barLength; i++) backgroundBar += "'";
                for (int i = 0; i < barLength; i++) if (i < currentValue) bar += "|";

                // Draw label followed by bracket bar shell, right aligned, then overlay the filled portion
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{label} [{backgroundBar}]", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                if (current > 0)
                {
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $" {bar} ", TextAlignment.RIGHT, surfaceData.useColors ? barColor : surfaceData.surface.ScriptForegroundColor);
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenPowerSummary: Caught Exception while DrawRightHalfBarWithLabel: {e.ToString()}");
            }
        }
    }
}
