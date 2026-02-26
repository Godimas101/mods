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
    [MyTextSurfaceScript("LCDInfoScreenWeaponsSummary", "$IOS LCD - Weapons")]
    public class LCDWeaponsSummaryInfo : MyTextSurfaceScriptBase
    {
        MyIni config = new MyIni();
        // Centering constants for left-side status badges; tweak Bias to shift text left (-) or right (+)
        const int BadgeInnerWidth = 16;         // number of spaces inside "[                ]"
        const float BadgeCenterBiasChars = 1.55f; // nudge in character widths (negative = left, positive = right)

        public static string CONFIG_SECTION_ID = "SettingsWeaponsSummary";

        List<string> item_types = new List<string>
        {
            "AmmoMagazine",
        };

        string searchId = "*";
        bool detailedInfo = true;
        bool showTurrets = true;
        bool showInteriors = true;
        bool showCannons = true;
        bool showCustom = true;

        void TryCreateSurfaceData()
        {
            if (surfaceData != null)
                return;

            compactMode = mySurface.SurfaceSize.X / mySurface.SurfaceSize.Y > 4;
            var textSize = compactMode ? .4f : .4f;

            surfaceData = new SurfaceDrawer.SurfaceData
            {
                surface = mySurface,
                textSize = textSize,
                titleOffset = 128,
                ratioOffset = mySurface.SurfaceSize.X > 300 || detailedInfo ? 128 : 64,
                viewPortOffsetX = compactMode ? 10 : 20,
                viewPortOffsetY = compactMode ? 5 : 10,
                newLine = new Vector2(0, 30 * textSize),
                showHeader = true,
                showSummary = false,
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
            
            // Preserve existing CustomData from other sections only (not our config section)
            string existing = myTerminalBlock.CustomData ?? "";
            if (!string.IsNullOrWhiteSpace(existing))
            {
                // Manually remove our section while preserving all other content (including comments)
                string[] lines = existing.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                bool inOurSection = false;
                bool addedOtherContent = false;
                
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    string trimmed = line.Trim();
                    
                    // Check if this is a section header
                    if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                    {
                        string sectionName = trimmed.Substring(1, trimmed.Length - 2);
                        inOurSection = (sectionName == CONFIG_SECTION_ID);
                        
                        // If not our section, add it
                        if (!inOurSection)
                        {
                            if (addedOtherContent) sb.AppendLine(); // blank line before new section
                            sb.AppendLine(line);
                            addedOtherContent = true;
                        }
                    }
                    else if (!inOurSection && addedOtherContent)
                    {
                        // Add any line that's not in our section
                        sb.AppendLine(line);
                    }
                    else if (!inOurSection && !string.IsNullOrWhiteSpace(trimmed))
                    {
                        // First non-empty line before any section
                        sb.AppendLine(line);
                        addedOtherContent = true;
                    }
                }
                
                // Add blank line separator if we preserved other content
                if (addedOtherContent)
                    sb.AppendLine();
            }

            sb.AppendLine($"[{CONFIG_SECTION_ID}]");
            sb.AppendLine();
            sb.AppendLine("; [ WEAPONS - GENERAL OPTIONS ]");
            ConfigHelpers.AppendSearchIdConfig(sb, searchId);
            ConfigHelpers.AppendExcludeIdsConfig(sb, excludeIds);
            ConfigHelpers.AppendShowHeaderConfig(sb, surfaceData.showHeader);
            ConfigHelpers.AppendShowSubgridsConfig(sb, surfaceData.showSubgrids);
            ConfigHelpers.AppendSubgridUpdateFrequencyConfig(sb, surfaceData.subgridUpdateFrequency);
            ConfigHelpers.AppendUseColorsConfig(sb, surfaceData.useColors);

            sb.AppendLine();
            sb.AppendLine("; [ WEAPONS - LAYOUT OPTIONS ]");
            sb.AppendLine($"TextSize={surfaceData.textSize}");
            sb.AppendLine($"ViewPortOffsetX={surfaceData.viewPortOffsetX}");
            sb.AppendLine($"ViewPortOffsetY={surfaceData.viewPortOffsetY}");
            sb.AppendLine($"TitleFieldWidth={surfaceData.titleOffset}");
            sb.AppendLine($"RatioFieldWidth={surfaceData.ratioOffset}");

            sb.AppendLine();
            sb.AppendLine("; [ WEAPONS - SCREEN OPTIONS ]");
            sb.AppendLine($"ShowTurrets={showTurrets}");
            sb.AppendLine($"ShowInteriors={showInteriors}");
            sb.AppendLine($"ShowCannons={showCannons}");
            sb.AppendLine($"ShowCustom={showCustom}");
            sb.AppendLine($"DetailedInfo={detailedInfo}");

            sb.AppendLine();

            CreateCargoItemDefinitionList();

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

                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowTurrets", ref showTurrets, ref configError);
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowInteriors", ref showInteriors, ref configError);
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowCannons", ref showCannons, ref configError);
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowCustom", ref showCustom, ref configError);
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "DetailedInfo", ref detailedInfo, ref configError);
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "UseColors", ref surfaceData.useColors, ref configError);

                    CreateExcludeIdsList();
                }
                else
                {
                    MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenWeaponsSummary: Config Syntax error at Line {result}");
                }
                
                // Apply compact-mode overrides similar to Ores screen when on corner LCDs
                if (compactMode)
                {
                    detailedInfo = false; // force compact rendering
                    surfaceData.textSize = 0.4f; // align with Systems screen default text size
                    surfaceData.viewPortOffsetX = 10;
                    surfaceData.viewPortOffsetY = 5;
                    surfaceData.newLine = new Vector2(0, 30 * surfaceData.textSize);
                }

                CreateCargoItemDefinitionList();
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenWeaponsSummary: Caught Exception while loading config: {e.ToString()}");
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

        void CreateCargoItemDefinitionList()
        {
            itemDefinitions.Clear();

            foreach (CargoItemDefinition definition in MahDefinitions.cargoItemDefinitions)
            {
                if (item_types.Contains(definition.typeId))
                {
                    itemDefinitions.Add(new CargoItemDefinition { typeId = definition.typeId, subtypeId = definition.subtypeId, displayName = definition.displayName, volume = definition.volume, minAmount = 0, sortId = definition.sortId });
                }
            }

            foreach (CargoItemDefinition definition in unknownItemDefinitions)
            {
                if (item_types.Contains(definition.typeId))
                {
                    itemDefinitions.Add(new CargoItemDefinition { typeId = definition.typeId, subtypeId = definition.subtypeId, displayName = definition.displayName, volume = definition.volume, minAmount = 0, sortId = definition.sortId });
                }
            }
        }

        IMyTextSurface mySurface;
        IMyTerminalBlock myTerminalBlock;

        List<string> excludeIds = new List<string>();
        List<CargoItemDefinition> itemDefinitions = new List<CargoItemDefinition>();
        List<CargoItemDefinition> unknownItemDefinitions = new List<CargoItemDefinition>();
        List<IMyInventory> inventories = new List<IMyInventory>();
        List<VRage.Game.ModAPI.Ingame.MyInventoryItem> inventoryItems = new List<VRage.Game.ModAPI.Ingame.MyInventoryItem>();
        List<IMyLargeTurretBase> turrets = new List<IMyLargeTurretBase>();
        List<IMyLargeInteriorTurret> interiorTurrets = new List<IMyLargeInteriorTurret>();
        List<IMyUserControllableGun> cannons = new List<IMyUserControllableGun>();
        List<IMyTurretControlBlock> customTurretControllers = new List<IMyTurretControlBlock>();

        // Cached subgrid collections
        List<IMyInventory> subgridInventories = new List<IMyInventory>();
        List<IMyLargeTurretBase> subgridTurrets = new List<IMyLargeTurretBase>();
        List<IMyLargeInteriorTurret> subgridInteriorTurrets = new List<IMyLargeInteriorTurret>();
        List<IMyUserControllableGun> subgridCannons = new List<IMyUserControllableGun>();
        List<IMyTurretControlBlock> subgridCustomTurretControllers = new List<IMyTurretControlBlock>();

    // Reusable lists to avoid GC allocations
    List<Sandbox.ModAPI.Ingame.IMyFunctionalBlock> _cachedTurretTools = new List<Sandbox.ModAPI.Ingame.IMyFunctionalBlock>();

        Dictionary<string, CargoItemType> cargo = new Dictionary<string, CargoItemType>();
        float pixelPerChar;

        VRage.Collections.DictionaryValuesReader<MyDefinitionId, MyDefinitionBase> myDefinitions;
        MyDefinitionId myDefinitionId;
        SurfaceDrawer.SurfaceData surfaceData;
        string gridId = "Unknown grid";
        int subgridScanTick = 0;
        bool configError = false;
        bool compactMode = false;
        bool isStation = false;
        Sandbox.ModAPI.Ingame.MyShipMass gridMass;

        public LCDWeaponsSummaryInfo(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
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

            MahDefinitions.LoadExternalItems();
            if (myTerminalBlock.CustomData.Length <= 0 || !myTerminalBlock.CustomData.Contains(CONFIG_SECTION_ID))
                CreateConfig();

            LoadConfig();

            UpdateBlocksAndInventories();
            UpdateContents();

            pixelPerChar = MahDefinitions.pixelPerChar * surfaceData.textSize;
            var myFrame = mySurface.DrawFrame();
            var myViewport = new RectangleF((mySurface.TextureSize - mySurface.SurfaceSize) / 2f, mySurface.SurfaceSize);
            var myPosition = new Vector2(surfaceData.viewPortOffsetX, surfaceData.viewPortOffsetY) + myViewport.Position;
            myDefinitions = MyDefinitionManager.Static.GetAllDefinitions();

            if (configError)
                SurfaceDrawer.DrawErrorSprite(ref myFrame, surfaceData, $"<< Config error. Please Delete CustomData >>", Color.Orange);
            else
            {
                DrawWeaponsMainSprite(ref myFrame, ref myPosition);
            }

            myFrame.Dispose();
        }

        void UpdateBlocksAndInventories()
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
                    
                    // Extract subgrid-only blocks and inventories
                    subgridCannons.Clear();
                    subgridTurrets.Clear();
                    subgridInventories.Clear();
                    subgridInteriorTurrets.Clear();
                    subgridCustomTurretControllers.Clear();

                    foreach (var block in allBlocks)
                    {
                        if (block == null || mainBlocks.Contains(block)) continue;

                        if (block is IMyTurretControlBlock)
                        {
                            subgridCustomTurretControllers.Add((IMyTurretControlBlock)block);
                        }
                        else if (block is IMyUserControllableGun)
                        {
                            if (block is IMyLargeInteriorTurret)
                            {
                                subgridInteriorTurrets.Add((IMyLargeInteriorTurret)block);

                                if (block.HasInventory)
                                {
                                    for (int i = 0; i < block.InventoryCount; i++)
                                    {
                                        subgridInventories.Add(block.GetInventory(i));
                                    }
                                }
                            }
                            else if (block is IMyLargeTurretBase)
                            {
                                subgridTurrets.Add((IMyLargeTurretBase)block);

                                if (block.HasInventory)
                                {
                                    for (int i = 0; i < block.InventoryCount; i++)
                                    {
                                        subgridInventories.Add(block.GetInventory(i));
                                    }
                                }
                            }
                            else
                            {
                                subgridCannons.Add((IMyUserControllableGun)block);
                        
                                if (block.HasInventory)
                                {
                                    for (int i = 0; i < block.InventoryCount; i++)
                                    {
                                        subgridInventories.Add(block.GetInventory(i));
                                    }
                                }
                            }
                        }
                    }
                }

                // Process main grid blocks and inventories
                cannons.Clear();
                turrets.Clear();
                inventories.Clear();
                interiorTurrets.Clear();
                customTurretControllers.Clear();

                foreach (var myBlock in mainBlocks)
                {
                    if (myBlock == null) continue;

                    if (myBlock is IMyTurretControlBlock)
                    {
                        customTurretControllers.Add((IMyTurretControlBlock)myBlock);
                    }
                    else if (myBlock is IMyUserControllableGun)
                    {
                        if (myBlock is IMyLargeInteriorTurret)
                        {
                            interiorTurrets.Add((IMyLargeInteriorTurret)myBlock);

                            if (myBlock.HasInventory)
                            {
                                for (int i = 0; i < myBlock.InventoryCount; i++)
                                {
                                    inventories.Add(myBlock.GetInventory(i));
                                }
                            }
                        }
                        else if (myBlock is IMyLargeTurretBase)
                        {
                            turrets.Add((IMyLargeTurretBase)myBlock);

                            if (myBlock.HasInventory)
                            {
                                for (int i = 0; i < myBlock.InventoryCount; i++)
                                {
                                    inventories.Add(myBlock.GetInventory(i));
                                }
                            }
                        }
                        else
                        {
                            cannons.Add((IMyUserControllableGun)myBlock);
                    
                            if (myBlock.HasInventory)
                            {
                                for (int i = 0; i < myBlock.InventoryCount; i++)
                                {
                                    inventories.Add(myBlock.GetInventory(i));
                                }
                            }
                        }
                    }
                }

                // Merge main (fresh) and subgrid (cached) collections
                cannons.AddRange(subgridCannons);
                turrets.AddRange(subgridTurrets);
                interiorTurrets.AddRange(subgridInteriorTurrets);
                customTurretControllers.AddRange(subgridCustomTurretControllers);
                inventories.AddRange(subgridInventories);
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenWeaponsSummary: Caught Exception while updating blocks and inventories: {e.ToString()}");
            }
        }

        void UpdateContents()
        {
            try
            {
                cargo.Clear();

                foreach (var inventory in inventories)
                {
                    if (inventory == null) continue;
                    if (inventory.ItemCount == 0) continue;

                    inventoryItems.Clear();
                    inventory.GetItems(inventoryItems);

                    foreach (var item in inventoryItems.OrderBy(i => i.Type.SubtypeId))
                    {
                        if (item == null) continue;

                        var typeId = item.Type.TypeId.Split('_')[1];
                        var subtypeId = item.Type.SubtypeId;
                        var currentAmount = item.Amount.ToIntSafe();

                        if (item_types.Contains(typeId))
                        {
                            if (!cargo.ContainsKey(subtypeId))
                            {
                                cargo.Add(subtypeId, new CargoItemType { item = item, amount = currentAmount });
                                CargoItemDefinition itemDefinition = FindCargoItemDefinition(typeId, subtypeId);

                                if (itemDefinition == null)
                                {
                                    itemDefinition = new CargoItemDefinition();
                                    itemDefinition.typeId = typeId;
                                    itemDefinition.subtypeId = subtypeId;
                                    itemDefinition.displayName = subtypeId.Length >= 18 ? subtypeId.Substring(0, 18) : subtypeId;
                                    itemDefinition.volume = 1f;
                                    itemDefinition.minAmount = currentAmount;
                                    itemDefinition.sortId = "misc"; // default category

                                    itemDefinitions.Add(itemDefinition);
                                }

                                cargo[subtypeId].definition = itemDefinition;
                            }
                            else
                            {
                                cargo[subtypeId].definition.minAmount = (int)config.Get("SettingsWeaponsSummary", $"{item.Type.SubtypeId}").ToInt64();
                                cargo[subtypeId].amount += currentAmount;
                            }

                            cargo[subtypeId].item = item;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenWeaponsSummary: Caught Exception while updating contents: {e.ToString()}");
            }
        }

        void DrawWeaponsMainSprite (ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            try
            {
                if (surfaceData.showHeader)
                {
                    SurfaceDrawer.DrawHeader(ref frame, ref position, surfaceData, $"Weapons Summary [{(searchId == "*" ? "All" : searchId)} -{excludeIds.Count}]");
                    // In compact mode, nudge up by one line so the first category sits just under the header's summary line
                    if (compactMode) position -= 1 * surfaceData.newLine;
                }

                // Prefer compact mode on corner LCDs, mirroring Ores screen behavior
                if (compactMode || !detailedInfo)
                {
                    if (showCustom)
                        DrawCustomTurretControllerCompactSprite(ref frame, ref position);
                    if (showTurrets)
                        DrawTurretsCompactSprite(ref frame, ref position);
                    if (showInteriors)
                        DrawInteriorTurretsCompactSprite(ref frame, ref position);
                    if (showCannons)
                        DrawCannonsCompactSprite(ref frame, ref position);
                }
                else
                {
                    if (showCustom)
                        DrawCustomTurretsControllerDetailedSprite(ref frame, ref position);
                    if (showTurrets)
                        DrawTurretsDetailedSprite(ref frame, ref position);
                    if (showInteriors)
                        DrawInteriorTurretsDetailedSprite(ref frame, ref position);
                    if (showCannons)
                        DrawCannonsDetailedSprite(ref frame, ref position);
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenWeaponsSummary: Caught Exception while DrawWeaponsMainSprite: {e.ToString()}");
            }
        }

        void DrawInteriorTurretsDetailedSprite(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            if (interiorTurrets.Count <= 0) return;
            
            try
            {
                // Check if there's space for header (1 line) + at least first turret (3 lines) = 4 lines minimum
                float spaceNeeded = 4 * surfaceData.newLine.Y;
                var viewportBottom = (mySurface.TextureSize.Y + mySurface.SurfaceSize.Y) / 2f;
                float spaceAvailable = viewportBottom - position.Y;
                if (spaceAvailable < spaceNeeded)
                    return; // Skip entire category if not enough space

                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Interior Turrets [{interiorTurrets.Count}]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, "Settings", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine;

                foreach (IMyLargeInteriorTurret turret in interiorTurrets)
                {
                    if (turret == null) continue;

                    // Check if there's enough space for a full turret (3 lines)
                    spaceNeeded = 3 * surfaceData.newLine.Y;
                    spaceAvailable = viewportBottom - position.Y;
                    if (spaceAvailable < spaceNeeded)
                        break; // Stop rendering if turret won't fit completely

                    DrawDetailedTurretSprite(ref frame, ref position, turret);
                }
                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenWeaponsSummary: Caught Exception while DrawInteriorTurretsDetailedSprite: {e.ToString()}");
            }
        }

        void DrawInteriorTurretsCompactSprite(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            if (interiorTurrets.Count <= 0) return;

            try
            {
                int total = interiorTurrets.Count;
                int noAmmo = 0, firing = 0, idle = 0;

                foreach (IMyLargeInteriorTurret turret in interiorTurrets)
                {
                    if (turret == null) continue;

                    float curr = (float)turret.GetInventory(0).CurrentVolume;
                    if (!turret.IsWorking)
                        idle++;
                    else if (curr <= 0f)
                        noAmmo++;
                    else if (turret.IsShooting)
                        firing++;
                    else
                        idle++;
                }

                // Compact: anchor the bracketed counters to the right for consistency
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, "Interior Turrets", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                string right = $"[NoAmmo {noAmmo}/{total}] [Idle {idle}/{total}] [Firing {firing}/{total}]";
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, right, TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenWeaponsSummary: Caught Exception while DrawInteriorTurretsCompactSprite: {e.ToString()}");
            }
        }

        void DrawTurretsDetailedSprite(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            if (turrets.Count <= 0) return;

            try
            {
                // Check if there's space for header (1 line) + at least first turret (3 lines) = 4 lines minimum
                float spaceNeeded = 4 * surfaceData.newLine.Y;
                var viewportBottom = (mySurface.TextureSize.Y + mySurface.SurfaceSize.Y) / 2f;
                float spaceAvailable = viewportBottom - position.Y;
                if (spaceAvailable < spaceNeeded)
                    return; // Skip entire category if not enough space

                // Sort turrets alphabetically by custom name
                MahSorting.SortBlocksByName(turrets);

                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Turrets [{turrets.Count}]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, "Settings", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine;

                foreach (IMyLargeTurretBase turret in turrets)
                {
                    if (turret == null) continue;

                    // Check if there's enough space for a full turret (3 lines)
                    spaceNeeded = 3 * surfaceData.newLine.Y;
                    spaceAvailable = viewportBottom - position.Y;
                    if (spaceAvailable < spaceNeeded)
                        break; // Stop rendering if turret won't fit completely

                    DrawDetailedTurretSprite(ref frame, ref position, turret);
                }
                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenWeaponsSummary: Caught Exception while DrawTurretsDetailedSprite: {e.ToString()}");
            }
        }

        void DrawTurretsCompactSprite(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            if (turrets.Count <= 0) return;

            try
            {
                int total = turrets.Count;
                int noAmmo = 0, firing = 0, idle = 0;

                foreach (IMyLargeTurretBase turret in turrets)
                {
                    if (turret == null) continue;

                    float curr = (float)turret.GetInventory(0).CurrentVolume;
                    if (!turret.IsWorking)
                        idle++;
                    else if (curr <= 0f)
                        noAmmo++;
                    else if (turret.IsShooting)
                        firing++;
                    else
                        idle++;
                }

                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, "Turrets", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                string right = $"[NoAmmo {noAmmo}/{total}] [Idle {idle}/{total}] [Firing {firing}/{total}]";
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, right, TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenWeaponsSummary: Caught Exception while DrawTurretsCompactSprite: {e.ToString()}");
            }
        }

        void DrawCustomTurretsControllerDetailedSprite(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            if (customTurretControllers.Count <= 0) return;

            try
            {
                // Check if there's space for header (1 line) + at least first controller (3 lines) = 4 lines minimum
                float spaceNeeded = 4 * surfaceData.newLine.Y;
                var viewportBottom = (mySurface.TextureSize.Y + mySurface.SurfaceSize.Y) / 2f;
                float spaceAvailable = viewportBottom - position.Y;
                if (spaceAvailable < spaceNeeded)
                    return; // Skip entire category if not enough space

                float pixelPerChar = MahDefinitions.pixelPerChar * surfaceData.textSize;
                Vector2 stateOffset = new Vector2(pixelPerChar * 19, 0);
                var maxNameLength = (int)(mySurface.SurfaceSize.X > 300 ? 35 : 20);

                // Sort custom turret controllers alphabetically by custom name
                MahSorting.SortBlocksByName(customTurretControllers);

                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Custom Turrets [{customTurretControllers.Count}]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, "Settings", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine;

                foreach (IMyTurretControlBlock controller in customTurretControllers)
                {
                    if (controller == null) continue;

                    // Check if there's enough space for a full controller (3 lines)
                    spaceNeeded = 3 * surfaceData.newLine.Y;
                    spaceAvailable = viewportBottom - position.Y;
                    if (spaceAvailable < spaceNeeded)
                        break; // Stop rendering if controller won't fit completely

                    var controllerName = controller.CustomName.Length > maxNameLength ? controller.CustomName.Substring(0, maxNameLength) : controller.CustomName;

                    _cachedTurretTools.Clear();
                    controller.GetTools(_cachedTurretTools);
                    var hasGuns = false;
                    var isShooting = false;
                    var isRecharging = false;
                    var secondsLeftToRecharge = 0;
                    var secondsToRecharge = 1;
                    var gunCount = 0;
                    var currentVolume = 0.0f;
                    var maximumVolume = 0.0f;

                    foreach (var myTool in _cachedTurretTools)
                    {
                        if (myTool == null) continue;

                        if (myTool is IMyUserControllableGun)
                        {
                            isShooting = ((IMyUserControllableGun)myTool).IsShooting;
                            hasGuns = true;
                            gunCount++;

                            string detailedInfo = ((IMyUserControllableGun)myTool).DetailedInfo;
                            string rechargeInfo = "";
                            isRecharging = detailedInfo.Contains("Fully recharged in:") && !detailedInfo.Contains("Fully recharged in: 0 sec");

                            if (isRecharging) // Might be a railgun
                            {
                                secondsToRecharge = 60;

                                string[] s = detailedInfo.Split('\n');

                                if (s.Length > 2)
                                {
                                    bool isMinutes = s.Contains("min");
                                    rechargeInfo = (isMinutes ? s[2].Replace(" min", "") : s[2].Replace(" sec", "")).Replace("Fully recharged in: ", "");
                                    int.TryParse(rechargeInfo, out secondsLeftToRecharge);
                                    secondsLeftToRecharge *= isMinutes ? 60 : 1;
                                    secondsLeftToRecharge = secondsLeftToRecharge <= 0 ? 60 : secondsLeftToRecharge;
                                }
                            }

                            if (myTool.HasInventory)
                            {
                                currentVolume += (float)myTool.GetInventory(0).CurrentVolume;
                                maximumVolume += (float)myTool.GetInventory(0).MaxVolume;
                            }
                        }
                    }
                
                    var rechargeBar = $"  {secondsLeftToRecharge} sec";
                    var functional = controller.AzimuthRotor != null && controller.ElevationRotor != null && controller.Camera != null;
                    var state = $"{(!functional ? " Missing" : !controller.IsWorking ? "     Off     " : controller.IsSunTrackerEnabled ? "Tracking" : hasGuns && isRecharging ? $"{rechargeBar}" : hasGuns && currentVolume <= 0 ? "NoAmmo" : isShooting ? "Firing" : controller.IsUnderControl ? "Manned" : controller.AIEnabled ? controller.HasTarget && !controller.IsAimed ? "Follow" : controller.HasTarget && controller.IsAimed ? "Locked" : "Idle" : "Manual")}";
                    // Draw badge shell and name
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"[                ] {controllerName}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    // Overlay centered state inside badge using pixel center (same approach as Life Support)
                    {
                        float ppc = MahDefinitions.pixelPerChar * surfaceData.textSize;
                        float desiredCenterX = position.X + ppc * (1f + (BadgeInnerWidth * 0.5f) + BadgeCenterBiasChars);
                        var centerPos = new Vector2(desiredCenterX - (surfaceData.surface.SurfaceSize.X * 0.5f), position.Y);
                        var overlayColor = !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : state.Contains("Off") || state.Contains("NoAmmo") ? Color.Orange : isRecharging || state.Contains("Follow") ? Color.Yellow : state.Contains("Firing") || state.Contains("Missing") ? Color.Red : state.Contains("Manned") || state.Contains("Tracking") ? Color.Magenta : Color.GreenYellow;
                        SurfaceDrawer.WriteTextSprite(ref frame, centerPos, surfaceData, state.Trim(), TextAlignment.CENTER, overlayColor);
                    }

                    if (functional)
                    {
                        var targetCharacters = $"[{(controller.TargetCharacters ? "X" : "  ")}]";
                        var targetFriends = $"[{(controller.TargetFriends ? "X" : "  ")}]";
                        var targetLargeGrids = $"[{(controller.TargetLargeGrids ? "X" : "  ")}]";
                        var targetMeteors = $"[{(controller.TargetMeteors ? "X" : "  ")}]";
                        var targetMissiles = $"[{(controller.TargetMissiles ? "X" : "  ")}]";
                        var targetNeutrals = $"[{(controller.TargetNeutrals ? "X" : "  ")}]";
                        var targetSmallGrids = $"[{(controller.TargetSmallGrids ? "X" : "  ")}]";
                        var targetStations = $"[{(controller.TargetStations ? "X" : "  ")}]";
                        var targetSet = $"{(controller.IsSunTrackerEnabled ? "Tracking Sun" : "Ch Fr LG Me Mi Ne SG St ")}";
                        var targetting = controller.IsSunTrackerEnabled ? "" : $"{targetCharacters} {targetFriends} {targetLargeGrids} {targetMeteors} {targetMissiles} {targetNeutrals} {targetSmallGrids} {targetStations}";

                        SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{targetSet}", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                        position += surfaceData.newLine;
                        SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"[{(hasGuns ? $"Armed ({gunCount})" : "Unarmed")}]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                        SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{targetting}", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                        position += surfaceData.newLine;
                        if (hasGuns)
                        {
                            SurfaceDrawer.DrawHalfBar(ref frame, position, surfaceData, TextAlignment.LEFT, currentVolume, maximumVolume, Unit.Percent, Color.Orange);
                            SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"[Range: {controller.Range.ToString("#,0")} m]", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                        }
                    }
                    else
                    {
                        position += surfaceData.newLine;
                        var builtState = "";
                        if (controller.AzimuthRotor == null)
                            builtState = "No Azimuth Rotor found";
                        else if (controller.ElevationRotor == null)
                            builtState = "No Elevation Rotor found";
                        else if (controller.Camera == null)
                            builtState = "No Camera found";
                        SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{builtState}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                        position += surfaceData.newLine;
                    }
                    position += surfaceData.newLine;
                }
                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenWeaponsSummary: Caught Exception while DrawCustomTurretsControllerDetailedSprite: {e.ToString()}");
            }
        }

        void DrawCustomTurretControllerCompactSprite(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            if (customTurretControllers.Count <= 0) return;

            try
            {
                int total = customTurretControllers.Count;
                int noAmmo = 0, firing = 0, idle = 0;

                foreach (IMyTurretControlBlock controller in customTurretControllers)
                {
                    if (controller == null) continue;

                    bool functional = controller.AzimuthRotor != null && controller.ElevationRotor != null && controller.Camera != null;
                    bool hasGuns = false, isShooting = false;
                    float currentVolume = 0f;

                    _cachedTurretTools.Clear();
                    controller.GetTools(_cachedTurretTools);
                    foreach (var tool in _cachedTurretTools)
                    {
                        var gun = tool as IMyUserControllableGun;
                        if (gun == null) continue;
                        hasGuns = true;
                        if (gun.IsShooting) isShooting = true;
                        if (tool.HasInventory)
                            currentVolume += (float)tool.GetInventory(0).CurrentVolume;
                    }

                    if (!functional || !controller.IsWorking)
                        idle++;
                    else if (hasGuns && currentVolume <= 0f)
                        noAmmo++;
                    else if (isShooting)
                        firing++;
                    else
                        idle++;
                }

                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, "Custom Turrets", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                string right = $"[NoAmmo {noAmmo}/{total}] [Idle {idle}/{total}] [Firing {firing}/{total}]";
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, right, TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenWeaponsSummary: Caught Exception while DrawCustomTurretControllerCompactSprite: {e.ToString()}");
            }
        }

        void DrawDetailedTurretSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyLargeTurretBase turret)
        {
            if (turret == null) return;

            try
            {
                float pixelPerChar = MahDefinitions.pixelPerChar * surfaceData.textSize;
                var maxNameLength = (int)(mySurface.SurfaceSize.X > 300 ? 35 : 20);
                var turrentName = turret.CustomName.Length > maxNameLength ? turret.CustomName.Substring(0, maxNameLength) : turret.CustomName;
                var currentVolume = (float)turret.GetInventory(0).CurrentVolume;
                var maximumVolume = (float)turret.GetInventory(0).MaxVolume;
                var ammoType = "No Ammo";
                var ammoCount = 0;
                var percentValue = (currentVolume / maximumVolume) * 100;
                Vector2 stateOffset = new Vector2(pixelPerChar * 19, 0);

                if (turret.GetInventory(0).CurrentVolume > 0)
                {
                    List<VRage.Game.ModAPI.Ingame.MyInventoryItem> inventoryItems = new List<VRage.Game.ModAPI.Ingame.MyInventoryItem>();
                    turret.GetInventory(0).GetItems(inventoryItems);

                    foreach (var item in inventoryItems)
                    {
                        if (item == null) continue;

                        if (ammoType == "No Ammo")
                        {
                            ammoType = item.Type.SubtypeId;
                            CargoItemDefinition itemDefinition = MahDefinitions.GetDefinition("AmmoMagazine", ammoType);

                            if (itemDefinition != null)
                            {
                                ammoType = itemDefinition.displayName;
                            }
                        }

                        ammoCount += (int)item.Amount;
                    }
                }

                var targetCharacters = $"[{(turret.TargetCharacters ? "X" : "  ")}]";
                var targetEnemies = $"[{(turret.TargetEnemies ? "X" : "  ")}]";
                var targetLargeGrids = $"[{(turret.TargetLargeGrids ? "X" : "  ")}]";
                var targetMeteors = $"[{(turret.TargetMeteors ? "X" : "  ")}]";
                var targetMissiles = $"[{(turret.TargetMissiles ? "X" : "  ")}]";
                var targetNeutrals = $"[{(turret.TargetNeutrals ? "X" : "  ")}]";
                var targetSmallGrids = $"[{(turret.TargetSmallGrids ? "X" : "  ")}]";
                var targetStations = $"[{(turret.TargetStations ? "X" : "  ")}]";
                var targetSet = $"Ch En LG Me Mi Ne SG St ";
                var targetting = $"{targetCharacters} {targetEnemies} {targetLargeGrids} {targetMeteors} {targetMissiles} {targetNeutrals} {targetSmallGrids} {targetStations}";

                ammoType = ammoType.Length >= maxNameLength ? ammoType.Substring(0, maxNameLength) : ammoType;
                var state = $"{(!turret.IsWorking ? "     Off     " : currentVolume <= 0 ? "NoAmmo" : turret.IsShooting ? "Firing" : turret.IsUnderControl ? "Manned" : turret.HasTarget && !turret.IsAimed ? "Follow" : turret.HasTarget && turret.IsAimed ? "Locked" : "Idle")}";

                // Draw badge shell and name
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"[                ] {turrentName}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                // Overlay centered state inside badge using pixel center
                {
                    float ppc = MahDefinitions.pixelPerChar * surfaceData.textSize;
                    float desiredCenterX = position.X + ppc * (1f + (BadgeInnerWidth * 0.5f) + BadgeCenterBiasChars);
                    var centerPos = new Vector2(desiredCenterX - (surfaceData.surface.SurfaceSize.X * 0.5f), position.Y);
                    var overlayColor = !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : state.Contains("Off") || state.Contains("NoAmmo") ? Color.Orange : state.Contains("Manned") ? Color.Magenta : state.Contains("Follow") ? Color.Yellow : state.Contains("Firing") ? Color.Red : Color.GreenYellow;
                    SurfaceDrawer.WriteTextSprite(ref frame, centerPos, surfaceData, state.Trim(), TextAlignment.CENTER, overlayColor);
                }
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{targetSet}", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine;
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{MahDefinitions.KiloFormat(ammoCount)}x <{ammoType}> ", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{targetting}", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine;
                SurfaceDrawer.DrawHalfBar(ref frame, position, surfaceData, TextAlignment.LEFT, currentVolume, maximumVolume, Unit.Percent, !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : Color.Orange);
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"[Range: {turret.Range.ToString("#,0")} m]", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenWeaponsSummary: Caught Exception while DrawDetailedTurretSprite: {e.ToString()}");
            }
        }

        void DrawCompactTurretSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyLargeTurretBase turret)
        {
            if (turret == null) return;

            try
            {
                float pixelPerChar = MahDefinitions.pixelPerChar * surfaceData.textSize;
                var maxNameLength = (int)(mySurface.SurfaceSize.X > 300 ? 35 : 20);
                var turrentName = turret.CustomName.Length > maxNameLength ? turret.CustomName.Substring(0, maxNameLength) : turret.CustomName;
                var currentVolume = (float)turret.GetInventory(0).CurrentVolume;
                var maximumVolume = (float)turret.GetInventory(0).MaxVolume;
                var ammoType = "No Ammo";
                var percentValue = (currentVolume / maximumVolume) * 100;
                Vector2 stateOffset = new Vector2(pixelPerChar * 19, 0);

                var state = $"{(!turret.IsWorking ? "     Off     " : currentVolume <= 0 ? "NoAmmo" : turret.IsShooting ? "Firing" : turret.IsUnderControl ? "Manned" : turret.HasTarget && !turret.IsAimed ? "Follow" : turret.HasTarget && turret.IsAimed ? "Locked" : "Idle")}";

                // Draw badge shell and name
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"[                ] {turrentName}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                // Overlay centered state inside badge using pixel center
                {
                    float ppc = MahDefinitions.pixelPerChar * surfaceData.textSize;
                    float desiredCenterX = position.X + ppc * (1f + (BadgeInnerWidth * 0.5f) + BadgeCenterBiasChars);
                    var centerPos = new Vector2(desiredCenterX - (surfaceData.surface.SurfaceSize.X * 0.5f), position.Y);
                    var overlayColor = !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : state.Contains("Off") || state.Contains("NoAmmo") ? Color.Orange : state.Contains("Manned") ? Color.Magenta : state.Contains("Follow") ? Color.Yellow : state.Contains("Firing") ? Color.Red : Color.GreenYellow;
                    SurfaceDrawer.WriteTextSprite(ref frame, centerPos, surfaceData, state.Trim(), TextAlignment.CENTER, overlayColor);
                }
                SurfaceDrawer.DrawHalfBar(ref frame, position, surfaceData, TextAlignment.RIGHT, currentVolume, maximumVolume, Unit.Percent, !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : Color.Orange);
                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenWeaponsSummary: Caught Exception while DrawCompactTurretSprite: {e.ToString()}");
            }
        }

        void DrawCannonsDetailedSprite(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            if (cannons.Count <= 0) return;

            try
            {
                // Check if there's space for header (1 line) + at least first cannon (3 lines) = 4 lines minimum
                float spaceNeeded = 4 * surfaceData.newLine.Y;
                var viewportBottom = (mySurface.TextureSize.Y + mySurface.SurfaceSize.Y) / 2f;
                float spaceAvailable = viewportBottom - position.Y;
                if (spaceAvailable < spaceNeeded)
                    return; // Skip entire category if not enough space

                float pixelPerChar = MahDefinitions.pixelPerChar * surfaceData.textSize;
                Vector2 stateOffset = new Vector2(pixelPerChar * 19, 0);
                var maxNameLength = (int)(mySurface.SurfaceSize.X > 300 ? 35 : 20);

                // Sort fixed weapons alphabetically by custom name
                MahSorting.SortBlocksByName(cannons);

                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Fixed Weapons [{cannons.Count}]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, "", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine;

                foreach (IMyUserControllableGun cannon in cannons)
                {
                    if (cannon == null) continue;

                    // Check if there's enough space for a full cannon (3 lines)
                    spaceNeeded = 3 * surfaceData.newLine.Y;
                    spaceAvailable = viewportBottom - position.Y;
                    if (spaceAvailable < spaceNeeded)
                        break; // Stop rendering if cannon won't fit completely

                    var cannonName = cannon.CustomName.Length > maxNameLength ? cannon.CustomName.Substring(0, maxNameLength) : cannon.CustomName;
                    var currentVolume = (float)cannon.GetInventory(0).CurrentVolume;
                    var maximumVolume = (float)cannon.GetInventory(0).MaxVolume;
                    var ammoType = "No Ammo";
                    var ammoCount = 0;
                    var isShooting = cannon.IsShooting;
                    var isRecharging = false;
                    var secondsLeftToRecharge = 0;
                    var secondsToRecharge = 1;
                    var percentValue = (currentVolume / maximumVolume) * 100;

                    string detailedInfo = ((IMyUserControllableGun)cannon).DetailedInfo;
                    string rechargeInfo = "";
                    isRecharging = detailedInfo.Contains("Fully recharged in:") && !detailedInfo.Contains("Fully recharged in: 0 sec");

                    if (isRecharging) // Might be a railgun
                    {
                        secondsToRecharge = 60;

                        string[] s = detailedInfo.Split('\n');

                        if (s.Length > 2)
                        {
                            bool isMinutes = s.Contains("min");
                            rechargeInfo = (isMinutes ? s[2].Replace(" min", "") : s[2].Replace(" sec", "")).Replace("Fully recharged in: ", "");
                            int.TryParse(rechargeInfo, out secondsLeftToRecharge);
                            secondsLeftToRecharge *= isMinutes ? 60 : 1;
                            secondsLeftToRecharge = secondsLeftToRecharge <= 0 ? 60 : secondsLeftToRecharge;
                        }
                    }

                    if (cannon.GetInventory(0).CurrentVolume > 0)
                    {
                        List<VRage.Game.ModAPI.Ingame.MyInventoryItem> inventoryItems = new List<VRage.Game.ModAPI.Ingame.MyInventoryItem>();
                        cannon.GetInventory(0).GetItems(inventoryItems);

                        foreach (var item in inventoryItems)
                        {
                            if (item == null) continue;

                            if (ammoType == "No Ammo")
                            {
                                ammoType = item.Type.SubtypeId;
                                CargoItemDefinition itemDefinition = MahDefinitions.GetDefinition("AmmoMagazine", ammoType);

                                if (itemDefinition != null)
                                {
                                    ammoType = itemDefinition.displayName;
                                }
                            }

                            ammoCount += (int)item.Amount;
                        }
                    }

                    ammoType = ammoType.Length >= 18 ? ammoType.Substring(0, 18) : ammoType;
                    var rechargeBar = $"  {secondsLeftToRecharge} sec";
                    var state = $"{(!cannon.IsWorking ? "     Off     " : isRecharging ? $"{rechargeBar}" : currentVolume <= 0 ? "NoAmmo" : isShooting ? "Firing" : "Ready")}";

                    // Draw badge shell and name
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"[                ] {cannonName}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    // Overlay centered state inside badge using pixel center
                    {
                        float ppc = MahDefinitions.pixelPerChar * surfaceData.textSize;
                        float desiredCenterX = position.X + ppc * (1f + (BadgeInnerWidth * 0.5f) + BadgeCenterBiasChars);
                        var centerPos = new Vector2(desiredCenterX - (surfaceData.surface.SurfaceSize.X * 0.5f), position.Y);
                        var overlayColor = !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : state.Contains("Off") || state.Contains("NoAmmo") ? Color.Orange : isRecharging ? Color.Yellow : state.Contains("Firing") ? Color.Red : Color.GreenYellow;
                        SurfaceDrawer.WriteTextSprite(ref frame, centerPos, surfaceData, state.Trim(), TextAlignment.CENTER, overlayColor);
                    }
                    position += surfaceData.newLine;
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"[{MahDefinitions.KiloFormat(ammoCount)}] <{ammoType}> ", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    position += surfaceData.newLine;
                    SurfaceDrawer.DrawHalfBar(ref frame, position, surfaceData, TextAlignment.LEFT, currentVolume, maximumVolume, Unit.Percent, !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : Color.Orange);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                    position += surfaceData.newLine;
                }

                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenWeaponsSummary: Caught Exception while DrawCannonsDetailedSprite: {e.ToString()}");
            }
        }
        
        void DrawCannonsCompactSprite(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            if (cannons.Count <= 0) return;

            try
            {
                int total = cannons.Count;
                int noAmmo = 0, firing = 0, idle = 0;

                foreach (IMyUserControllableGun cannon in cannons)
                {
                    if (cannon == null) continue;
                    float curr = (float)cannon.GetInventory(0).CurrentVolume;
                    if (!cannon.IsWorking)
                        idle++;
                    else if (curr <= 0f)
                        noAmmo++;
                    else if (cannon.IsShooting)
                        firing++;
                    else
                        idle++;
                }

                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, "Fixed Weapons", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                string right = $"[NoAmmo {noAmmo}/{total}] [Ready {idle}/{total}] [Firing {firing}/{total}]";
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, right, TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenWeaponsSummary: Caught Exception while DrawCannonsCompactSprite: {e.ToString()}");
            }
        }

        bool IgnoreDefinition(CargoItemDefinition itemDefinition)
        {
            string sType = itemDefinition.subtypeId.ToLower();
            string dName = itemDefinition.displayName.ToLower();

            foreach (string s in excludeIds)
                if (sType.Contains(s.ToLower()) || dName.Contains(s.ToLower())) return true;

            return false;
        }

        CargoItemDefinition FindCargoItemDefinition(string typeId, string subtypeId)
        {
            // 3-tier matching strategy for robust item identification:
            // Tier 1: Exact match on both typeId and subtypeId (handles items with duplicate subtypeIds across different typeIds)
            // Tier 2: Contains match on typeId (handles "MyObjectBuilder_PhysicalGunObject" containing "PhysicalGunObject") + exact subtypeId
            // Tier 3: SubtypeId-only fallback (backward compatible, works for items with unique subtypeIds)
            
            foreach (CargoItemDefinition definition in MahDefinitions.OrderedCargoItems(itemDefinitions))
            {
                // Tier 1: Exact match
                if (definition.typeId == typeId && definition.subtypeId == subtypeId) return definition;
                
                // Tier 2: Contains match (handles MyObjectBuilder_ prefix variations)
                if (typeId.Contains(definition.typeId) && definition.subtypeId == subtypeId) return definition;
                
                // Tier 3: SubtypeId fallback (backward compatible)
                if (definition.subtypeId == subtypeId) return definition;
            }

            foreach (CargoItemDefinition definition in unknownItemDefinitions)
            {
                if (definition.typeId == typeId && definition.subtypeId == subtypeId) return definition;
                if (typeId.Contains(definition.typeId) && definition.subtypeId == subtypeId) return definition;
                if (definition.subtypeId == subtypeId) return definition;
            }

            return null;
        }
    }
}
