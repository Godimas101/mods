using MahrianeIndustries.LCDInfo;
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
using static MahrianeIndustries.LCDInfo.SurfaceDrawer;

namespace MahrianeIndustries.LCDInfo
{
    public static class SurfaceDrawer
    {
        public class SurfaceData
        {
            public IMyTextSurface surface;
            public float textSize;
            public float titleOffset;
            public float ratioOffset;
            public float viewPortOffsetX;
            public float viewPortOffsetY;
            public Vector2 newLine;
            public bool showHeader;
            public bool showSummary;
            public bool showMissing;
            public bool showRatio;
            public bool showBars;
            public bool showSubgrids;
            public int subgridUpdateFrequency = 100;
            public bool showDocked;
            public bool useColors;
            public bool summaryOnly = false;
            public bool showAmmo = true;
            public bool showHandAmmo = true;
            public bool showComponents = true;
            public bool showProtoComponents = true; 
            public bool showStructuralDetails = false;
            public bool showStructural = false;
            public bool showMovement = false;
            public bool showCommunications = false;
            public bool showTanks = false;
            public bool showPower = false;
            public bool showProduction = false;
            public bool showContainers = false;
            public bool showDoors = false;
            public bool showControllers = false;
            public bool showGyros = false;
            public bool showMechanical = false;
            public bool showJumpdrives = false;
            public bool showWeapons = false;
            public bool showTools = false;
            public bool showAutomation = false;
            public bool showMedical = false;
            public bool showHealthyCategories = false;
            public bool showVent = true;
            public bool showIntakeVent = true;
            public bool showTool = true;
            public bool showRifle = true;
            public bool showPistol = true;
            public bool showLauncher = true;
            public bool showKit = true;
            public bool showMisc = true;
            public bool showBottle = true;
            public bool showRawFood = true;
            public bool showCookedFood = true;
            public bool showDrink = true;
            public bool showSeed = true;
            public bool showProtoIngots = true;
            public bool showScrap = false;
            public bool useSubtypeId = false;
        }

        // Write text with a specific font id (default WriteTextSprite uses White)
        public static void WriteTextSpriteWithFont(ref MySpriteDrawFrame frame, Vector2 position, SurfaceData surfaceData, string text, TextAlignment alignment, Color color, string fontId)
        {
            Color fontColor = color;

            if (alignment == TextAlignment.RIGHT)
                position += new Vector2(surfaceData.surface.SurfaceSize.X - (2 * surfaceData.viewPortOffsetX), 0);
            else if (alignment == TextAlignment.CENTER)
                position += new Vector2(surfaceData.surface.SurfaceSize.X * .5f, 0);

            var sprite = new MySprite
            {
                Type = SpriteType.TEXT,
                Data = text,
                Position = position,
                RotationOrScale = surfaceData.textSize,
                Color = fontColor,
                Alignment = alignment,
                FontId = fontId
            };

            frame.Add(sprite);
        }

        public static void DrawHeader(ref MySpriteDrawFrame frame, ref Vector2 position, SurfaceData surfaceData, string title, string rightText = "")
        {
            try
            {
                // Draw left side main title.
                WriteTextSprite(ref frame, position, surfaceData, $"{title}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);

                // Draw right side.
                WriteTextSprite(ref frame, position, surfaceData, $"[{(rightText == "" ? MahDefinitions.CurrentTimeStamp : rightText)}]", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);

                // Debugging SurfaceSize
                // WriteTextSprite(ref frame, position, surfaceData, $"[{surfaceData.surface.SurfaceSize.X.ToString("0.0")}x{surfaceData.surface.SurfaceSize.Y.ToString("0.0")}]", TextAlignment.CENTER, surfaceData.surface.ScriptForegroundColor);

                position += surfaceData.newLine + surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.SurfaceDrawer: Caught Exception while drawing header sprite: {e.ToString()}");
            }

        }

        public static void DrawPowerTimeHeaderSprite(ref MySpriteDrawFrame frame, ref Vector2 position, SurfaceData surfaceData, string title, List<IMyPowerProducer> powerProducers)
        {
            try
            {
                // Reset value
                var timeRemaining = MahUtillities.GetPowerTimeLeft(powerProducers);

                DrawHeader(ref frame, ref position, surfaceData, $"{title}", $"Power Left: {(timeRemaining > 0 ? MahDefinitions.TimeFormat((int)timeRemaining) : "-")}");
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.SurfaceDrawer: Caught Exception while drawing PowerTimeHeaderSprite: {e.ToString()}");
            }
        }

        public static void DrawErrorSprite (ref MySpriteDrawFrame frame, SurfaceData surfaceData, string text, Color fontColor)
        {
            try
            {
                var myViewport = new RectangleF((surfaceData.surface.TextureSize - surfaceData.surface.SurfaceSize) / 2f, surfaceData.surface.SurfaceSize);

                // Create background sprite
                var sprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = text,
                    Position = new Vector2(0, surfaceData.surface.SurfaceSize.Y * .5f),
                    Size = myViewport.Size,
                    Color = Color.Yellow,
                    Alignment = TextAlignment.CENTER
                };
                // Add the sprite to the frame
                frame.Add(sprite);

                WriteTextSprite(ref frame, new Vector2(0, surfaceData.surface.SurfaceSize.Y * .5f + (30 * surfaceData.textSize)), surfaceData, $"{text}", TextAlignment.CENTER, fontColor);
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.SurfaceDrawer: Caught Exception while drawing FootNoteSprite: {e.ToString()}");
            }
        }

