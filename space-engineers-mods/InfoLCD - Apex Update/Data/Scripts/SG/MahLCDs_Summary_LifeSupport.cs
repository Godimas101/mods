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
    [MyTextSurfaceScript("LCDInfoScreenLifeSupportSummary", "$IOS LCD - LifeSupport")]
    public class LCDLifeSupportSummary : MyTextSurfaceScriptBase
    {
        MyIni config = new MyIni();

        public static string CONFIG_SECTION_ID = "SettingsLifeSupportStatus";

        string searchId = "*";
        bool showBatteries = true;
        bool showHydrogen = true;
        bool showOxygen = true;
    bool showIce = true;
    // User-configurable Ice target (item count), defaults to 50000
    int iceMinAmount = 50000;

        void TryCreateSurfaceData()
        {
            if (surfaceData != null)
                return;

            compactMode = mySurface.SurfaceSize.X / mySurface.SurfaceSize.Y > 4;
            var textSize = compactMode ? .4f : .45f;

            surfaceData = new SurfaceDrawer.SurfaceData
            {
                surface = mySurface,
                textSize = textSize,
                titleOffset = 128,
                ratioOffset = 96,
                viewPortOffsetX = 12,
                viewPortOffsetY = 12,
                newLine = new Vector2(0, 30 * textSize),
                showHeader = true,
                showSummary = true,
                showMissing = false,
                showRatio = false,
                showBars = true,
                showSubgrids = false,
                useColors = true,
                showVent = true
            };
        }

        void CreateConfig()
        {
            TryCreateSurfaceData();

            // Determine default IceMinAmount from definitions
            var iceDef = MahDefinitions.GetDefinition("Ore", "Ice");
            iceMinAmount = (iceMinAmount > 0) ? iceMinAmount : (iceDef != null && iceDef.minAmount > 0 ? iceDef.minAmount : 50000);

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
            sb.AppendLine("; [ LIFESUPPORT - GENERAL OPTIONS ]");
            ConfigHelpers.AppendSearchIdConfig(sb, searchId);
            ConfigHelpers.AppendExcludeIdsConfig(sb, excludeIds, "Airlock,");
            ConfigHelpers.AppendShowHeaderConfig(sb, surfaceData.showHeader);
            ConfigHelpers.AppendShowSubgridsConfig(sb, surfaceData.showSubgrids);
            ConfigHelpers.AppendSubgridUpdateFrequencyConfig(sb, surfaceData.subgridUpdateFrequency);
            ConfigHelpers.AppendShowDockedConfig(sb, surfaceData.showDocked);
            ConfigHelpers.AppendUseColorsConfig(sb, surfaceData.useColors);

            sb.AppendLine();
            sb.AppendLine("; [ LIFESUPPORT - LAYOUT OPTIONS ]");
            sb.AppendLine($"TextSize={surfaceData.textSize}");
            sb.AppendLine($"ViewPortOffsetX={surfaceData.viewPortOffsetX}");
            sb.AppendLine($"ViewPortOffsetY={surfaceData.viewPortOffsetY}");
            sb.AppendLine($"TitleFieldWidth={surfaceData.titleOffset}");
            sb.AppendLine($"RatioFieldWidth={surfaceData.ratioOffset}");

            sb.AppendLine();
            sb.AppendLine("; [ LIFESUPPORT - SCREEN OPTIONS ]");
            sb.AppendLine($"ShowBatteries={showBatteries}");
            sb.AppendLine($"ShowHydrogen={showHydrogen}");
            sb.AppendLine($"ShowOxygen={showOxygen}");
            sb.AppendLine($"ShowIce={showIce}");
            sb.AppendLine($"ShowVent={surfaceData.showVent}");
            sb.AppendLine($"ShowIntakeVent={surfaceData.showIntakeVent}");

            sb.AppendLine();
            sb.AppendLine("; [ LIFESUPPORT - ITEM THRESHOLDS ]");
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
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowHydrogen", ref showHydrogen, ref configError);
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowOxygen", ref showOxygen, ref configError);
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowIce", ref showIce, ref configError);
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "UseColors", ref surfaceData.useColors, ref configError);
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowVent"))
                        surfaceData.showVent = config.Get(CONFIG_SECTION_ID, "ShowVent").ToBoolean();
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
                    MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenLifeSupportSummary: Config Syntax error at Line {result}");
                }
            }
            catch(Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenLifeSupportSummary: Caught Exception while loading config: {e.ToString()}");
            }
        }

        string ExtractOurConfigSection(string customData)
        {
            // Extract only the lines between our category headers
            string startMarker = "; [ LIFESUPPORT - GENERAL OPTIONS ]";
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
        List<IMyGasGenerator> generators = new List<IMyGasGenerator>();
        List<IMyReactor> reactors = new List<IMyReactor>();
        List<IMyGasTank> tanks = new List<IMyGasTank>();
        List<IMyPowerProducer> powerProducers = new List<IMyPowerProducer>();
    List<IMyAirVent> airVents = new List<IMyAirVent>();

        // Cached subgrid collections
        List<IMyBatteryBlock> subgridBatteries = new List<IMyBatteryBlock>();
        List<IMyGasGenerator> subgridGenerators = new List<IMyGasGenerator>();
        List<IMyReactor> subgridReactors = new List<IMyReactor>();
        List<IMyGasTank> subgridTanks = new List<IMyGasTank>();
        List<IMyPowerProducer> subgridPowerProducers = new List<IMyPowerProducer>();
        List<IMyAirVent> subgridAirVents = new List<IMyAirVent>();

    // Reusable lists to avoid GC allocations
    List<VRage.Game.ModAPI.Ingame.MyInventoryItem> _cachedInventoryItems = new List<VRage.Game.ModAPI.Ingame.MyInventoryItem>();

        VRage.Collections.DictionaryValuesReader<MyDefinitionId, MyDefinitionBase> myDefinitions;
        MyDefinitionId myDefinitionId;
        SurfaceDrawer.SurfaceData surfaceData;
        string gridId = "Unknown grid";
        int subgridScanTick = 0;

        float reactorsCurrentVolume = 0.0f;
        float reactorsMaximumVolume = 0.0f;
        float currentIceVolume = 0.0f;
        float maximumIceVolume = 0.0f;

        int hydrogenTanks = 0;
        int oxygenTanks = 0;
        bool configError = false;
        bool compactMode = false;
        bool isStation = false;
        Sandbox.ModAPI.Ingame.MyShipMass gridMass;

        public LCDLifeSupportSummary(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
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
            UpdateIceContents();

            var myFrame = mySurface.DrawFrame();
            var myViewport = new RectangleF((mySurface.TextureSize - mySurface.SurfaceSize) / 2f, mySurface.SurfaceSize);
            var myPosition = new Vector2(surfaceData.viewPortOffsetX, surfaceData.viewPortOffsetY) + myViewport.Position;
            myDefinitions = MyDefinitionManager.Static.GetAllDefinitions();

            if (configError)
                SurfaceDrawer.DrawErrorSprite(ref myFrame, surfaceData, $"<< Config error. Please Delete CustomData >>", Color.Orange);
            else
            {
                DrawLifeSupportMainSprite(ref myFrame, ref myPosition);
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

                    subgridReactors.Clear();
                    foreach (var rea in allPowerBlocks.Reactors)
                        if (!mainPowerBlocks.Reactors.Contains(rea))
                            subgridReactors.Add(rea);

                    subgridPowerProducers.Clear();
                    foreach (var pow in allPowerBlocks.AllPowerProducers)
                        if (!mainPowerBlocks.AllPowerProducers.Contains(pow))
                            subgridPowerProducers.Add(pow);

                    // Extract subgrid-only blocks for generators, tanks, and air vents
                    subgridGenerators.Clear();
                    subgridTanks.Clear();
                    subgridAirVents.Clear();
                    foreach (var block in allBlocks)
                    {
                        if (!mainBlocks.Contains(block))
                        {
                            if (block is IMyGasGenerator)
                                subgridGenerators.Add((IMyGasGenerator)block);
                            else if (block is IMyGasTank)
                                subgridTanks.Add((IMyGasTank)block);
                            else if (block is IMyAirVent)
                                subgridAirVents.Add((IMyAirVent)block);
                        }
                    }
                }

                // Merge main (fresh) and subgrid (cached) collections
                batteries.Clear();
                batteries.AddRange(mainPowerBlocks.Batteries);
                batteries.AddRange(subgridBatteries);

                reactors.Clear();
                reactors.AddRange(mainPowerBlocks.Reactors);
                reactors.AddRange(subgridReactors);

                powerProducers.Clear();
                powerProducers.AddRange(mainPowerBlocks.AllPowerProducers);
                powerProducers.AddRange(subgridPowerProducers);

                // Categorize main grid blocks for other types
                generators.Clear();
                tanks.Clear();
                airVents.Clear();
                foreach (var myBlock in mainBlocks)
                {
                    if (myBlock == null) continue;

                    if (myBlock is IMyGasGenerator)
                    {
                        generators.Add((IMyGasGenerator)myBlock);
                    }
                    else if (myBlock is IMyGasTank)
                    {
                        tanks.Add((IMyGasTank)myBlock);
                    }
                    else if (myBlock is IMyAirVent)
                    {
                        airVents.Add((IMyAirVent)myBlock);
                    }
                }

                // Merge subgrid collections
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
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenLifeSupportSummary: Caught Exception while updating blocks: {e.ToString()}");
            }
        }

        void UpdateIceContents()
        {
            try
            {
                currentIceVolume = 0.0f;
                maximumIceVolume = 0.0f;

                CargoItemDefinition iceDefinition = MahDefinitions.GetDefinition("Ore", "Ice");
                float iceItemVolumeL = 0f;
                if (iceDefinition != null)
                    iceItemVolumeL = iceDefinition.volume; // volume is already in liters

                var myCubeGrid = myTerminalBlock.CubeGrid as MyCubeGrid;
                if (myCubeGrid != null)
                {
                    Sandbox.ModAPI.Ingame.MyShipMass tmpMass = gridMass;
                    var allBlocks = MahUtillities.GetBlocks(myCubeGrid, searchId, excludeIds, ref tmpMass, surfaceData.showSubgrids);
                    if (allBlocks != null && allBlocks.Count > 0)
                    {
                        var iceData = MahUtillities.GetGridIceData(allBlocks, iceItemVolumeL, _cachedInventoryItems);
                        currentIceVolume = iceData.currentIceVolumeL;
                        maximumIceVolume = iceData.maxIceVolumeL;
                    }
                }

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
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenLifeSupportSummary: Caught Exception while updating ice contents: {e.ToString()}");
            }
        }

        void DrawLifeSupportMainSprite (ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            try
            {
                if (surfaceData.showHeader)
                {
                    SurfaceDrawer.DrawPowerTimeHeaderSprite(ref frame, ref position, surfaceData, $"Life Support [{(searchId == "*" ? "All" : searchId)} -{excludeIds.Count}]", powerProducers);
                    if (compactMode) position -= surfaceData.newLine;
                }

                if (showBatteries)
                {
                    SurfaceDrawer.DrawOutputSprite(ref frame, ref position, surfaceData, "BAT", batteries.Sum(block => block.CurrentStoredPower), batteries.Sum(block => block.MaxStoredPower), true, Unit.WattHours, true);
                }
                if (tanks.Count > 0)
                {
                    if (showOxygen) SurfaceDrawer.DrawGasTankSprite(ref frame, ref position, surfaceData, "Oxygen", $"OXY ({oxygenTanks.ToString()})", tanks, true);
                    if (showHydrogen) SurfaceDrawer.DrawGasTankSprite(ref frame, ref position, surfaceData, "Hydrogen", $"HYD ({hydrogenTanks.ToString()})", tanks, true);
                }
                if (generators.Count > 0 && showIce)
                {
                    // Use Ice minAmount (user-configurable) as the 100% target
                    float targetIceLiters = maximumIceVolume;
                    CargoItemDefinition iceDefLS = MahDefinitions.GetDefinition("Ore", "Ice");
                    int targetItems = (iceMinAmount > 0 ? iceMinAmount : (iceDefLS != null ? iceDefLS.minAmount : 50000));
                    if (iceDefLS != null && targetItems > 0)
                    {
                        float itemL = iceDefLS.volume; // volume is already in liters
                        targetIceLiters = targetItems * itemL;
                    }
                    SurfaceDrawer.DrawBarFixedColor(ref frame, ref position, surfaceData, "Ice", currentIceVolume, targetIceLiters, Color.Aquamarine, Unit.Liters);
                }
                position += surfaceData.newLine + surfaceData.newLine;

                if (surfaceData.showVent)
                {
                    DrawAirVentsList(ref frame, ref position);
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenLifeSupportSummary: Caught Exception while DrawLifeSupportSummarySprite: {e.ToString()}");
            }
        }

        void DrawAirVentsList(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            try
            {
                // Clean nulls
                airVents.RemoveAll(v => v == null);

                // Sort air vents by custom name alphabetically
                MahSorting.SortBlocksByName(airVents);

                // Count visible vents (all if showIntakeVent, else only non-depressurizing)
                int visibleCount = 0;
                if (surfaceData.showIntakeVent)
                {
                    visibleCount = airVents.Count;
                }
                else
                {
                    foreach (var v in airVents)
                    {
                        if (v != null && !v.Depressurize) visibleCount++;
                    }
                }

                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Air Vents [{visibleCount}]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine;

                if (visibleCount == 0)
                {
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, "- No vents detected.", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    position += surfaceData.newLine;
                    return;
                }

                foreach (var vent in airVents)
                {
                    if (vent == null) continue;
                    // Skip depressurizing vents if not showing intake vents
                    if (!surfaceData.showIntakeVent && vent.Depressurize) continue;

                    var name = vent.CustomName;
                    float level = 0f;
                    string statusText = "";
                    string statusBracket = "";
                    string statusWordOnly = "";
                    bool isIntake = false;
                    Color statusColor = surfaceData.surface.ScriptForegroundColor;
                    try
                    {
                        level = vent.GetOxygenLevel();
                        statusText = vent.Status.ToString();
                        isIntake = vent.Depressurize; // Depressurize mode = air intake
                        var upper = statusText.ToUpperInvariant();
                        // Normalize status labels and assign colors
                        string statusWord = upper;
                        if (upper.StartsWith("DEPRESSURIZ"))
                        {
                            if (upper == "DEPRESSURIZING")
                            {
                                statusWord = "DEPRESSURIZING";
                                statusColor = Color.Gold;
                            }
                            else
                            {
                                statusWord = "DEPRESSURIZED";
                                statusColor = Color.IndianRed;
                            }
                        }
                        else if (upper.StartsWith("PRESSURIZ"))
                        {
                            if (upper == "PRESSURIZING")
                            {
                                statusWord = "PRESSURIZING";
                                statusColor = Color.Gold;
                            }
                            else
                            {
                                statusWord = "PRESSURIZED";
                                statusColor = Color.YellowGreen;
                            }
                        }
                        else
                        {
                            // Fallback/unknown: keep default color
                            statusWord = upper;
                            statusColor = surfaceData.surface.ScriptForegroundColor;
                        }

                        // We'll draw a constant-width bracket shell in white, then overlay the centered status word in color (keeps brackets white, width constant)
                        const int innerWidth = 30; // slightly wider than "DEPRESSURIZING" for proportional font
                        if (isIntake)
                        {
                            // Override label/color for dedicated intake mode
                            statusWord = "AIR INTAKE";
                            statusColor = surfaceData.useColors ? Color.Aqua : surfaceData.surface.ScriptForegroundColor;
                        }
                        int pad = innerWidth - statusWord.Length;
                        if (pad < 0) pad = 0;
                        int leftPad = pad / 2;
                        int rightPad = pad - leftPad;
                        statusWordOnly = statusWord;
                        // Shell with only spaces inside (constant width regardless of text)
                        statusBracket = "[ " + new string(' ', innerWidth) + " ]";
                        // Build a mask with the word centered inside, but replace brackets with spaces when overlaying so brackets stay white
                        string statusMask = "[ " + new string(' ', leftPad) + statusWord + new string(' ', rightPad) + " ]";
                        statusText = statusMask; // reuse variable to carry the mask through the overlay stage
                    }
                    catch { }
                    // Percent display (no brackets to match Damage Control style); hide in intake mode
                    string pct = isIntake ? "" : (level * 100f).ToString("0.0") + "%";

                    // Dynamically truncate the name so it won't collide with the right badges.
                    // Compute available character budget from total width minus right-side content.
                    float ppcName = MahDefinitions.pixelPerChar * surfaceData.textSize;
                    const int badgeInnerWidth = 30; // must match the bracket shell above
                    bool showPct = pct.Length > 0;
                    int rightChars = (showPct ? (pct.Length + 2) : 0) + (2 + badgeInnerWidth + 2); // optional pct+gap + bracket shell
                    float totalLinePx = surfaceData.surface.SurfaceSize.X - (2 * surfaceData.viewPortOffsetX);
                    int maxNameChars = (int)Math.Floor((totalLinePx - (rightChars * ppcName) - ppcName) / ppcName); // leave ~1 char gap
                    // Cap the visible name length at 40 characters max
                    if (maxNameChars > 40) maxNameChars = 40;
                    string leftName = name;
                    if (maxNameChars < 0) maxNameChars = 0;
                    if (leftName.Length > maxNameChars && maxNameChars > 0)
                        leftName = leftName.Substring(0, Math.Max(0, maxNameChars - 1)) + ".";
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, leftName, TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    // Draw the combined right-side string in white so brackets remain white and spacing is fixed
                    string rightWhite = showPct ? (pct + "  " + statusBracket) : statusBracket;
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, rightWhite, TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                    // Overlay the status word centered by pixel approximation at the center of the bracket shell
                    // Always draw the status word overlay; when colors are disabled, render it in the default foreground color instead of hiding it
                    {
                        float ppc = MahDefinitions.pixelPerChar * surfaceData.textSize;
                        // Bracket shell total characters: "[ " + innerWidth + " ]" => innerWidth + 4
                        const int innerWidth = 30;
                        int shellChars = innerWidth + 4;
                        float rightAnchorX = position.X + (surfaceData.surface.SurfaceSize.X - (2 * surfaceData.viewPortOffsetX));
                        float shellCenterX = rightAnchorX - (shellChars * ppc * 0.5f);
                        // Micro-adjust slightly to the LEFT for visual balance on proportional fonts
                        const float statusCenterBiasChars = -2.50f;
                        var centerPos = new Vector2(shellCenterX + (statusCenterBiasChars * ppc) - (surfaceData.surface.SurfaceSize.X * 0.5f), position.Y);
                        var finalColor = surfaceData.useColors ? statusColor : surfaceData.surface.ScriptForegroundColor;
                        SurfaceDrawer.WriteTextSprite(ref frame, centerPos, surfaceData, statusWordOnly, TextAlignment.CENTER, finalColor);
                    }
                    position += surfaceData.newLine;
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenLifeSupportSummary: Caught Exception while DrawAirVentsList: {e.ToString()}");
            }
        }
    }
}
