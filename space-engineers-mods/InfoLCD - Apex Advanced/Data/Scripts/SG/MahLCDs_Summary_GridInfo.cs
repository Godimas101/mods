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
    [MyTextSurfaceScript("LCDInfoScreenGridInfoSummary", "$IOS LCD - Grid & Flight Info")]
    public class LCDGridInfoSummary : MyTextSurfaceScriptBase
    {
        MyIni config = new MyIni();

        public static string CONFIG_SECTION_ID = "SettingsGridInfoStatus";

        string searchId = "*";

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
                ratioOffset = compactMode ? 300 : 104,
                viewPortOffsetX = 10,
                viewPortOffsetY = compactMode ? 5 : 10,
                newLine = new Vector2(0, 30 * textSize),
                showHeader = true,
                showSummary = true,
                showMissing = false,
                showRatio = false,
                showBars = true,
                showSubgrids = false,
                showDocked = true,
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
            sb.AppendLine("; [ GRIDINFO - GENERAL OPTIONS ]");
            sb.AppendLine($"SearchId={searchId}");
            sb.AppendLine($"ExcludeIds={(excludeIds != null && excludeIds.Count > 0 ? String.Join(", ", excludeIds.ToArray()) : "")}");
            sb.AppendLine($"ShowHeader={surfaceData.showHeader}");
            sb.AppendLine($"ShowSummary={surfaceData.showSummary}");
            sb.AppendLine($"ShowSubgrids={surfaceData.showSubgrids}");
            sb.AppendLine($"SubgridUpdateFrequency={surfaceData.subgridUpdateFrequency}");
            sb.AppendLine("; Subgrid scan frequency: 1=fastest (60/sec), 10=normal (6/sec), 100=slowest (0.6/sec)");
            sb.AppendLine($"ShowDocked={surfaceData.showDocked}");
            sb.AppendLine($"UseColors={surfaceData.useColors}");

            sb.AppendLine();
            sb.AppendLine("; [ GRIDINFO - LAYOUT OPTIONS ]");
            sb.AppendLine($"TextSize={surfaceData.textSize}");
            sb.AppendLine($"ViewPortOffsetX={surfaceData.viewPortOffsetX}");
            sb.AppendLine($"ViewPortOffsetY={surfaceData.viewPortOffsetY}");
            sb.AppendLine($"TitleFieldWidth={surfaceData.titleOffset}");
            sb.AppendLine($"RatioFieldWidth={surfaceData.ratioOffset}");
            sb.AppendLine($"ShowRatio={surfaceData.showRatio}");
            sb.AppendLine($"ShowBars={surfaceData.showBars}");

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
                    MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenGridInfoSummary: Config Syntax error at Line {result}");
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenGridInfoSummary: Caught Exception while loading config: {e.ToString()}");
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
        List<IMyJumpDrive> jumpdrives = new List<IMyJumpDrive>();
        List<IMyShipConnector> connectors = new List<IMyShipConnector>();
        
        // Cached subgrid collections (persisted between main grid scans)
        List<IMyJumpDrive> subgridJumpdrives = new List<IMyJumpDrive>();
        List<IMyShipConnector> subgridConnectors = new List<IMyShipConnector>();

        VRage.Collections.DictionaryValuesReader<MyDefinitionId, MyDefinitionBase> myDefinitions;
        MyDefinitionId myDefinitionId;
        SurfaceDrawer.SurfaceData surfaceData;
        float timeRemaining = 0.0f;
        
        float reactorsCurrentVolume = 0.0f;
        float reactorsMaximumVolume = 0.0f;
        float reactorsCurrentLoad = 0.0f;
        float reactorsMaximumLoad = 0.0f;
        string gridId = "Unknown grid";
        int subgridScanTick = 0;
        bool configError = false;
        bool compactMode = false;
        bool isStation = false;
        Sandbox.ModAPI.Ingame.MyShipMass gridMass;
        
        public LCDGridInfoSummary(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
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
                DrawGridInfoSummaryMainSprite(ref myFrame, ref myPosition);
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
                jumpdrives.Clear();
                connectors.Clear();

                var myCubeGrid = myTerminalBlock.CubeGrid as MyCubeGrid;

                if (myCubeGrid == null) return;

                IMyCubeGrid cubeGrid = myCubeGrid as IMyCubeGrid;
                isStation = cubeGrid.IsStatic;
                gridId = cubeGrid.CustomName;

                // Always get main grid blocks
                var mainBlocks = MahUtillities.GetBlocks(myCubeGrid, searchId, excludeIds, ref gridMass, false, false);

                // Process main grid blocks
                foreach (var myBlock in mainBlocks)
                {
                    if (myBlock == null) continue;

                    if (myBlock is IMyJumpDrive)
                    {
                        jumpdrives.Add((IMyJumpDrive)myBlock);
                    }
                    else if (myBlock is IMyShipConnector)
                    {
                        connectors.Add(myBlock as IMyShipConnector);
                    }
                }

                // Periodically update subgrid cache
                if (scanSubgrids)
                {
                    var allBlocks = MahUtillities.GetBlocks(myCubeGrid, searchId, excludeIds, ref gridMass, true, false);
                    
                    subgridJumpdrives.Clear();
                    subgridConnectors.Clear();
                    
                    // Extract subgrid-only blocks
                    foreach (var myBlock in allBlocks)
                    {
                        if (mainBlocks.Contains(myBlock)) continue;
                        if (myBlock == null) continue;

                        if (myBlock is IMyJumpDrive)
                        {
                            subgridJumpdrives.Add((IMyJumpDrive)myBlock);
                        }
                        else if (myBlock is IMyShipConnector)
                        {
                            subgridConnectors.Add(myBlock as IMyShipConnector);
                        }
                    }
                }
                
                // Merge cached subgrid blocks
                jumpdrives.AddRange(subgridJumpdrives);
                connectors.AddRange(subgridConnectors);
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenGridInfoSummary: Caught Exception while updating blocks: {e.ToString()}");
            }
        }

        void DrawGridInfoSummaryMainSprite(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            if (surfaceData.showHeader)
            {
                SurfaceDrawer.DrawHeader(ref frame, ref position, surfaceData, $"{(isStation ? "Grid" : "Flight")} Info '{gridId}' {(isStation ? "<Station>" : "<Vessel>")}");
                if (compactMode) position -= surfaceData.newLine;
            }

            if (surfaceData.showSummary)
            {
                // Stationary objects have, for whatever reason, no mass calculation.
                if (!isStation)
                {
                    SurfaceDrawer.DrawShipMassSprite(ref frame, ref position, surfaceData, gridMass, compactMode);
                    position += surfaceData.newLine;
                }

                if (surfaceData.showDocked)
                {
                    if (!compactMode)
                    {
                        foreach (IMyShipConnector connector in connectors)
                        {
                            if (connector == null) continue;
                            Sandbox.ModAPI.Ingame.MyShipConnectorStatus status = connector.Status;

                            string state = $"{(status == Sandbox.ModAPI.Ingame.MyShipConnectorStatus.Connectable ? "Ready    " : status == Sandbox.ModAPI.Ingame.MyShipConnectorStatus.Connected ? "Locked    " : "Unlocked  ")}";
                            string connectedGridId = $"{(connector.IsConnected ? $"<{(connector.OtherConnector.CubeGrid as IMyCubeGrid).CustomName}>" : "")}";
                            Color stateColor = !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : status == Sandbox.ModAPI.Ingame.MyShipConnectorStatus.Connectable ? Color.Orange : status == Sandbox.ModAPI.Ingame.MyShipConnectorStatus.Connected ? Color.GreenYellow : Color.Yellow;

                            // Only show all connectors on stations. On Vessels only show actually connected.
                            if (isStation || status != Sandbox.ModAPI.Ingame.MyShipConnectorStatus.Unconnected)
                            {
                                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{(" " + connector.CustomName + ":")}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{connectedGridId}   ", TextAlignment.CENTER, !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : Color.Yellow);
                                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"[                 ]", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $" {state}", TextAlignment.RIGHT, stateColor);
                                position += surfaceData.newLine;
                            }
                        }
                        position += surfaceData.newLine;
                    }
                    else
                    {
                        if (!isStation) position -= surfaceData.newLine;
                        var dockedVesselCount = 0;

                        foreach (IMyShipConnector connector in connectors)
                        {
                            if (connector == null) continue;
                            if (connector.IsConnected) dockedVesselCount++;
                        }

                        SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Docked Ships: {dockedVesselCount}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                        position += surfaceData.newLine;
                    }
                }
            }

            SurfaceDrawer.DrawJumpDriveSprite(ref frame, ref position, surfaceData, jumpdrives, isStation, compactMode);
        }
    }
}