        public static void DrawItemSprite(ref MySpriteDrawFrame frame, ref Vector2 position, SurfaceData surfaceData, string subtypeId, string displayName, int currentAmount, int minAmount, bool invertColors = false, bool ignoreColors = false)
        {
            try
            {
                if (minAmount <= 0 || !surfaceData.showBars)
                {
                    displayName = MahUtillities.GetSubstring(displayName, surfaceData, true);
                    WriteTextSprite(ref frame, position, surfaceData, displayName, TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    WriteTextSprite(ref frame, position, surfaceData, MahDefinitions.KiloFormat(currentAmount), TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                    position += surfaceData.newLine;
                }
                else
                {
                    DrawBar(ref frame, ref position, surfaceData, displayName, currentAmount, minAmount, Unit.Count, invertColors, ignoreColors);
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.SurfaceDrawer: Caught Exception while drawing item sprite: {e.ToString()}");
            }
        }

        public static void DrawOutputSprite(ref MySpriteDrawFrame frame, ref Vector2 position, SurfaceData surfaceData, string title, double current, double total, bool showInactive, Unit unit, bool invertColors = false)
        {
            if (current <= 0 && !showInactive) return;

            try
            {
                // Avoid division by 0
                total = total <= 0 ? 1 : total;

                if (surfaceData.showBars)
                {
                    DrawBar(ref frame, ref position, surfaceData, title, current, total, unit, invertColors);
                }
                else
                {
                    title = MahUtillities.GetSubstring(title, surfaceData);
                    WriteTextSprite(ref frame, position, surfaceData, title, TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    if (surfaceData.showRatio)
                        WriteTextSprite(ref frame, position, surfaceData, $"{((current / total) * 100).ToString("0.00")} %", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                    else
                        WriteTextSprite(ref frame, position, surfaceData, $"{MahDefinitions.WattFormat(current)}{(unit == Unit.WattHours ? "h" : "")} / {MahDefinitions.WattFormat(total)}{(unit == Unit.WattHours ? "h" : "")}", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                    position += surfaceData.newLine;
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.SurfaceDrawer: Caught Exception while drawing output sprite: {e.ToString()}");
            }
        }

        public static void DrawGasTankSprite(ref MySpriteDrawFrame frame, ref Vector2 position, SurfaceData surfaceData, string gasSubtypeId, string title, List<IMyGasTank> allGasTankList, bool invertColors = true)
        {
            if (allGasTankList.Count <= 0) return;

            try
            {

                if (allGasTankList.Count > 0)
                {
                    var tanks = allGasTankList.Where(block => gasSubtypeId == "Hydrogen" ? block.BlockDefinition.SubtypeName.Contains(gasSubtypeId) : !block.BlockDefinition.SubtypeName.Contains("Hydrogen"));
                    var total = tanks.Count() == 0 ? 0 : tanks.Sum(block => block.Capacity);
                    var current = tanks.Count() == 0 ? 0 : tanks.Average(block => block.FilledRatio) * total;
                    var currentPercent = tanks.Count() == 0 ? 0 : tanks.Average(block => block.FilledRatio * 100);

                    if (surfaceData.showBars)
                    {
                        DrawBarFixedColor(ref frame, ref position, surfaceData, $"{title}", (currentPercent / 100) * total, total, gasSubtypeId == "Hydrogen" ? Color.LightSalmon : Color.MediumAquamarine, surfaceData.showRatio ? Unit.Percent : Unit.Liters);
//                        DrawBar(ref frame, ref position, surfaceData, $"{title}", (currentPercent / 100) * total, total, surfaceData.showRatio ? Unit.Percent : Unit.Liters, invertColors);
                    }
                    else
                    {
                        title = MahUtillities.GetSubstring(title, surfaceData);
                        WriteTextSprite(ref frame, position, surfaceData, $"{title}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                        if (surfaceData.showRatio)
                            WriteTextSprite(ref frame, position, surfaceData, $"{currentPercent.ToString("0.00")} %", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                        else
                            WriteTextSprite(ref frame, position, surfaceData, $"{MahDefinitions.LiterFormat(current)}", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                        position += surfaceData.newLine;
                    }
                }
                else
                {
                    WriteTextSprite(ref frame, position, surfaceData, $"- No {gasSubtypeId} tanks detected.", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    position += surfaceData.newLine;
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.SurfaceDrawer: Caught Exception while drawing gas tanks sprite: {e.ToString()}");
            }
        }

        public static void DrawAssemblerSummarySprite(ref MySpriteDrawFrame frame, ref Vector2 position, SurfaceData surfaceData, List<IMyAssembler> assemblers)
        {
            if (assemblers.Count <= 0) return;
            
            try
            {
                WriteTextSprite(ref frame, position, surfaceData, $"Assemblers [{assemblers.Count}]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                WriteTextSprite(ref frame, position, surfaceData, $"Active Task      ", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine;

                foreach (var assembler in assemblers)
                {
                    if (assembler == null) continue;

                    var name = assembler.CustomName;
                    var inventory = assembler.GetInventory(0);
                    var currentVolume = inventory.CurrentVolume;
                    List<Sandbox.ModAPI.Ingame.MyProductionItem> queuedBlueprints = new List<Sandbox.ModAPI.Ingame.MyProductionItem>();
                    assembler.GetQueue(queuedBlueprints);
                    var blueprintId = queuedBlueprints.Count > 0 ? queuedBlueprints[0].BlueprintId.ToString().Split('/')[1] : "";
                    var blueprintAmount = queuedBlueprints.Count > 0 ? (int)queuedBlueprints[0].Amount : 0;

                    // If this is the Survival Kit assembling, it might be producing Ingots from Stone.
                    if (blueprintId.Contains("StoneOre"))
                    {
                        blueprintId = "Basic Ingots";
                    }
                    // If this is a standard assembler
                    else if (blueprintId != "")
                    {
                        // Try to find a definition for the item in production.
                        CargoItemDefinition itemDefinition = MahDefinitions.GetDefinition("Component", blueprintId);
                        if (itemDefinition == null)
                            itemDefinition = MahDefinitions.GetDefinition("PhysicalGunObject", blueprintId);
                        if (itemDefinition == null)
                            itemDefinition = MahDefinitions.GetDefinition("AmmoMagazine", blueprintId);
                        if (itemDefinition == null)
                            itemDefinition = MahDefinitions.GetDefinition("OxygenContainerObject", blueprintId);
                        if (itemDefinition == null)
                            itemDefinition = MahDefinitions.GetDefinition("GasContainerObject", blueprintId);
                        if (itemDefinition == null)
                            itemDefinition = MahDefinitions.GetDefinition("PhysicalObject", blueprintId);
                        if (itemDefinition == null)
                            itemDefinition = MahDefinitions.GetDefinition("ConsumableItem", blueprintId);
                        if (itemDefinition == null)
                            itemDefinition = MahDefinitions.GetDefinition("Package", blueprintId);
                        // If we found a definition, use its displayname, otherwise keep the blueprintId as name.
                        if (itemDefinition != null)
                        {
                            blueprintId = itemDefinition.displayName;
                        }
                    }

                    var queue = blueprintId == "" ? "-" : $"{blueprintId}";
                    var outputBlocked = assembler.OutputInventory.CurrentVolume > assembler.OutputInventory.MaxVolume * .9f;
                    var state = $"{(!assembler.IsWorking ? "    Off" : outputBlocked ? "   Full" : queuedBlueprints.Count > 0 && assembler.IsProducing ? "  Work" : "   Halt")}";

                    WriteTextSprite(ref frame, position, surfaceData, $"{state} ", TextAlignment.LEFT, !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : state.Contains("Off") ? Color.Orange : state.Contains("Halt") ? Color.Yellow : state.Contains("Full") ? Color.Red : Color.GreenYellow);
                    WriteTextSprite(ref frame, position, surfaceData, $"[          ] {name}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    WriteTextSprite(ref frame, position, surfaceData, $"{(blueprintAmount > 0 ? blueprintAmount.ToString("0") : "")} {queue}  +{(queuedBlueprints.Count > 0 ? queuedBlueprints.Count - 1 : 0).ToString("0").Replace("1", " 1")}", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);

                    position += surfaceData.newLine;
                }

                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.SurfaceDrawer: Caught Exception while drawing assembler sprite: {e.ToString()}");
            }
        }

        public static void DrawRefinerySummarySprite(ref MySpriteDrawFrame frame, ref Vector2 position, SurfaceData surfaceData, List<IMyRefinery> refineries)
        {
            if (refineries.Count <= 0) return;

            try
            {
                WriteTextSprite(ref frame, position, surfaceData, $"Refineries [{refineries.Count}]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                WriteTextSprite(ref frame, position, surfaceData, $"Active Task      ", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine;

                foreach (var refinery in refineries)
                {
                    if (refinery == null) continue;

                    var name = refinery.CustomName;
                    var inventory = refinery.GetInventory(0);
                    var currentVolume = inventory.CurrentVolume;

                    List<VRage.Game.ModAPI.Ingame.MyInventoryItem> queuedItems = new List<VRage.Game.ModAPI.Ingame.MyInventoryItem>();
                    inventory.GetItems(queuedItems);

                    var subtypeId = queuedItems.Count > 0 ? queuedItems[0].Type.SubtypeId : "";
                    var amount = queuedItems.Count > 0 ? queuedItems[0].Amount.ToIntSafe() : 0;
                    var queue = subtypeId == "" ? "-" : $"{MahDefinitions.KiloFormat(amount)} {subtypeId}";
                    var outputBlocked = refinery.OutputInventory.CurrentVolume >= refinery.OutputInventory.MaxVolume * .9f;
                    var state = $"{(!refinery.IsWorking ? "    Off" : outputBlocked ? "   Full" : queuedItems.Count > 0 ? "  Work" : "   Halt")}";

                    WriteTextSprite(ref frame, position, surfaceData, $"{state}", TextAlignment.LEFT, !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : state.Contains("Off") ? Color.Orange : state.Contains("Halt") ? Color.Yellow : state.Contains("Full") ? Color.Red : Color.GreenYellow);
                    WriteTextSprite(ref frame, position, surfaceData, $"[          ] {name}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    WriteTextSprite(ref frame, position, surfaceData, $"{queue}  +{(queuedItems.Count > 0 ? queuedItems.Count - 1 : 0).ToString("0").Replace("1", " 1")}", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);

                    position += surfaceData.newLine;
                }

                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.SurfaceDrawer: Caught Exception while drawing refinery sprite: {e.ToString()}");
            }
        }

        public static void DrawGasGeneratorSummarySprite(ref MySpriteDrawFrame frame, ref Vector2 position, SurfaceData surfaceData, List<IMyGasGenerator> generators)
        {
            if (generators.Count <= 0) return;

            try
            {
                WriteTextSprite(ref frame, position, surfaceData, $"H2/O2 Generators [{generators.Count}]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                WriteTextSprite(ref frame, position, surfaceData, $"Inventory      ", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine;

                CargoItemDefinition iceDefinition = MahDefinitions.GetDefinition("Ore", "Ice");
                List<VRage.Game.ModAPI.Ingame.MyInventoryItem> inventoryItems = new List<VRage.Game.ModAPI.Ingame.MyInventoryItem>();

                foreach (var generator in generators)
                {
                    if (generator == null) continue;

                    var name = generator.CustomName;
                    var inventory = generator.GetInventory(0);
                    float currentVolume = 0.0f;
                    
                    if (iceDefinition != null)
                    {
                        int iceCount = 0;
                        inventoryItems.Clear();
                        inventory.GetItems(inventoryItems);
                        foreach (var item in inventoryItems.OrderBy(i => i.Type.SubtypeId))
                        {
                            if (item == null) continue;

                            var subtypeId = item.Type.SubtypeId;

                            if (subtypeId.Contains("Ice"))
                            {
                                iceCount += item.Amount.ToIntSafe();
                            }
                        }
                        currentVolume = iceCount * iceDefinition.volume;
                    }
                    else
                        currentVolume = (float)inventory.CurrentVolume;

                    float maximumVolume = (float)inventory.MaxVolume * 1000f;
                    var state = $"{(!generator.IsWorking ? "    Off" : currentVolume <= 0 ? "   Halt" : "  Work")}";

                    WriteTextSprite(ref frame, position, surfaceData, $"{state}", TextAlignment.LEFT, !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : state.Contains("Off") ? Color.Orange : state.Contains("Halt") ? Color.Yellow : Color.GreenYellow);
                    WriteTextSprite(ref frame, position, surfaceData, $"[          ] {name}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    DrawHalfBar(ref frame, position, surfaceData, TextAlignment.RIGHT, currentVolume, maximumVolume, Unit.Percent, Color.Aquamarine);
                    position += surfaceData.newLine;
                }

                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.SurfaceDrawer: Caught Exception while drawing gas generator sprite: {e.ToString()}");
            }
        }

        public static void DrawOxygenFarmSummarySprite(ref MySpriteDrawFrame frame, ref Vector2 position, SurfaceData surfaceData, List<IMyOxygenFarm> oxygenFarms)
        {
            if (oxygenFarms.Count <= 0) return;

            try
            {
                WriteTextSprite(ref frame, position, surfaceData, $"Oxygen Farms [{oxygenFarms.Count}]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                WriteTextSprite(ref frame, position, surfaceData, $"State        ", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine;

                foreach (var oxygenFarm in oxygenFarms)
                {
                    if (oxygenFarm == null) continue;

                    var name = oxygenFarm.CustomName;
                    var currentOutput = oxygenFarm.DetailedInfo.Split('\n')[2].Replace("Oxygen Output:", "").Trim();
                    var state = $"{(!oxygenFarm.IsWorking ? "    Off" : !oxygenFarm.CanProduce ? "  Idle" : "    On")}";

                    WriteTextSprite(ref frame, position, surfaceData, $"{state}", TextAlignment.LEFT, !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : state.Contains("Off") ? Color.Orange : state.Contains("Idle") ? Color.Yellow : Color.GreenYellow);
                    WriteTextSprite(ref frame, position, surfaceData, $"[          ] {name}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    WriteTextSprite(ref frame, position, surfaceData, $"{currentOutput}    ", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                    position += surfaceData.newLine;
                }

                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.SurfaceDrawer: Caught Exception while drawing oxygen farms sprite: {e.ToString()}");
            }
        }

        public static void DrawShipMassSprite(ref MySpriteDrawFrame frame, ref Vector2 position, SurfaceData surfaceData, Sandbox.ModAPI.Ingame.MyShipMass gridMass, bool compactMode)
        {
            if (!compactMode)
            {
                // Stationary cubeGrids have no mass calculation.
                WriteTextSprite(ref frame, position, surfaceData, $"Dry Mass:", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                WriteTextSprite(ref frame, position, surfaceData, $"{gridMass.BaseMass.ToString("#,0 K")}g", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine;
                WriteTextSprite(ref frame, position, surfaceData, $"Total Mass:", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                WriteTextSprite(ref frame, position, surfaceData, $"{gridMass.TotalMass.ToString("#,0 K")}g", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine;
            }
            else
            {
                WriteTextSprite(ref frame, position, surfaceData, $"Total Mass: {gridMass.TotalMass.ToString("#,0 K")}g", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
            }
        }

        public static void DrawIntegritySummarySprite(ref MySpriteDrawFrame frame, ref Vector2 position, SurfaceData surfaceData, List<BlockStateData> blocks, bool allowSummary = true, bool hideOverview = false)
        {
            try
            {
                var total = blocks.Sum(block => block.MaxIntegrity);
                var current = blocks.Sum(block => block.CurrentIntegrity);

                if (!hideOverview)
                {
                    WriteTextSprite(ref frame, position, surfaceData, $"Systems Integrity", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    position += surfaceData.newLine;
                    //SurfaceDrawer.DrawBar(ref frame, ref position, surfaceData, "SYS", blocks.Count - damagedBlocksCounter, blocks.Count, Unit.Percent, true, false);
                    DrawBar(ref frame, ref position, surfaceData, "SYS", current, total, Unit.Percent, true, false);
                }

                // If summary should not be displayed, or (no damaged detected and not explicitly showing healthy categories), stop.
                if (!allowSummary || !surfaceData.showSummary || (current == total && !surfaceData.showHealthyCategories))
                {
                    if (!hideOverview)
                        position += surfaceData.newLine;
                    return;
                }

                // Categories aligned with Damage Monitor toggles
                List<BlockStateData> movement = new List<BlockStateData>();
                List<BlockStateData> communications = new List<BlockStateData>();
                List<BlockStateData> tanks = new List<BlockStateData>();
                List<BlockStateData> power = new List<BlockStateData>();
                List<BlockStateData> production = new List<BlockStateData>();
                List<BlockStateData> containers = new List<BlockStateData>();
                List<BlockStateData> doors = new List<BlockStateData>();
                List<BlockStateData> controllers = new List<BlockStateData>();
                List<BlockStateData> gyros = new List<BlockStateData>();
                List<BlockStateData> mechanical = new List<BlockStateData>();
                List<BlockStateData> jumpdrives = new List<BlockStateData>();
                List<BlockStateData> weapons = new List<BlockStateData>();
                List<BlockStateData> tools = new List<BlockStateData>();
                List<BlockStateData> automation = new List<BlockStateData>();
                List<BlockStateData> medical = new List<BlockStateData>();
                List<BlockStateData> misc = new List<BlockStateData>();

                foreach (BlockStateData blockData in blocks)
                {
                    if (blockData.IsNull) continue;

                    IMyTerminalBlock terminalBlock = blockData.block;

                    if (terminalBlock is IMyUserControllableGun)
                        weapons.Add(blockData);
                    else if (terminalBlock is IMyShipToolBase || terminalBlock is IMyShipDrill || terminalBlock is IMyShipWelder || terminalBlock is IMyShipGrinder)
                        tools.Add(blockData);
                    else if (terminalBlock is IMyPistonBase || terminalBlock is IMyMotorAdvancedStator || terminalBlock is IMyMotorStator || terminalBlock is IMyLandingGear || terminalBlock is IMyShipConnector || terminalBlock is IMyAdvancedDoor)
                        mechanical.Add(blockData);
                    else if (terminalBlock is IMyShipController || terminalBlock is IMyCockpit)
                        controllers.Add(blockData);
                    else if (terminalBlock is IMyBatteryBlock || terminalBlock is IMyReactor || terminalBlock is IMySolarPanel || terminalBlock.BlockDefinition.SubtypeName.IndexOf("Wind", StringComparison.OrdinalIgnoreCase) >= 0 || terminalBlock.BlockDefinition.SubtypeName.IndexOf("HydrogenEngine", StringComparison.OrdinalIgnoreCase) >= 0)
                        power.Add(blockData);
                    else if (terminalBlock is IMyAssembler || terminalBlock is IMyRefinery || terminalBlock is IMyGasGenerator || terminalBlock is IMyOxygenFarm)
                        production.Add(blockData);
                    else if (terminalBlock is IMyCargoContainer)
                        containers.Add(blockData);
                    else if (terminalBlock is IMyDoor)
                        doors.Add(blockData);
                    else if (terminalBlock is IMyThrust || terminalBlock is IMyWheel)
                        movement.Add(blockData);
                    else if (terminalBlock is IMyRadioAntenna || terminalBlock is IMyLaserAntenna || terminalBlock is IMyBeacon || terminalBlock is IMySensorBlock || terminalBlock is IMyOreDetector || terminalBlock is IMyCameraBlock || terminalBlock.BlockDefinition.SubtypeName.IndexOf("Beacon", StringComparison.OrdinalIgnoreCase) >= 0)
                        communications.Add(blockData);
                    else if (terminalBlock is IMyGyro)
                        gyros.Add(blockData);
                    else if (terminalBlock is IMyJumpDrive)
                        jumpdrives.Add(blockData);
                    else if (terminalBlock is IMyGasTank)
                        tanks.Add(blockData);
                    else if (terminalBlock is IMyCryoChamber || terminalBlock is IMyMedicalRoom || terminalBlock.BlockDefinition.SubtypeName.IndexOf("Cryo", StringComparison.OrdinalIgnoreCase) >= 0 || terminalBlock.BlockDefinition.SubtypeName.IndexOf("Medical", StringComparison.OrdinalIgnoreCase) >= 0 || terminalBlock.BlockDefinition.SubtypeName.IndexOf("MedBay", StringComparison.OrdinalIgnoreCase) >= 0 || terminalBlock.BlockDefinition.SubtypeName.IndexOf("SurvivalKit", StringComparison.OrdinalIgnoreCase) >= 0 || terminalBlock.BlockDefinition.SubtypeName.IndexOf("Refill", StringComparison.OrdinalIgnoreCase) >= 0)
                        medical.Add(blockData);
                    else
                    {
                        // Automation by subtype name patterns
                        var sub = terminalBlock.BlockDefinition.SubtypeName;
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
                            automation.Add(blockData);
                        else
                            misc.Add(blockData);
                    }
                }

                // Respect toggles, build categories, then render in alphabetical order
                var cats = new List<KeyValuePair<string, List<BlockStateData>>>();
                if (surfaceData.showAutomation && automation.Count > 0) cats.Add(new KeyValuePair<string, List<BlockStateData>>("Automation", automation));
                if (surfaceData.showCommunications && communications.Count > 0) cats.Add(new KeyValuePair<string, List<BlockStateData>>("Communications", communications));
                if (surfaceData.showContainers && containers.Count > 0) cats.Add(new KeyValuePair<string, List<BlockStateData>>("Containers", containers));
                if (surfaceData.showControllers && controllers.Count > 0) cats.Add(new KeyValuePair<string, List<BlockStateData>>("Controllers", controllers));
                if (surfaceData.showDoors && doors.Count > 0) cats.Add(new KeyValuePair<string, List<BlockStateData>>("Doors", doors));
                if (surfaceData.showGyros && gyros.Count > 0) cats.Add(new KeyValuePair<string, List<BlockStateData>>("Gyros", gyros));
                if (surfaceData.showJumpdrives && jumpdrives.Count > 0) cats.Add(new KeyValuePair<string, List<BlockStateData>>("Jumpdrives", jumpdrives));
                if (surfaceData.showMechanical && mechanical.Count > 0) cats.Add(new KeyValuePair<string, List<BlockStateData>>("Mechanical", mechanical));
                if (surfaceData.showMedical && medical.Count > 0) cats.Add(new KeyValuePair<string, List<BlockStateData>>("Medical", medical));
                if (surfaceData.showMovement && movement.Count > 0) cats.Add(new KeyValuePair<string, List<BlockStateData>>("Movement", movement));
                if (surfaceData.showPower && power.Count > 0) cats.Add(new KeyValuePair<string, List<BlockStateData>>("Power", power));
                if (surfaceData.showProduction && production.Count > 0) cats.Add(new KeyValuePair<string, List<BlockStateData>>("Production", production));
                if (surfaceData.showTanks && tanks.Count > 0) cats.Add(new KeyValuePair<string, List<BlockStateData>>("Tanks", tanks));
                if (surfaceData.showTools && tools.Count > 0) cats.Add(new KeyValuePair<string, List<BlockStateData>>("Tools", tools));
                if (surfaceData.showWeapons && weapons.Count > 0) cats.Add(new KeyValuePair<string, List<BlockStateData>>("Weapons", weapons));
                if (misc.Count > 0) cats.Add(new KeyValuePair<string, List<BlockStateData>>("Misc", misc));

                // Sort A→Z, but keep 'Misc' as the last category
                foreach (var kv in cats
                    .OrderBy(k => k.Key.Equals("Misc", StringComparison.OrdinalIgnoreCase))
                    .ThenBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
                {
                    DrawSystemSprite(ref frame, ref position, surfaceData, kv.Key, kv.Value);
                }

                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.SurfaceDrawer: Caught Exception while DrawIntegritySummarySprite: {e.ToString()}");
            }
        }

        public static void DrawSystemSprite(ref MySpriteDrawFrame frame, ref Vector2 position, SurfaceData surfaceData, string title, List<BlockStateData> blocks)
        {
            if (blocks.Count <= 0) return;

            try
            {
                var blockCount = 0;
                var damagedBlockCount = 0;

                foreach (var block in blocks)
                {
                    if (block.IsNull) continue;

                    blockCount++;

                    if (!block.IsFullIntegrity)
                        damagedBlockCount++;
                }

                var total = blocks.Sum(block => block.MaxIntegrity);
                var current = blocks.Sum(block => block.CurrentIntegrity);

                // No need to display fully functional systems unless showing healthy categories
                if (!surfaceData.showHealthyCategories && current == total) return;

                // Avoid division by 0.
                total = total <= 0 ? 1 : total;

                var ratio = current / total;

                // Do not truncate category titles on Systems screen; display full
                WriteTextSprite(ref frame, position, surfaceData, $"{title}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);

                DrawHalfBar(ref frame, position, surfaceData, TextAlignment.RIGHT, current, total, Unit.Percent, !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : ratio > .9 ? Color.GreenYellow : ratio > .66 ? Color.Yellow : ratio > .33 ? Color.Orange : Color.DarkRed);
                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.SurfaceDrawer: Caught Exception while DrawSystemSprite: {e.ToString()}");
            }
        }

        public static void DrawResourceSummarySprite(ref MySpriteDrawFrame frame, ref Vector2 position, SurfaceData surfaceData, List<IMyGasTank> tanks, List<IMyReactor> reactors)
        {
            try
            {
                if (tanks.Count > 0)
                {
                    WriteTextSprite(ref frame, position, surfaceData, $"Resources", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    position += surfaceData.newLine;
                    DrawGasTankSprite(ref frame, ref position, surfaceData, "Hydrogen", "HYD", tanks);
                    DrawGasTankSprite(ref frame, ref position, surfaceData, "Oxygen", "OXY", tanks);
                }

                if (reactors.Count > 0)
                {
                    float current = 0.0f;
                    float total = 0.0f;

                    foreach (var reactor in reactors)
                    {
                        current += (float)reactor.GetInventory(0).CurrentVolume;
                        total += (float)reactor.GetInventory(0).MaxVolume;
                    }

                    DrawBar(ref frame, ref position, surfaceData, "U", current, total, Unit.Percent, true, false);
                    position += surfaceData.newLine;
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.SurfaceDrawer: Caught Exception while DrawResourceSummarySprite: {e.ToString()}");
            }
        }

        public static void DrawJumpDriveSprite(ref MySpriteDrawFrame frame, ref Vector2 position, SurfaceData surfaceData, List<IMyJumpDrive> jumpdrives, bool isStation, bool compactMode)
        {
            if (jumpdrives.Count <= 0) return;

            try
            {
                if (isStation)
                {
                    WriteTextSprite(ref frame, position, surfaceData, $">> Stationary object! Jumpdrives disfunctional. <<    ", TextAlignment.CENTER, !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : Color.Orange);
                    position += surfaceData.newLine;
                }
                position += surfaceData.newLine;

                var current = jumpdrives.Sum(block => block.CurrentStoredPower);
                var total = jumpdrives.Sum(block => block.MaxStoredPower);
                var currentDistance = jumpdrives[0].MaxJumpDistanceMeters;
                var maximumDistance = jumpdrives[0].MaxJumpDistanceMeters;
                var state = "";
                var offline = false;
                int rechargeTimeLeft = 0;

                foreach (var drive in jumpdrives)
                {
                    if (drive == null) continue;

                    offline = !drive.IsWorking;

                    if (offline) continue;

                    string[] detailedInfo = drive.DetailedInfo.Split('\n');
                    bool isMinutes = detailedInfo[6].Contains("min");
                    int timeLeft = 0;
                    int.TryParse(detailedInfo[6].Replace("Fully recharged in:", "").Replace("min", "").Replace("sec", "").Trim(), out timeLeft);
                    timeLeft *= isMinutes ? 60 : 1;
                    rechargeTimeLeft = timeLeft > rechargeTimeLeft ? timeLeft : rechargeTimeLeft;

                    currentDistance = drive.JumpDistanceMeters < currentDistance ? drive.JumpDistanceMeters : currentDistance;
                    maximumDistance = drive.MaxJumpDistanceMeters > maximumDistance ? drive.MaxJumpDistanceMeters : maximumDistance;
                    state = drive.Status.ToString().Contains("Jumping") ? "Jumping" : drive.Status.ToString().Contains("Charging") ? "Charging" : "  Ready";
                }

                state = offline ? "    Off" : state;

                if (compactMode)
                {
                    position -= surfaceData.newLine;
                    surfaceData.textSize *= 1.4f;
                    surfaceData.newLine = new Vector2(0, 30 * surfaceData.textSize);
                }

                // JumpDrive Stored Power Summary
                var ratio = current / total;
                WriteTextSprite(ref frame, position, surfaceData, $"   {state}", TextAlignment.LEFT, !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : state.Contains("Off") ? Color.Orange : state.Contains("Jump") ? Color.Aquamarine : state.Contains("Charg") ? Color.Yellow : Color.GreenYellow);
                WriteTextSprite(ref frame, position, surfaceData, $"[                  ] Jumpdrives ({jumpdrives.Count}) {(rechargeTimeLeft <= 0 ? "" : " - " + (rechargeTimeLeft > 60 ? rechargeTimeLeft / 60 + " min" : rechargeTimeLeft + " sec"))}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                DrawHalfBar(ref frame, position, surfaceData, TextAlignment.RIGHT, current, total, Unit.Percent, ratio > .95f ? Color.GreenYellow : ratio > .75f ? Color.Yellow : ratio > .25f ? Color.Orange : ratio > 0f ? Color.Red : surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine;

                // JumpDrive Range Summary
                WriteTextSprite(ref frame, position, surfaceData, $"   {MahDefinitions.KiloFormat(currentDistance)}m", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                WriteTextSprite(ref frame, position, surfaceData, $"[                  ] Target distance", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                DrawHalfBar(ref frame, position, surfaceData, TextAlignment.RIGHT, currentDistance, maximumDistance, Unit.Percent, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.SurfaceDrawer: Caught Exception while DrawJumpDriveSprite: {e.ToString()}");
            }
        }
        
        public static void DrawCargoItemBar(ref MySpriteDrawFrame frame, ref Vector2 position, SurfaceData surfaceData, Dictionary<string, CargoItemType> dict, string title, float total)
        {
            try
            {
                if (dict.Count > 0 || surfaceData.showMissing)
                {
                    var volume = 0.0f;

                    foreach (var item in MahSorting.SortCargoItems(dict, MahSorting.ItemSortMode.SubtypeId))
                        volume += item.Value.amount * item.Value.definition.volume;
                    volume /= 1000;

                    if (surfaceData.showBars)
                    {
                        DrawBar(ref frame, ref position, surfaceData, title, volume, total, Unit.Percent, false, false);
                    }
                    else
                    {
                        var ratio = (volume / total) * 100;
                        WriteTextSprite(ref frame, position, surfaceData, $"{MahUtillities.GetSubstring(title, surfaceData, true)}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                        WriteTextSprite(ref frame, position, surfaceData, $"{ratio.ToString("0.0")}%", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                        position += surfaceData.newLine;
                    }
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.SurfaceDrawer: Caught Exception while DrawCargoItemBar: {e.ToString()}");
            }
        }

        public static void DrawHalfBar (ref MySpriteDrawFrame frame, Vector2 position, SurfaceData surfaceData, TextAlignment alignment, float current, float total, Unit unit, Color color)
        {
            try
            {
                // Avoid division by 0
                total = total <= 0 ? 1 : total;

                var pixelPerChar = MahDefinitions.pixelPerChar * surfaceData.textSize;
                var barLength = (int)((alignment == TextAlignment.RIGHT ? (surfaceData.ratioOffset / pixelPerChar) : (surfaceData.titleOffset / pixelPerChar)) * .8f);
                var ratio = current / total;
                var currentValue = (int)(ratio * barLength);

                string backgroundBar = "";
                string bar = "";

                for (int i = 0; i < barLength; i++)
                {
                    backgroundBar += "\'";
                }

                for (int i = 0; i < barLength; i++)
                {
                    if (i < currentValue)
                    {
                        bar += "|";
                    }
                }

                string unitString = unit == Unit.None ? "" : surfaceData.showRatio || unit == Unit.Percent ? $"{((ratio * 100).ToString("0.0"))}%" : unit == Unit.Liters ? $"{MahDefinitions.LiterFormat(current)}" : unit == Unit.Watt ? $"{MahDefinitions.WattFormat(current)}" : $"{MahDefinitions.KiloFormat(current)}";

                if (alignment == TextAlignment.RIGHT)
                {
                    WriteTextSprite(ref frame, position, surfaceData, $"{unitString} [{backgroundBar}]", alignment, surfaceData.surface.ScriptForegroundColor);
                }
                else
                {
                    WriteTextSprite(ref frame, position, surfaceData, $"[{backgroundBar}] {unitString}", alignment, surfaceData.surface.ScriptForegroundColor);
                }
                if (current > 0) WriteTextSprite(ref frame, position, surfaceData, $" {bar} ", alignment, surfaceData.useColors ? color : surfaceData.surface.ScriptForegroundColor);
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.SurfaceDrawer: Caught Exception while drawing half bar: {e.ToString()}");
            }
        }

        public static void DrawBarFixedColor(ref MySpriteDrawFrame frame, ref Vector2 position, SurfaceData surfaceData, string title, double current, double total, Color barColor, Unit unit = Unit.Count)
        {
            try
            {
                // Avoid division by 0
                total = total <= 0 ? 1 : total;

                Vector2 titleOffset = new Vector2((title != "" ? surfaceData.titleOffset : 0) * surfaceData.textSize, 0);
                Vector2 ratioOffset = new Vector2(surfaceData.ratioOffset * surfaceData.textSize, 0);
                double ratio = current / total;
                float pixelPerChar = MahDefinitions.pixelPerChar * surfaceData.textSize;
                int barLength = (int)Math.Floor((surfaceData.surface.SurfaceSize.X - (2 * surfaceData.viewPortOffsetX) - titleOffset.X - ratioOffset.X - (4 * pixelPerChar)) / pixelPerChar);
                int currentValue = (int)(barLength * ratio);

                string backgroundBar = " ";
                string bar = " ";

                for (int i = 0; i < barLength; i++)
                {
                    backgroundBar += "\'";
                }

                for (int i = 0; i < barLength; i++)
                {
                    if (i < currentValue)
                    {
                        bar += "|";
                    }
                }

                barColor = !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : barColor;
                // Improved fixed-field title truncation to avoid overlap with bar start
                // Keep original fixed bar start behavior (titleOffset) but ensure title fits
                {
                    float titlePixelLimit = (surfaceData.titleOffset * surfaceData.textSize) - (pixelPerChar * 1.5f); // margin before '['
                    if (titlePixelLimit < 0) titlePixelLimit = 0;
                    int maxChars = (int)(titlePixelLimit / pixelPerChar);
                    if (maxChars < 0) maxChars = 0;
                    if (title.Length > maxChars)
                    {
                        if (maxChars > 1)
                            title = title.Substring(0, Math.Max(0, maxChars - 1)) + "…"; // ellipsis
                        else
                            title = ""; // nothing fits
                    }
                }

                // Print Bar
                WriteTextSprite(ref frame, position, surfaceData, $"{title}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                WriteTextSprite(ref frame, position + titleOffset, surfaceData, "[", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                WriteTextSprite(ref frame, position + titleOffset, surfaceData, backgroundBar + "]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                WriteTextSprite(ref frame, position + titleOffset, surfaceData, bar, TextAlignment.LEFT, barColor);

                string unitString = unit == Unit.None ? "" : surfaceData.showRatio || unit == Unit.Percent ? $"{(ratio * 100).ToString("#0.0")} %" : unit == Unit.Liters ? $"{MahDefinitions.LiterFormat(current)}" : unit == Unit.Watt ? $"{MahDefinitions.WattFormat(current)}" : unit == Unit.WattHours ? $"{MahDefinitions.WattFormat(current)}h" : unit == Unit.Kilograms ? $"{MahDefinitions.KiloFormat(current)}g" : $"{MahDefinitions.KiloFormat(current)}";

                WriteTextSprite(ref frame, position, surfaceData, $"{unitString}", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);

                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.SurfaceDrawer: Caught Exception while drawing bar: {e.ToString()}");
            }
        }

        public static void DrawBar(ref MySpriteDrawFrame frame, ref Vector2 position, SurfaceData surfaceData, string title, double current, double total, Unit unit = Unit.Count, bool invertColors = false, bool ignoreColors = false)
        {
            try
            {
                // Avoid division by 0
                total = total <= 0 ? 1 : total;

                Vector2 titleOffset = new Vector2((title != "" ? surfaceData.titleOffset : 0) * surfaceData.textSize, 0);
                Vector2 ratioOffset = new Vector2(surfaceData.ratioOffset * surfaceData.textSize, 0);
                double ratio = current / total;
                float pixelPerChar = MahDefinitions.pixelPerChar * surfaceData.textSize;
                int barLength = (int)Math.Floor((surfaceData.surface.SurfaceSize.X - (2 * surfaceData.viewPortOffsetX) - titleOffset.X - ratioOffset.X - (4 * pixelPerChar)) / pixelPerChar);
                int currentValue = (int)(barLength * ratio);

                string backgroundBar = " ";
                string bar = " ";

                for (int i = 0; i < barLength; i++)
                {
                    backgroundBar += "\'";
                }

                for (int i = 0; i < barLength; i++)
                {
                    if (i < currentValue)
                    {
                        bar += "|";
                    }
                }

                Color barColor = surfaceData.surface.ScriptForegroundColor;

                if (surfaceData.useColors && !ignoreColors)
                {
                    if (ratio > .9f)
                        barColor = invertColors ? Color.GreenYellow : Color.Red;
                    else if (ratio > .66f)
                        barColor = invertColors ? Color.Yellow : Color.Orange;
                    else if (ratio > .33f)
                        barColor = invertColors ? Color.Orange : Color.Yellow;
                    else
                        barColor = invertColors ? Color.Red : Color.GreenYellow;
                }

                // Improved fixed-field title truncation to avoid overlap with bar start
                {
                    float titlePixelLimit = (surfaceData.titleOffset * surfaceData.textSize) - (pixelPerChar * 1.5f);
                    if (titlePixelLimit < 0) titlePixelLimit = 0;
                    int maxChars = (int)(titlePixelLimit / pixelPerChar);
                    if (maxChars < 0) maxChars = 0;
                    if (title.Length > maxChars)
                    {
                        if (maxChars > 1)
                            title = title.Substring(0, Math.Max(0, maxChars - 1)) + "…";
                        else
                            title = "";
                    }
                }

                // Print Bar
                WriteTextSprite(ref frame, position, surfaceData, $"{title}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                WriteTextSprite(ref frame, position + titleOffset, surfaceData, "[", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                WriteTextSprite(ref frame, position + titleOffset, surfaceData, backgroundBar + "]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                WriteTextSprite(ref frame, position + titleOffset, surfaceData, bar, TextAlignment.LEFT, barColor);

                string unitString = unit == Unit.None ? "" : surfaceData.showRatio || unit == Unit.Percent ? $"{(ratio * 100).ToString("#0.0")} %" : unit == Unit.Liters ? $"{MahDefinitions.LiterFormat(current)}" : unit == Unit.Watt ? $"{MahDefinitions.WattFormat(current)}" : unit == Unit.WattHours ? $"{MahDefinitions.WattFormat(current)}h" : unit == Unit.Kilograms ? $"{MahDefinitions.KiloFormat(current)}g" : $"{MahDefinitions.KiloFormat(current)}";

                WriteTextSprite(ref frame, position, surfaceData, $"{unitString}", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);

                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.SurfaceDrawer: Caught Exception while drawing bar: {e.ToString()}");
            }
        }

        public static void WriteTextSprite(ref MySpriteDrawFrame frame, Vector2 position, SurfaceData surfaceData, string text, TextAlignment alignment, Color color)
        {
            Color fontColor = color;

            if (alignment == TextAlignment.RIGHT)
                position += new Vector2(surfaceData.surface.SurfaceSize.X - (2 * surfaceData.viewPortOffsetX), 0);
            else if (alignment == TextAlignment.CENTER)
                position += new Vector2(surfaceData.surface.SurfaceSize.X * .5f, 0);

            var sprite = new MySprite
            {
                Type = SpriteType.TEXT,
                Data = text,
                Position = position,
                RotationOrScale = surfaceData.textSize,
                Color = fontColor,
                Alignment = alignment,
                FontId = "White"
            };

            frame.Add(sprite);
        }

        // Farming summary drawer (C#6 compliant, simple counters)
        public static void DrawFarmingSummarySprite(ref MySpriteDrawFrame frame, ref Vector2 position, SurfaceData surfaceData,
            bool showFarmPlots, bool showAlgaeFarms, bool showIrrigationBlocks,
            int totalFarmPlots, int algaeFarmCount, int irrigationBlockCount)
        {
            try
            {
                if (!showFarmPlots && !showAlgaeFarms && !showIrrigationBlocks)
                {
                    WriteTextSprite(ref frame, position, surfaceData, "All counters hidden (enable in config)", TextAlignment.LEFT, Color.Orange);
                    position += surfaceData.newLine;
                    return;
                }

                if (showFarmPlots)
                {
                    WriteTextSprite(ref frame, position, surfaceData, "Farm Plots", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    WriteTextSprite(ref frame, position, surfaceData, totalFarmPlots.ToString(), TextAlignment.RIGHT,
                        surfaceData.useColors ? (totalFarmPlots > 0 ? Color.GreenYellow : Color.OrangeRed) : surfaceData.surface.ScriptForegroundColor);
                    position += surfaceData.newLine;
                }

                if (showAlgaeFarms)
                {
                    WriteTextSprite(ref frame, position, surfaceData, "Algae Farms", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    WriteTextSprite(ref frame, position, surfaceData, algaeFarmCount.ToString(), TextAlignment.RIGHT,
                        surfaceData.useColors ? (algaeFarmCount > 0 ? Color.Aqua : Color.OrangeRed) : surfaceData.surface.ScriptForegroundColor);
                    position += surfaceData.newLine;
                }

                if (showIrrigationBlocks)
                {
                    WriteTextSprite(ref frame, position, surfaceData, "Irrigation Blocks", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    WriteTextSprite(ref frame, position, surfaceData, irrigationBlockCount.ToString(), TextAlignment.RIGHT,
                        surfaceData.useColors ? (irrigationBlockCount > 0 ? Color.LightSkyBlue : Color.OrangeRed) : surfaceData.surface.ScriptForegroundColor);
                    position += surfaceData.newLine;
                }

                if (totalFarmPlots == 0 && algaeFarmCount == 0 && irrigationBlockCount == 0)
                {
                    WriteTextSprite(ref frame, position, surfaceData, "No farming related blocks found.", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    position += surfaceData.newLine;
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("MahrianeIndustries.LCDInfo.SurfaceDrawer: Exception in DrawFarmingSummarySprite: " + e.ToString());
            }
        }
    }
}
