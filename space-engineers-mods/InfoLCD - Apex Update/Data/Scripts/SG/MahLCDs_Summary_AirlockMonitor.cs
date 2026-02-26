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
    [MyTextSurfaceScript("LCDInfoScreenAirlockMonitorSummary", "$IOS LCD - Airlock Monitor")]
    public class LCDAirlockMonitorSummary : MyTextSurfaceScriptBase
    {
        MyIni config = new MyIni();

        public static string CONFIG_SECTION_ID = "SettingsAirlockMonitorStatus";

        string searchId = "Airlock";
        bool compactMode = false;

        void TryCreateSurfaceData()
        {
            if (surfaceData != null)
                return;

            compactMode = mySurface.SurfaceSize.X / mySurface.SurfaceSize.Y > 4;
            var textSize = compactMode ? .8f : mySurface.SurfaceSize.Y > 300 ? .6f : .4f;

            // Initialize surface settings
            surfaceData = new SurfaceDrawer.SurfaceData
            {
                surface = mySurface,
                textSize = textSize,
                titleOffset = 200,
                ratioOffset = 82,
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
            sb.AppendLine("; [ AIRLOCKMONITOR - GENERAL OPTIONS ]");
            ConfigHelpers.AppendSearchIdConfig(sb, searchId);
            ConfigHelpers.AppendExcludeIdsConfig(sb, excludeIds);
            ConfigHelpers.AppendShowHeaderConfig(sb, surfaceData.showHeader);
            ConfigHelpers.AppendShowSubgridsConfig(sb, surfaceData.showSubgrids);
            ConfigHelpers.AppendSubgridUpdateFrequencyConfig(sb, surfaceData.subgridUpdateFrequency);
            ConfigHelpers.AppendUseColorsConfig(sb, surfaceData.useColors);

            sb.AppendLine();
            sb.AppendLine("; [ AIRLOCKMONITOR - LAYOUT OPTIONS ]");
            sb.AppendLine($"TextSize={surfaceData.textSize}");
            sb.AppendLine($"ViewPortOffsetX={surfaceData.viewPortOffsetX}");
            sb.AppendLine($"ViewPortOffsetY={surfaceData.viewPortOffsetY}");
            sb.AppendLine($"TitleFieldWidth={surfaceData.titleOffset}");
            sb.AppendLine($"RatioFieldWidth={surfaceData.ratioOffset}");
            sb.AppendLine();

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
                        searchId = string.IsNullOrWhiteSpace(searchId) ? "Airlock" : searchId;
                    }
                    else
                        configError = true;

                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowSubgrids", ref surfaceData.showSubgrids, ref configError);
                    if (config.ContainsKey(CONFIG_SECTION_ID, "SubgridUpdateFrequency")) surfaceData.subgridUpdateFrequency = config.Get(CONFIG_SECTION_ID, "SubgridUpdateFrequency").ToInt32();

                    if (config.ContainsKey(CONFIG_SECTION_ID, "TextSize"))
                        surfaceData.textSize = config.Get(CONFIG_SECTION_ID, "TextSize").ToSingle(defaultValue: 1.0f);
                    else
                        configError = true;

                    MahUtillities.TryGetConfigFloat(config, CONFIG_SECTION_ID, "TitleFieldWidth", ref surfaceData.titleOffset, ref configError);
                    MahUtillities.TryGetConfigFloat(config, CONFIG_SECTION_ID, "RatioFieldWidth", ref surfaceData.ratioOffset, ref configError);
                    MahUtillities.TryGetConfigFloat(config, CONFIG_SECTION_ID, "ViewPortOffsetX", ref surfaceData.viewPortOffsetX, ref configError);
                    MahUtillities.TryGetConfigFloat(config, CONFIG_SECTION_ID, "ViewPortOffsetY", ref surfaceData.viewPortOffsetY, ref configError);

                    surfaceData.newLine = new Vector2(0, 30 * surfaceData.textSize);

                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "UseColors", ref surfaceData.useColors, ref configError);

                    CreateExcludeIdsList();

                    // Is Corner LCD?
                    if (compactMode)
                    {
                        surfaceData.textSize = surfaceData.showHeader ? 0.8f : 1.2f;
                        surfaceData.titleOffset = 200;
                        surfaceData.ratioOffset = 82;
                        surfaceData.viewPortOffsetX = 10;
                        surfaceData.viewPortOffsetY = surfaceData.showHeader ? 10 : 15;
                        surfaceData.newLine = new Vector2(0, 30 * surfaceData.textSize);
                    }
                }
                else
                {
                    MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenAirlockMonitorSummary: Config Syntax error at Line {result}");
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenAirlockMonitorSummary: Caught Exception while loading config: {e.ToString()}");
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
        List<IMyDoor> doors = new List<IMyDoor>();
        List<IMyAirVent> airVents = new List<IMyAirVent>();

        // Cached subgrid collections
        List<IMyDoor> subgridDoors = new List<IMyDoor>();
        List<IMyAirVent> subgridAirVents = new List<IMyAirVent>();

        VRage.Collections.DictionaryValuesReader<MyDefinitionId, MyDefinitionBase> myDefinitions;
        MyDefinitionId myDefinitionId;
        SurfaceDrawer.SurfaceData surfaceData;
        string gridId = "Unknown grid";
        int subgridScanTick = 0;
        bool configError = false;
        bool isStation = false;
        Sandbox.ModAPI.Ingame.MyShipMass gridMass;

        public LCDAirlockMonitorSummary(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
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
                DrawAirlockMainSprite(ref myFrame, ref myPosition);

            myFrame.Dispose();
        }

        void UpdateBlocks ()
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
            var mainBlocks = MahUtillities.GetBlocks(myCubeGrid, searchId, excludeIds, ref gridMass, false, false);

            // Periodically update subgrid cache
            if (scanSubgrids)
            {
                var allBlocks = MahUtillities.GetBlocks(myCubeGrid, searchId, excludeIds, ref gridMass, surfaceData.showSubgrids, false);
                subgridDoors.Clear();
                subgridAirVents.Clear();
                foreach (var block in allBlocks)
                {
                    if (!mainBlocks.Contains(block))
                    {
                        if (block is IMyAirVent)
                            subgridAirVents.Add((IMyAirVent)block);
                        if (block is IMyDoor)
                            subgridDoors.Add((IMyDoor)block);
                    }
                }
            }

            // Categorize main grid blocks
            doors.Clear();
            airVents.Clear();
            foreach (var myBlock in mainBlocks)
            {
                if (myBlock == null) continue;

                if (myBlock is IMyAirVent)
                {
                    airVents.Add((IMyAirVent)myBlock);
                }
                if (myBlock is IMyDoor)
                {
                    doors.Add((IMyDoor)myBlock);
                }
            }

            // Merge main (fresh) and subgrid (cached) collections
            doors.AddRange(subgridDoors);
            airVents.AddRange(subgridAirVents);
        }

        void DrawAirlockMainSprite (ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            if (surfaceData.showHeader)
            {
                SurfaceDrawer.DrawHeader(ref frame, ref position, surfaceData, $"[{searchId} Monitor]");
                if (compactMode) position -= surfaceData.newLine;
            }

            DrawAirlockMonitorAirVentSprite(ref frame, ref position);
            DrawAirlockMonitorDoorSprite(ref frame, ref position);
        }

        void DrawAirlockMonitorAirVentSprite (ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            if (searchId == "*" || string.IsNullOrWhiteSpace(searchId))
            {
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $" No [SearchId] has been provided.", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                return;
            }

            // Remove all non existent or missing airVents.
            airVents.RemoveAll(block => block == null);

            if (airVents.Count <= 0)
            {
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $" No airVents with id [{searchId}] could be found.", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                return;
            }

            // We only need to check 1 single airlock to get the pressure info.
            SurfaceDrawer.DrawBar(ref frame, ref position, surfaceData, $"{(airVents[0].GetOxygenLevel() <= 0 ? "Depressurized" : airVents[0].Status.ToString())}", (airVents[0].GetOxygenLevel() * 100), 100, Unit.Percent, true);
            position += surfaceData.newLine;
        }

        void DrawAirlockMonitorDoorSprite (ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            if (compactMode) return;

            if (doors.Count <= 0) return;

            // Sort airlock doors alphabetically by custom name
            MahSorting.SortBlocksByName(doors);

            SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Airlock Doors [{doors.Count}]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
            SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"State", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
            position += surfaceData.newLine;

            foreach (var door in doors)
            {
                if (door == null) continue;

                var state = door.IsWorking ? "  On " : "  Off ";
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{state}", TextAlignment.LEFT, !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : state.Contains("Off") ? Color.Red : Color.GreenYellow);
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"[      ] {(door.CustomName.Length > 25 ? door.CustomName.Substring(0, 25) : door.CustomName)}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                var status = door.Status.ToString();
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{status}", TextAlignment.RIGHT, !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : status.Contains("Closed") ? Color.GreenYellow : Color.Orange);
                position += surfaceData.newLine;
            }
        }
    }
}