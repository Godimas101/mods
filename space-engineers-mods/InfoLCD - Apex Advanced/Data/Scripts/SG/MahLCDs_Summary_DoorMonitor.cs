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
    [MyTextSurfaceScript("LCDInfoScreenDoorMonitorSummary", "$IOS LCD - Door Monitor")]
    public class LCDDoorMonitorSummary : MyTextSurfaceScriptBase
    {
        MyIni config = new MyIni();

        public static string CONFIG_SECTION_ID = "SettingsDoorMonitorStatus";

        string searchId = "*";

        void TryCreateSurfaceData()
        {
            if (surfaceData != null)
                return;

            compactMode = mySurface.SurfaceSize.X / mySurface.SurfaceSize.Y > 4;
            var textSize = compactMode ? .6f : .5f;

            // Initialize surface settings
            surfaceData = new SurfaceDrawer.SurfaceData
            {
                surface = mySurface,
                textSize = textSize,
                titleOffset = 128,
                ratioOffset = 128,
                viewPortOffsetX = 20,
                viewPortOffsetY = 10,
                newLine = new Vector2(0, 30 * textSize),
                showHeader = true,
                showSummary = false,
                showMissing = false,
                showRatio = false,
                showBars = false,
                showSubgrids = false,
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
            sb.AppendLine("; [ DOORMONITOR - GENERAL OPTIONS ]");
            sb.AppendLine($"SearchId={searchId}");
            sb.AppendLine($"ExcludeIds={(excludeIds != null && excludeIds.Count > 0 ? String.Join(", ", excludeIds.ToArray()) : "")}");
            sb.AppendLine($"ShowSubgrids={surfaceData.showSubgrids}");
            sb.AppendLine($"SubgridUpdateFrequency={surfaceData.subgridUpdateFrequency}");
            sb.AppendLine("; Subgrid scan frequency: 1=fastest (60/sec), 10=normal (6/sec), 100=slowest (0.6/sec)");
            sb.AppendLine($"UseColors={surfaceData.useColors}");

            sb.AppendLine();
            sb.AppendLine("; [ DOORMONITOR - LAYOUT OPTIONS ]");
            sb.AppendLine($"TextSize={surfaceData.textSize}");
            sb.AppendLine($"ViewPortOffsetX={surfaceData.viewPortOffsetX}");
            sb.AppendLine($"ViewPortOffsetY={surfaceData.viewPortOffsetY}");
            sb.AppendLine($"TitleFieldWidth={surfaceData.titleOffset}");
            sb.AppendLine($"RatioFieldWidth={surfaceData.ratioOffset}");

            sb.AppendLine();
            sb.AppendLine("; [ DOORMONITOR - SCREEN OPTIONS ]");
            sb.AppendLine($"CompactMode={compactMode}");

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

                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowSubgrids", ref surfaceData.showSubgrids, ref configError);
                    if (config.ContainsKey(CONFIG_SECTION_ID, "SubgridUpdateFrequency")) surfaceData.subgridUpdateFrequency = config.Get(CONFIG_SECTION_ID, "SubgridUpdateFrequency").ToInt32();
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "CompactMode", ref compactMode, ref configError);
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "UseColors", ref surfaceData.useColors, ref configError);


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
                    MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenDoorMonitorSummary: Config Syntax error at Line {result}");
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenDoorMonitorSummary: Caught Exception while loading config: {e.ToString()}");
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
    
    // Cached subgrid doors (persisted between main grid scans)
    List<IMyDoor> subgridDoors = new List<IMyDoor>();

        VRage.Collections.DictionaryValuesReader<MyDefinitionId, MyDefinitionBase> myDefinitions;
        MyDefinitionId myDefinitionId;
        SurfaceDrawer.SurfaceData surfaceData;
        string gridId = "Unknown grid";
        int subgridScanTick = 0;
        bool configError = false;
        bool compactMode = false;
        bool isStation = false;
        Sandbox.ModAPI.Ingame.MyShipMass gridMass;

        public LCDDoorMonitorSummary(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
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
                if (compactMode)
                {
                    // Compact: first line is header; no extra top padding beyond base offset
                    SurfaceDrawer.DrawHeader(ref myFrame, ref myPosition, surfaceData, $"Door Monitor");
                    // Remove the second added blank line from DrawHeader (it advances two newLines); pull back one
                    myPosition -= surfaceData.newLine; 
                    // Totals
                    int total = doors.Count;
                    int open = doors.Count(d => d.Status != Sandbox.ModAPI.Ingame.DoorStatus.Closed);
                    SurfaceDrawer.WriteTextSprite(ref myFrame, myPosition, surfaceData, $"Doors: {total}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    myPosition += surfaceData.newLine;
                    SurfaceDrawer.WriteTextSprite(ref myFrame, myPosition, surfaceData, $"Open:  {open}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                }
                else
                {
                    SurfaceDrawer.DrawHeader(ref myFrame, ref myPosition, surfaceData, $"Door Monitor [{(searchId.Trim().Length <= 0 || searchId == "*" ? "All" : searchId)} -{excludeIds.Count}]");
                    myPosition += surfaceData.newLine;
                    DrawDoorMonitorSprite(ref myFrame, ref myPosition);
                }
            }

            myFrame.Dispose();
        }

        void UpdateBlocks ()
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
                var myCubeGrid = myTerminalBlock.CubeGrid as MyCubeGrid;

                if (myCubeGrid == null) return;

                IMyCubeGrid cubeGrid = myCubeGrid as IMyCubeGrid;
                isStation = cubeGrid.IsStatic;
                gridId = cubeGrid.CustomName;

                // Always get main grid blocks
                var mainBlocks = MahUtillities.GetBlocks(myCubeGrid, searchId, excludeIds, ref gridMass, false);

                doors.Clear();

                // Process main grid blocks
                foreach (var myBlock in mainBlocks)
                {
                    if (myBlock is IMyDoor)
                    {
                        var d = (IMyDoor)myBlock;
                        var name = d.CustomName ?? string.Empty;
                        if (name.IndexOf("airlock", StringComparison.OrdinalIgnoreCase) >= 0)
                            continue; // always skip airlock-designated doors
                        doors.Add(d);
                    }
                }

                // Periodically update subgrid cache
                if (scanSubgrids)
                {
                    var allBlocks = MahUtillities.GetBlocks(myCubeGrid, searchId, excludeIds, ref gridMass, true);
                    subgridDoors.Clear();
                    
                    // Extract subgrid-only blocks
                    foreach (var myBlock in allBlocks)
                    {
                        if (mainBlocks.Contains(myBlock)) continue;
                        if (myBlock is IMyDoor)
                        {
                            var d = (IMyDoor)myBlock;
                            var name = d.CustomName ?? string.Empty;
                            if (name.IndexOf("airlock", StringComparison.OrdinalIgnoreCase) >= 0)
                                continue; // always skip airlock-designated doors
                            subgridDoors.Add(d);
                        }
                    }
                }
                
                // Merge cached subgrid doors
                doors.AddRange(subgridDoors);
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenDoorMonitorSummary: Caught Exception while updating blocks: {e.ToString()}");
            }
        }

        void DrawDoorMonitorSprite (ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            try
            {
                if (doors.Count <= 0) return; // full mode only

                // Sort doors alphabetically by custom name
                MahSorting.SortBlocksByName(doors);

                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Doors [{doors.Count}]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"State", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine;

                foreach (var door in doors)
                {
                    var state = door.IsWorking ? "  On " : "  Off ";
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{state}", TextAlignment.LEFT, !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : state.Contains("Off") ? Color.Red : Color.GreenYellow);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"[      ] {(door.CustomName.Length > 30 ? door.CustomName.Substring(0, 30) : door.CustomName)}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    var status = door.Status.ToString();
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{status}", TextAlignment.RIGHT, !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : status.Contains("Closed") ? Color.GreenYellow : Color.Orange);
                    position += surfaceData.newLine;
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenDoorMonitorSummary: Caught Exception while DrawDoorMonitorSprite: {e.ToString()}");
            }
        }
    }
}