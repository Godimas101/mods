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

namespace MahrianeIndustries.LCDInfo
{
    public static class MahDefinitions
    {
        public static float pixelPerChar = 6.0f;
        const string ExternalItemsFileName = "AdditionalItems.ini"; // placed at mod root or under Data/
    static bool externalItemsLoaded = false;
    static int externalItemsLoadAttempts = 0; // how many times we've tried to load external items
    const int ExternalItemsMaxAttempts = 600; // ~10 seconds at 60 TPS before giving up silently
        // Cache a reference to this mod's ModItem for APIs that now require it (SE updated overloads)
        static VRage.Game.MyObjectBuilder_Checkpoint.ModItem? selfModItem = null;

        static void EnsureSelfModItem()
        {
            if (selfModItem.HasValue) return;
            try
            {
                var session = MyAPIGateway.Session;
                if (session == null) return; // will try again later
                var mods = session.Mods;
                if (mods == null) return;

                // Try exact / prefix match first
                foreach (var mod in mods)
                {
                    if (mod.Name == null) continue;
                    if (mod.Name.Equals("InfoLCD - Apex Update", StringComparison.OrdinalIgnoreCase) ||
                        mod.Name.StartsWith("InfoLCD - Apex Update", StringComparison.OrdinalIgnoreCase))
                    {
                        selfModItem = mod;
                        break;
                    }
                }

                // Fallback: any mod whose name contains "InfoLCD"
                if (!selfModItem.HasValue)
                {
                    foreach (var mod in mods)
                    {
                        if (!string.IsNullOrEmpty(mod.Name) && mod.Name.IndexOf("InfoLCD", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            selfModItem = mod;
                            break;
                        }
                    }
                }
            }
            catch { }
        }

        // All Vanilla items. If you want to add modded items, simply duplicate the last line, of the desired typeId...
        // ...enter the mod items IMyInventoryItem.Type.SubtypeId into the subtypeId = "id here"
        // ...enter the mod items displayed name into the displayName = "displayed name here"
        // ...enter the mod items volume from the ingame UI into the volume = xf
        // ...enter a desired minAmount into the minAmount = xf field.
        // If you don't want to have a minAmount (will hide the status bar on LCD screen) just set minAmount = 0
       public static List<CargoItemDefinition> cargoItemDefinitions = new List<CargoItemDefinition>
        {
            //Ore Items (alphabetical by displayName)
            new CargoItemDefinition { typeId = "Ore",           subtypeId = "Cobalt",                               displayName = "Cobalt",                 volume = 0.37f,     sortId = "ore", minAmount = 10000   },
            new CargoItemDefinition { typeId = "Ore",           subtypeId = "Gold",                                 displayName = "Gold",                   volume = 0.37f,     sortId = "ore", minAmount = 10000   },
            new CargoItemDefinition { typeId = "Ore",           subtypeId = "Ice",                                  displayName = "Ice",                    volume = 0.37f,     sortId = "ore", minAmount = 20000   },
            new CargoItemDefinition { typeId = "Ore",           subtypeId = "Iron",                                 displayName = "Iron",                   volume = 0.37f,     sortId = "ore", minAmount = 10000   },
            new CargoItemDefinition { typeId = "Ore",           subtypeId = "Magnesium",                            displayName = "Magnesium",              volume = 0.37f,     sortId = "ore", minAmount = 10000   },
            new CargoItemDefinition { typeId = "Ore",           subtypeId = "Nickel",                               displayName = "Nickel",                 volume = 0.37f,     sortId = "ore", minAmount = 10000   },
            new CargoItemDefinition { typeId = "Ore",           subtypeId = "Platinum",                             displayName = "Platinum",               volume = 0.37f,     sortId = "ore", minAmount = 10000   },
            new CargoItemDefinition { typeId = "Ore",           subtypeId = "Scrap",                                displayName = "Scrap",                  volume = 0.254f,    sortId = "ore", minAmount = 0    },
            new CargoItemDefinition { typeId = "Ore",           subtypeId = "Silicon",                              displayName = "Silicon",                volume = 0.37f,     sortId = "ore", minAmount = 10000   },
            new CargoItemDefinition { typeId = "Ore",           subtypeId = "Silver",                               displayName = "Silver",                 volume = 0.37f,     sortId = "ore", minAmount = 10000   },
            new CargoItemDefinition { typeId = "Ore",           subtypeId = "Stone",                                displayName = "Stone",                  volume = 0.37f,     sortId = "ore", minAmount = 10000   },
            new CargoItemDefinition { typeId = "Ore",           subtypeId = "Uranium",                              displayName = "Uranium",                volume = 0.37f,     sortId = "ore", minAmount = 10000   },

            //Modded Ore Items (alphabetical by displayName)
            new CargoItemDefinition { typeId = "Ore",           subtypeId = "HydroPellets",                         displayName = "Nutrient Pellets",       volume = 0.37f,     sortId = "ore", minAmount = 5000    },
            new CargoItemDefinition { typeId = "Ore",           subtypeId = "Organic",                              displayName = "Organic",                volume = 0.37f,     sortId = "ore", minAmount = 5000    },
            //Ingot items (alphabetical by displayName)
            new CargoItemDefinition { typeId = "Ingot",        subtypeId = "Cobalt",                                displayName = "Cobalt",                 volume = 0.112f,    sortId = "ingot", minAmount =  25000  },
            new CargoItemDefinition { typeId = "Ingot",        subtypeId = "Gold",                                  displayName = "Gold",                   volume = 0.052f,    sortId = "ingot", minAmount =   5000  },
            new CargoItemDefinition { typeId = "Ingot",        subtypeId = "Stone",                                 displayName = "Gravel",                 volume = 0.37f,     sortId = "ingot", minAmount = 500    },
            new CargoItemDefinition { typeId = "Ingot",        subtypeId = "Iron",                                  displayName = "Iron",                   volume = 0.127f,    sortId = "ingot", minAmount = 100000  },
            new CargoItemDefinition { typeId = "Ingot",        subtypeId = "Magnesium",                             displayName = "Magnesium Pow.",         volume = 0.575f,    sortId = "ingot", minAmount = 15000   },
            new CargoItemDefinition { typeId = "Ingot",        subtypeId = "Nickel",                                displayName = "Nickel",                 volume = 0.112f,    sortId = "ingot", minAmount = 25000   },
            new CargoItemDefinition { typeId = "Ingot",        subtypeId = "Platinum",                              displayName = "Platinum",               volume = 0.047f,    sortId = "ingot", minAmount = 2000    },
            new CargoItemDefinition { typeId = "Ingot",        subtypeId = "PrototechScrap",                        displayName = "Prototech Scrap",        volume = 1.5f,      sortId = "ingot", minAmount = 20       },
            new CargoItemDefinition { typeId = "Ingot",        subtypeId = "Silicon",                               displayName = "Silicon Waf.",           volume = 0.429f,    sortId = "ingot", minAmount = 15000   },
            new CargoItemDefinition { typeId = "Ingot",        subtypeId = "Silver",                                displayName = "Silver",                 volume = 0.095f,    sortId = "ingot", minAmount = 5000    },
            new CargoItemDefinition { typeId = "Ingot",        subtypeId = "Uranium",                               displayName = "Uranium",                volume = 0.052f,    sortId = "ingot", minAmount = 2000    },

            //Modded Ingot Items
            //None!

            //Component items (alphabetical by displayName)
            new CargoItemDefinition { typeId = "Component",    subtypeId = "BulletproofGlass",                      displayName = "Bulletproof Glass",      volume = 8.0f,     sortId = "component", minAmount = 12000   },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "Canvas",                                displayName = "Canvas",                 volume = 8.0f,      sortId = "component", minAmount = 300     },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "Computer",                              displayName = "Computer",               volume = 1.0f,      sortId = "component", minAmount = 6500    },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "Construction",                          displayName = "Construction",           volume = 2.0f,      sortId = "component", minAmount = 50000   },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "Detector",                              displayName = "Detector Compnts",       volume = 6.0f,      sortId = "component", minAmount = 400     },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "Display",                               displayName = "Displays",               volume = 6.0f,      sortId = "component", minAmount = 500     },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "EngineerPlushie",                       displayName = "Engineer Plushie",       volume = 3.0f,      sortId = "component", minAmount = 1       },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "Explosives",                            displayName = "Explosives",             volume = 2.0f,      sortId = "component", minAmount = 500     },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "Girder",                                displayName = "Girder",                 volume = 2.0f,      sortId = "component", minAmount = 3500    },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "GravityGenerator",                      displayName = "Gravity Generators",     volume = 200.0f,    sortId = "component", minAmount = 250     },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "InteriorPlate",                         displayName = "Interior Plates",        volume = 5.0f,      sortId = "component", minAmount = 55000   },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "LargeTube",                             displayName = "Large Steeltubes",       volume = 38.0f,     sortId = "component", minAmount = 6000    },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "Medical",                               displayName = "Medical Compnts",        volume = 160.0f,    sortId = "component", minAmount = 120     },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "MetalGrid",                             displayName = "Metalgrids",             volume = 15.0f,     sortId = "component", minAmount = 15500   },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "Motor",                                 displayName = "Motors",                 volume = 8.0f,      sortId = "component", minAmount = 16000   },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "PowerCell",                             displayName = "Powercells",             volume = 40.0f,     sortId = "component", minAmount = 2800    },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "RadioCommunication",                    displayName = "Radio Comms.",           volume = 70.0f,     sortId = "component", minAmount = 250     },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "Reactor",                               displayName = "Reactor Compnts",        volume = 8.0f,      sortId = "component", minAmount = 10000   },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "SabiroidPlushie",                       displayName = "Sabiroid Plushie",       volume = 3.0f,      sortId = "component", minAmount = 1       },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "SmallTube",                             displayName = "Small Steel Tubes",      volume = 2.0f,      sortId = "component", minAmount = 26000   },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "SolarCell",                             displayName = "Solar Cells",            volume = 12.0f,     sortId = "component", minAmount = 2800    },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "SteelPlate",                            displayName = "Steelplates",            volume = 3.0f,      sortId = "component", minAmount = 300000  },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "Superconductor",                        displayName = "Super Conductors",       volume = 8.0f,      sortId = "component", minAmount = 3000    },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "Thrust",                                displayName = "Thruster Compnts",       volume = 10.0f,     sortId = "component", minAmount = 16000   },

            // Prototech component items (alphabetical by displayName)
            new CargoItemDefinition { typeId = "Component",    subtypeId = "PrototechCapacitor",                    displayName = "Prototech Capacitor",    volume = 50.0f,     sortId = "protoComponent", minAmount = 20       },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "PrototechCircuitry",                    displayName = "Prototech Circuitry",    volume = 20.0f,     sortId = "protoComponent", minAmount = 20       },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "PrototechCoolingUnit",                  displayName = "Prototech Cooling Unit",  volume = 80.0f,     sortId = "protoComponent", minAmount = 20       },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "PrototechFrame",                        displayName = "Prototech Frame",        volume = 50.0f,     sortId = "protoComponent", minAmount = 20       },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "PrototechMachinery",                    displayName = "Prototech Machinery",    volume = 35.0f,     sortId = "protoComponent", minAmount = 20       },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "PrototechPanel",                        displayName = "Prototech Panel",        volume = 6.0f,      sortId = "protoComponent", minAmount = 20       },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "PrototechPropulsionUnit",               displayName = "Prototech Propulsion",   volume = 160.0f,    sortId = "protoComponent", minAmount = 20       },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "AQD_Comp_Concrete",                     displayName = "Concrete",              volume = 0.37f,     sortId = "component", minAmount = 10000   },
           
            //Modded Component Items (alphabetical by displayName)
            new CargoItemDefinition { typeId = "Component",    subtypeId = "EmptyTinCan",                           displayName = "Empty Tin Can",         volume = 0.5f,      sortId = "component", minAmount =   0     },
           
           //Ammo items (alphabetical by displayName)
           new CargoItemDefinition { typeId = "AmmoMagazine", subtypeId = "LargeCalibreAmmo",                      displayName = "Artillery Shell",        volume = 100.0f,    sortId = "ammo", minAmount = 250     },
            new CargoItemDefinition { typeId = "AmmoMagazine", subtypeId = "MediumCalibreAmmo",                     displayName = "Assault Cannon Shell",   volume = 30.0f,     sortId = "ammo", minAmount = 500    },
            new CargoItemDefinition { typeId = "AmmoMagazine", subtypeId = "AutocannonClip",                        displayName = "Autocannon Mag",         volume = 24.0f,     sortId = "ammo", minAmount = 500     },
            new CargoItemDefinition { typeId = "AmmoMagazine", subtypeId = "FireworksBoxBlue",                      displayName = "Fireworks Box Blue",     volume = 6.0f,      sortId = "ammo", minAmount = 0      },
            new CargoItemDefinition { typeId = "AmmoMagazine", subtypeId = "FireworksBoxGreen",                     displayName = "Fireworks Box Green",    volume = 6.0f,      sortId = "ammo", minAmount = 0      },
            new CargoItemDefinition { typeId = "AmmoMagazine", subtypeId = "FireworksBoxPink",                      displayName = "Fireworks Box Pink",     volume = 6.0f,      sortId = "ammo", minAmount = 0      },
            new CargoItemDefinition { typeId = "AmmoMagazine", subtypeId = "FireworksBoxRainbow",                   displayName = "Fireworks Box Rainbow",  volume = 6.0f,      sortId = "ammo", minAmount = 0      },
            new CargoItemDefinition { typeId = "AmmoMagazine", subtypeId = "FireworksBoxRed",                       displayName = "Fireworks Box Red",      volume = 6.0f,      sortId = "ammo", minAmount = 0      },
            new CargoItemDefinition { typeId = "AmmoMagazine", subtypeId = "FireworksBoxYellow",                    displayName = "Fireworks Box Yellow",   volume = 6.0f,      sortId = "ammo", minAmount = 0      },
            new CargoItemDefinition { typeId = "AmmoMagazine", subtypeId = "FlareClip",                             displayName = "Flare Gun Clip",         volume = 0.05f,     sortId = "ammo", minAmount = 50      },
            new CargoItemDefinition { typeId = "AmmoMagazine", subtypeId = "NATO_25x184mm",                         displayName = "Gatling Ammo Box",       volume = 16.0f,     sortId = "ammo", minAmount = 500     },
            new CargoItemDefinition { typeId = "AmmoMagazine", subtypeId = "LargeRailgunAmmo",                      displayName = "Large Railgun Sabot",    volume = 40.0f,     sortId = "ammo", minAmount = 250     },
            new CargoItemDefinition { typeId = "AmmoMagazine", subtypeId = "Missile200mm",                          displayName = "Rocket",                 volume = 60.0f,     sortId = "ammo", minAmount = 500     },
            new CargoItemDefinition { typeId = "AmmoMagazine", subtypeId = "SmallRailgunAmmo",                      displayName = "Small Railgun Sabot",    volume = 8.0f,      sortId = "ammo", minAmount = 250     },
            new CargoItemDefinition { typeId = "AmmoMagazine", subtypeId = "PaintGunMag",                           displayName = "Paint Chemicals",        volume = 0.002f,    sortId = "ammo", minAmount = 5000    },

           /*
                       //Modded Ammo Items (alphabetical by displayName)
                       new CargoItemDefinition { typeId = "AmmoMagazine", subtypeId = "ElindisGaussAmmo",                      displayName = "Coilgun Box",            volume = 30.0f,     sortId = "ammo", minAmount = 500     },
                       new CargoItemDefinition { typeId = "AmmoMagazine", subtypeId = "HailstormMissile",                      displayName = "Hailstorm Rocket",       volume = 38.0f,     sortId = "ammo", minAmount = 250     },
                       new CargoItemDefinition { typeId = "AmmoMagazine", subtypeId = "ElindisTorpedo",                        displayName = "Torpedo",                volume = 120.0f,    sortId = "ammo", minAmount =  50     },
           */
           //Hand weapon ammo items (alphabetical by displayName)
            new CargoItemDefinition { typeId = "AmmoMagazine", subtypeId = "NATO_5p56x45mm",                        displayName = "5.56x45mm Mag",          volume = 0.2f,      sortId = "handAmmo", minAmount = 100     },
            new CargoItemDefinition { typeId = "AmmoMagazine", subtypeId = "AutomaticRifleGun_Mag_20rd",            displayName = "MR-20 Rifle Mag",        volume = 0.2f,      sortId = "handAmmo", minAmount = 100    },
            new CargoItemDefinition { typeId = "AmmoMagazine", subtypeId = "UltimateAutomaticRifleGun_Mag_30rd",    displayName = "MR-30E Rifle Mag",       volume = 0.3f,      sortId = "handAmmo", minAmount = 100    },
            new CargoItemDefinition { typeId = "AmmoMagazine", subtypeId = "RapidFireAutomaticRifleGun_Mag_50rd",   displayName = "MR-50A Rifle Mag",       volume = 0.5f,      sortId = "handAmmo", minAmount = 100    },
            new CargoItemDefinition { typeId = "AmmoMagazine", subtypeId = "PreciseAutomaticRifleGun_Mag_5rd",      displayName = "MR-8P Rifle Mag",        volume = 0.15f,     sortId = "handAmmo", minAmount = 100    },
            new CargoItemDefinition { typeId = "AmmoMagazine", subtypeId = "SemiAutoPistolMagazine",                displayName = "S-10 Pistol Mag",        volume = 0.1f,      sortId = "handAmmo", minAmount = 100    },
            new CargoItemDefinition { typeId = "AmmoMagazine", subtypeId = "ElitePistolMagazine",                   displayName = "S-10E Pistol Mag",       volume = 0.1f,      sortId = "handAmmo", minAmount = 100    },
            new CargoItemDefinition { typeId = "AmmoMagazine", subtypeId = "FullAutoPistolMagazine",                displayName = "S-20A Pistol Mag",       volume = 0.15f,     sortId = "handAmmo", minAmount = 100    },

            //Tools Items (alphabetical by displayName)
            new CargoItemDefinition { typeId = "PhysicalGunObject", subtypeId = "AngleGrinder4Item",                displayName = "Elite Grinder",          volume = 20.0f,     sortId = "tool", minAmount =  10     },
            new CargoItemDefinition { typeId = "PhysicalGunObject", subtypeId = "HandDrill4Item",                   displayName = "Elite Hand Drill",       volume = 25.0f,     sortId = "tool", minAmount =  10     },
            new CargoItemDefinition { typeId = "PhysicalGunObject", subtypeId = "Welder4Item",                      displayName = "Elite Welder",           volume = 8.0f,      sortId = "tool", minAmount =  10     },
            new CargoItemDefinition { typeId = "PhysicalGunObject", subtypeId = "AngleGrinder2Item",                displayName = "Enhanced Grinder",       volume = 20.0f,     sortId = "tool", minAmount =  10     },
            new CargoItemDefinition { typeId = "PhysicalGunObject", subtypeId = "HandDrill2Item",                   displayName = "Enhanced Hand Drill",    volume = 25.0f,     sortId = "tool", minAmount =  10     },
            new CargoItemDefinition { typeId = "PhysicalGunObject", subtypeId = "Welder2Item",                      displayName = "Enhanced Welder",        volume = 8.0f,      sortId = "tool", minAmount =  10     },
            new CargoItemDefinition { typeId = "PhysicalGunObject", subtypeId = "FlareGunItem",                     displayName = "Flare Gun",              volume = 6.0f,      sortId = "tool", minAmount =  10     },
            new CargoItemDefinition { typeId = "PhysicalGunObject", subtypeId = "AngleGrinderItem",                 displayName = "Grinder",                volume = 20.0f,     sortId = "tool", minAmount =  10     },
            new CargoItemDefinition { typeId = "PhysicalGunObject", subtypeId = "HandDrillItem",                    displayName = "Hand Drill",             volume = 25.0f,     sortId = "tool", minAmount =  10     },
            new CargoItemDefinition { typeId = "PhysicalGunObject", subtypeId = "AngleGrinder3Item",                displayName = "Proficient Grinder",     volume = 20.0f,     sortId = "tool", minAmount =  10     },
            new CargoItemDefinition { typeId = "PhysicalGunObject", subtypeId = "HandDrill3Item",                   displayName = "Proficient Hand Drill",  volume = 25.0f,     sortId = "tool", minAmount =  10     },
            new CargoItemDefinition { typeId = "PhysicalGunObject", subtypeId = "Welder3Item",                      displayName = "Proficient Welder",      volume = 8.0f,      sortId = "tool", minAmount =  10     },
            new CargoItemDefinition { typeId = "PhysicalGunObject", subtypeId = "WelderItem",                       displayName = "Welder",                 volume = 8.0f,      sortId = "tool", minAmount =  10     },
            new CargoItemDefinition { typeId = "PhysicalGunObject", subtypeId = "PhysicalPaintGun",                 displayName = "Paint Gun",              volume = 2.0f,      sortId = "tool", minAmount =  10     },


            //Rifle Items (alphabetical by displayName)
            new CargoItemDefinition { typeId = "PhysicalGunObject", subtypeId = "AutomaticRifleItem",               displayName = "MR-20 Rifle",            volume = 20.0f,     sortId = "rifle", minAmount =  10     },
            new CargoItemDefinition { typeId = "PhysicalGunObject", subtypeId = "UltimateAutomaticRifleItem",       displayName = "MR-30E Rifle",           volume = 20.0f,     sortId = "rifle", minAmount =  10     },
            new CargoItemDefinition { typeId = "PhysicalGunObject", subtypeId = "RapidFireAutomaticRifleItem",      displayName = "MR-50A Rifle",           volume = 20.0f,     sortId = "rifle", minAmount =  10     },
            new CargoItemDefinition { typeId = "PhysicalGunObject", subtypeId = "PreciseAutomaticRifleItem",        displayName = "MR-8P Rifle",            volume = 20.0f,     sortId = "rifle", minAmount =  10     },

            //Pistol Items (alphabetical by displayName)
            new CargoItemDefinition { typeId = "PhysicalGunObject", subtypeId = "SemiAutoPistolItem",               displayName = "S-10 Pistol",            volume =  6.0f,     sortId = "pistol", minAmount =  10     },
            new CargoItemDefinition { typeId = "PhysicalGunObject", subtypeId = "ElitePistolItem",                  displayName = "S-10E Pistol",           volume =  6.0f,     sortId = "pistol", minAmount =  10     },
            new CargoItemDefinition { typeId = "PhysicalGunObject", subtypeId = "FullAutoPistolItem",               displayName = "S-20A Pistol",           volume =  8.0f,     sortId = "pistol", minAmount =  10     },

            //Rocket Launcher Items
            new CargoItemDefinition { typeId = "PhysicalGunObject", subtypeId = "AdvancedHandHeldLauncherItem",     displayName = "Advanced Launcher",      volume = 125.0f,    sortId = "launcher", minAmount =  10     },
            new CargoItemDefinition { typeId = "PhysicalGunObject", subtypeId = "BasicHandHeldLauncherItem",        displayName = "R-01 Rocket Launcher",   volume = 125.0f,    sortId = "launcher", minAmount =  10     },

            //Kit Items (alphabetical by displayName)
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "Medkit",                           displayName = "Medkits",                volume = 12.0f,     sortId = "kit", minAmount =  30     },
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "Powerkit",                         displayName = "Powerkits",              volume = 9.0f,      sortId = "kit", minAmount =  30     },
            new CargoItemDefinition { typeId = "ConsumableItem",   subtypeId = "RadiationKit",                      displayName = "Radiation Kit",          volume = 8.0f,      sortId = "kit", minAmount =   30     },

            //Misc Items (alphabetical by displayName)
            new CargoItemDefinition { typeId = "Datapad",           subtypeId = "Datapad",                          displayName = "Datapads",               volume = 0.4f,      sortId = "", minAmount =   0     },
            new CargoItemDefinition { typeId = "Package",           subtypeId = "Package",                          displayName = "Packages",               volume = 125.0f,    sortId = "", minAmount =   0     },
            new CargoItemDefinition { typeId = "PhysicalObject",    subtypeId = "SpaceCredit",                      displayName = "Space Credits",          volume = 0.001f,    sortId = "", minAmount =   0     },
            new CargoItemDefinition { typeId = "Component",         subtypeId = "ZoneChip",                         displayName = "Zone Chips",             volume = 0.2f,      sortId = "", minAmount = 100     },

            //Bottle Items (alphabetical by displayName)
            new CargoItemDefinition { typeId = "GasContainerObject",    subtypeId = "HydrogenBottle",               displayName = "Hydrogen Bottle",        volume = 120.0f,    sortId = "bottle", minAmount =  10     },
            new CargoItemDefinition { typeId = "OxygenContainerObject", subtypeId = "OxygenBottle",                 displayName = "Oxygen Bottle",          volume = 120.0f,    sortId = "bottle", minAmount =  10     },

            // Raw Food items (alphabetical by displayName)
            new CargoItemDefinition { typeId = "PhysicalObject",    subtypeId = "Algae",                            displayName = "Algae",                  volume = 5.0f,       sortId = "rawFood", minAmount =  50    },
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "Fruit",                            displayName = "Fruit",                  volume = 3.0f,       sortId = "rawFood", minAmount =  50     },
            new CargoItemDefinition { typeId = "PhysicalObject",    subtypeId = "Grain",                            displayName = "Grain",                  volume = 2.0f,       sortId = "rawFood", minAmount =  50     },
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "Mushrooms",                        displayName = "Mushrooms",              volume = 8.0f,      sortId = "rawFood", minAmount =  50     },
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "InsectMeatRaw",                    displayName = "Raw Insect Meat",        volume = 2.0f,       sortId = "rawFood", minAmount =  10     },
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "MammalMeatRaw",                    displayName = "Raw Mammal Meat",        volume = 1.15f,      sortId = "rawFood", minAmount =  10     },
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "Vegetables",                       displayName = "Vegetables",             volume = 2.5f,      sortId = "rawFood", minAmount =  50     },

            // Cooked Food Items (alphabetical by displayName)
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "MealPack_BananaBeef",              displayName = "Banana Beef",            volume = 1.25f,     sortId = "cookedFood", minAmount =   20    },
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "MealPack_Burrito",                 displayName = "Burrito",                volume = 1.25f,     sortId = "cookedFood", minAmount =  20     },
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "MealPack_Chili",                   displayName = "Chili",                  volume = 1.25f,     sortId = "cookedFood", minAmount =  20     },
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "MealPack_ClangCrunchies",          displayName = "Clang Crunchies",        volume = 1.25f,     sortId = "cookedFood", minAmount =   20    },
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "InsectMeatCooked",                 displayName = "Cooked Insect Meat",     volume = 1.4f,      sortId = "cookedFood", minAmount =  50     },
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "MammalMeatCooked",                 displayName = "Cooked Mammal Meat",     volume = 0.875f,    sortId = "cookedFood", minAmount =  50     },
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "MealPack_Curry",                   displayName = "Curry",                  volume = 1.25f,     sortId = "cookedFood", minAmount =  20     },
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "MealPack_Dumplings",               displayName = "Dumplings",              volume = 1.25f,     sortId = "cookedFood", minAmount =  20     },
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "MealPack_ExpiredSlop",             displayName = "Expired Slop",           volume = 1.25f,     sortId = "cookedFood", minAmount =   0     },
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "MealPack_Flatbread",               displayName = "Flatbread",              volume = 1.25f,     sortId = "cookedFood", minAmount =  20     },
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "MealPack_FoodPaste",               displayName = "Food Paste",             volume = 1.25f,     sortId = "cookedFood", minAmount =   20    },
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "MealPack_FrontierStew",            displayName = "Frontier Stew",          volume = 1.25f,     sortId = "cookedFood", minAmount =  20     },
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "MealPack_FruitBar",                displayName = "Fruit Bar",              volume = 1.25f,     sortId = "cookedFood", minAmount =  20     },
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "MealPack_FruitPastry",             displayName = "Fruit Pastry",           volume = 1.25f,     sortId = "cookedFood", minAmount =  20     },
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "MealPack_GardenSlaw",              displayName = "Garden Slaw",            volume = 1.25f,     sortId = "cookedFood", minAmount =  20     },
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "MealPack_GreenPellets",            displayName = "Green Pellets",          volume = 1.25f,     sortId = "cookedFood", minAmount =  20     },
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "MealPack_Hardtack",                displayName = "Hardtack",               volume = 1.25f,     sortId = "cookedFood", minAmount =  20     },
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "MealPack_InsectMedley",            displayName = "Insect Medley",          volume = 1.25f,     sortId = "cookedFood", minAmount =  20     },
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "MealPack_KelpCrisp",               displayName = "Kelp Crisp",             volume = 1.25f,     sortId = "cookedFood", minAmount =  20     },
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "MealPack_Lasagna",                 displayName = "Lasagna",                volume = 1.25f,     sortId = "cookedFood", minAmount =  20     },
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "MealPack_MammalMince",             displayName = "Mammal Mince Pack",      volume = 1.25f,     sortId = "cookedFood", minAmount =  20     },
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "MealPack_Ramen",                   displayName = "Ramen",                  volume = 1.25f,     sortId = "cookedFood", minAmount =  20     },
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "MealPack_RedPellets",              displayName = "Red Pellets",            volume = 1.25f,     sortId = "cookedFood", minAmount =  20     },
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "MealPack_SabiroidSausage",         displayName = "Sabiroid Sausage Pack",  volume = 1.25f,     sortId = "cookedFood", minAmount =  20     },
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "MealPack_SearedSabiroid",          displayName = "Seared Sabiroid",        volume = 1.25f,     sortId = "cookedFood", minAmount =  20     },
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "MealPack_Spaghetti",               displayName = "Spaghetti",              volume = 1.25f,     sortId = "cookedFood", minAmount =  20     },
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "MealPack_SteakDinner",             displayName = "Steak Dinner",           volume = 1.25f,     sortId = "cookedFood", minAmount =  20     },
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "MealPack_SynthLoaf",               displayName = "Synth Loaf",            volume = 1.25f,     sortId = "cookedFood", minAmount =  20     },
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "MealPack_Unknown",                 displayName = "Unknown MealPack",      volume = 1.25f,     sortId = "cookedFood", minAmount =  0      },
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "MealPack_VeggieBurger",            displayName = "Veggie Burger",          volume = 1.25f,     sortId = "cookedFood", minAmount =  20     },

            //Modded Cooked Food Items (alphabetical by displayName)
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "BioPaste",                         displayName = "Bio-Nutri-Paste",             volume = 0.5f,       sortId = "cookedFood", minAmount =  50     },
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "LaysChips",                        displayName = "Lies Chips",            volume = 1.0f,       sortId = "cookedFood", minAmount =  20     },
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "PrlnglesChips",                    displayName = "Singles Chips",        volume = 1.0f,       sortId = "cookedFood", minAmount =  20     },
            //Drink Items (alphabetical by displayName)
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "ClangCola",                        displayName = "Clang Cola",             volume = 1.0f,      sortId = "drink", minAmount =   0     },
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "CosmicCoffee",                     displayName = "Cosmic Coffee",          volume = 1.0f,      sortId = "drink", minAmount =   0     },

            //Modded Drink Items (alphabetical by displayName)
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "FruitTea",                         displayName = "Fruit Tea",             volume = 0.5f,       sortId = "drink", minAmount =  20     },
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "MycoBoost",                        displayName = "Myco-Boost",            volume = 0.5f,       sortId = "drink", minAmount =  20     },
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "SparklingWater",                   displayName = "Sparkling Water",       volume = 0.5f,       sortId = "drink", minAmount =  20     },
            //Seed items (alphabetical by displayName)
            new CargoItemDefinition { typeId = "SeedItem",          subtypeId = "Fruit",                            displayName = "Fruit Seeds",            volume = 0.1f,      sortId = "seed", minAmount =  50     },
            new CargoItemDefinition { typeId = "SeedItem",          subtypeId = "Grain",                            displayName = "Grain Seeds",            volume = 0.1f,      sortId = "seed", minAmount =  50     },
            new CargoItemDefinition { typeId = "SeedItem",          subtypeId = "Vegetables",                       displayName = "Vegetable Seeds",        volume = 0.1f,      sortId = "seed", minAmount =  50     },
            new CargoItemDefinition { typeId = "SeedItem",          subtypeId = "Mushrooms",                        displayName = "Mushroom Spores",        volume = 0.1f,      sortId = "seed", minAmount =  50     },

            
        };

        // Static constructor: normalize blank sortIds to default category 'misc'
        static MahDefinitions()
        {
            foreach (var def in cargoItemDefinitions)
            {
                if (def != null && string.IsNullOrWhiteSpace(def.sortId))
                    def.sortId = "misc";
            }
        }

        // Returns cargo item definitions ordered by category (sortId) then display name
        public static IEnumerable<CargoItemDefinition> OrderedCargoItems(IEnumerable<CargoItemDefinition> defs)
        {
            if (defs == null) yield break;
            foreach (var d in defs.Where(d => d != null).OrderBy(d => d.sortId).ThenBy(d => d.displayName))
                yield return d;
        }

        // Public entry point (idempotent) to load any external user-provided item definitions
        public static void LoadExternalItems()
        {
            if (externalItemsLoaded) return; // already done (success or exhausted attempts)

            externalItemsLoadAttempts++;

            try
            {
                bool loaded = false;
                // Try both root and Data/ locations (one may exist depending on packaging)
                if (TryLoadExternalItems(ExternalItemsFileName))
                    loaded = true;
                else if (TryLoadExternalItems("Data/" + ExternalItemsFileName))
                    loaded = true;

                if (loaded)
                {
                    externalItemsLoaded = true; // success
                    MyLog.Default.WriteLine("MahDefinitions: ExternalItems loaded successfully.");
                }
                else if (externalItemsLoadAttempts >= ExternalItemsMaxAttempts)
                {
                    // Give up to avoid spamming attempts forever
                    externalItemsLoaded = true; // mark complete (no file or not yet accessible)
                    if (externalItemsLoadAttempts > 1)
                        MyLog.Default.WriteLine("MahDefinitions: ExternalItems load attempts exhausted; no file loaded.");
                }
            }
            catch (Exception e)
            {
                externalItemsLoaded = true; // avoid infinite loop on persistent exception
                MyLog.Default.WriteLine($"MahDefinitions.LoadExternalItems Exception: {e}");
            }
        }

        static bool TryLoadExternalItems(string relativePath)
        {
            try
            {
                // Some dedicated server environments may not allow this early; guard for null
                if (MyAPIGateway.Utilities == null)
                    return false;

                EnsureSelfModItem();
                if (!selfModItem.HasValue)
                    return false; // can't resolve mod item yet

                if (!MyAPIGateway.Utilities.FileExistsInModLocation(relativePath, selfModItem.Value))
                    return false;

                using (var reader = MyAPIGateway.Utilities.ReadFileInModLocation(relativePath, selfModItem.Value))
                {
                    if (reader == null) return false;

                    string line;
                    int lineNo = 0;
                    string currentSection = null;
                    string typeId = null, subtypeId = null, displayName = null, sortId = null;
                    float volume = 0.1f;
                    int minAmount = 0;
                    
                    while ((line = reader.ReadLine()) != null)
                    {
                        lineNo++;
                        line = line.Trim();
                        if (string.IsNullOrWhiteSpace(line)) continue;              // skip blank
                        if (line.StartsWith("#")) continue;                        // skip comments
                        if (line.StartsWith("//")) continue;                      // alternate comment

                        // Check for INI-style section header [ItemName]
                        if (line.StartsWith("[") && line.EndsWith("]"))
                        {
                            // Save previous item if we have one
                            if (currentSection != null && !string.IsNullOrWhiteSpace(typeId) && !string.IsNullOrWhiteSpace(subtypeId))
                            {
                                AddOrUpdateCargoItem(typeId, subtypeId, displayName ?? subtypeId, volume, sortId, minAmount);
                            }
                            
                            // Start new section
                            currentSection = line.Substring(1, line.Length - 2).Trim();
                            typeId = null;
                            subtypeId = null;
                            displayName = null;
                            sortId = null;
                            volume = 0.1f;
                            minAmount = 0;
                            continue;
                        }

                        // Check for key=value format (INI style)
                        if (line.Contains("="))
                        {
                            var parts = line.Split(new char[] { '=' }, 2);
                            if (parts.Length == 2)
                            {
                                string key = parts[0].Trim().ToLowerInvariant();
                                string value = parts[1].Trim();
                                
                                switch (key)
                                {
                                    case "typeid":
                                        typeId = value;
                                        break;
                                    case "subtypeid":
                                        subtypeId = value;
                                        break;
                                    case "displayname":
                                        displayName = value;
                                        break;
                                    case "volume":
                                        float.TryParse(value, out volume);
                                        break;
                                    case "sortid":
                                        sortId = value.ToLower();
                                        break;
                                    case "minamount":
                                        int.TryParse(value, out minAmount);
                                        break;
                                }
                                continue;
                            }
                        }

                        // Fall back to CSV / semicolon format: typeId;subtypeId;displayName;volume;sortId;minAmount
                        var csvParts = line.Split(new char[] { ';', ',' });
                        if (csvParts.Length < 2)
                        {
                            MyLog.Default.WriteLine($"MahDefinitions: Skipping line {lineNo} (invalid format): {line}");
                            continue;
                        }

                        typeId = csvParts[0].Trim();
                        subtypeId = csvParts[1].Trim();
                        if (string.IsNullOrWhiteSpace(typeId) || string.IsNullOrWhiteSpace(subtypeId))
                            continue;

                        displayName = csvParts.Length > 2 && !string.IsNullOrWhiteSpace(csvParts[2]) ? csvParts[2].Trim() : subtypeId;
                        volume = 0.1f;
                        if (csvParts.Length > 3)
                            float.TryParse(csvParts[3].Trim(), out volume);
                        sortId = csvParts.Length > 4 && !string.IsNullOrWhiteSpace(csvParts[4]) ? csvParts[4].Trim().ToLower() : "misc";
                        minAmount = 0;
                        if (csvParts.Length > 5)
                            int.TryParse(csvParts[5].Trim(), out minAmount);

                        // Add CSV item immediately
                        AddOrUpdateCargoItem(typeId, subtypeId, displayName, volume, sortId, minAmount);
                        
                        // Reset for next CSV line
                        typeId = null;
                        subtypeId = null;
                    }
                    
                    // Save final INI-style item if we have one
                    if (currentSection != null && !string.IsNullOrWhiteSpace(typeId) && !string.IsNullOrWhiteSpace(subtypeId))
                    {
                        AddOrUpdateCargoItem(typeId, subtypeId, displayName ?? subtypeId, volume, sortId, minAmount);
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahDefinitions.TryLoadExternalItems('{relativePath}') Exception: {e}");
                return false;
            }
        }

        static void AddOrUpdateCargoItem(string typeId, string subtypeId, string displayName, float volume, string sortId, int minAmount)
        {
            if (string.IsNullOrWhiteSpace(typeId) || string.IsNullOrWhiteSpace(subtypeId))
                return;

            // Prevent duplicates; update existing instead of adding new
            var existing = cargoItemDefinitions.FirstOrDefault(d => d.typeId == typeId && d.subtypeId == subtypeId);
            if (existing != null)
            {
                existing.displayName = displayName;
                existing.volume = volume;
                existing.sortId = string.IsNullOrWhiteSpace(sortId) ? existing.sortId : sortId;
                if (minAmount > 0) existing.minAmount = minAmount;
                return;
            }

            cargoItemDefinitions.Add(new CargoItemDefinition
            {
                typeId = typeId,
                subtypeId = subtypeId,
                displayName = displayName,
                volume = volume,
                sortId = string.IsNullOrWhiteSpace(sortId) ? "misc" : sortId,
                minAmount = minAmount
            });
        }

        public static CargoItemDefinition GetDefinition(string typeId, string subtypeId)
        {
            foreach(CargoItemDefinition definition in MahDefinitions.cargoItemDefinitions)
            {
                if (definition.typeId != typeId) continue;
                if (subtypeId != definition.subtypeId && !subtypeId.Contains(definition.subtypeId)) continue;

                return definition;
            }

            return null;
        }

        public static string WattFormat(double num)
        {
            // Values from power production blocks come in MW / MWh so we need to first get W
            num *= 1000000;

            if (num >= 100000000)
                return (num / 1000000).ToString("#,0 MW");

            if (num >= 10000000)
                return (num / 1000000).ToString("0.# MW");

            if (num >= 100000)
                return (num / 1000).ToString("#,0 kW");

            if (num >= 10000)
                return (num / 1000).ToString("0.# kW");

            return num.ToString("#,0 W");
        }

        public static string KiloFormat(double num)
        {
            if (num >= 100000000)
                return (num / 1000000).ToString("#,0 M");

            if (num >= 10000000)
                return (num / 1000000).ToString("0.# M");

            if (num >= 100000)
                return (num / 1000).ToString("#,0 K");

            if (num >= 10000)
                return (num / 1000).ToString("0.# K");

            return num.ToString("#,0");
        }

        public static string LiterFormat(double num)
        {
            if (num >= 100000)
                return (num / 1000).ToString("0.0 kL");

            if (num >= 10000)
                return (num / 1000).ToString("0.00 kL");

            return num.ToString("0.00 L");
        }

        public static string TimeFormat (int num)
        {
            TimeSpan t = TimeSpan.FromSeconds(num);

            return string.Format("{0:D1}h {1:D2}m",
                            t.Hours,
                            t.Minutes);

            /*
            return string.Format("{0:D2}h:{1:D2}m:{2:D2}s:{3:D3}ms",
                            t.Hours,
                            t.Minutes,
                            t.Seconds,
                            t.Milliseconds);
            */
        }

        public static string CurrentTimeStamp
        {
            get
            {
                System.DateTime moment = DateTime.Now;
                int hours = moment.Hour;
                int minutes = moment.Minute;
                int seconds = moment.Second;

                return $"{hours.ToString("#00").Replace("1", " 1")}:{minutes.ToString("#00").Replace("1", " 1")}:{seconds.ToString("#00").Replace("1", " 1")}";
            }
        }
    }

    public static class MahUtillities
    {
        public static IMySlimBlock GetSlimblock(IMyTerminalBlock block) => (block.CubeGrid as MyCubeGrid).GetCubeBlock(block.Position);

        public static float GetMaxOutput(List<IMyPowerProducer> powerProducers)
        {
            List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
            List<IMyPowerProducer> windTurbines = new List<IMyPowerProducer>();
            List<IMyPowerProducer> hydrogenEngines = new List<IMyPowerProducer>();
            List<IMySolarPanel> solarPanels = new List<IMySolarPanel>();
            List<IMyReactor> reactors = new List<IMyReactor>();

            foreach(IMyPowerProducer block in powerProducers)
            {
                if (block is IMyBatteryBlock)
                {
                    batteries.Add((IMyBatteryBlock)block);
                }
                else if (((MyCubeBlock)block).BlockDefinition.Id.SubtypeName.Contains("Wind"))
                {
                    windTurbines.Add(block);
                }
                else if (((MyCubeBlock)block).BlockDefinition.Id.SubtypeName.Contains("Hydrogen"))
                {
                    hydrogenEngines.Add(block);
                }
                else if (block is IMyReactor)
                {
                    reactors.Add((IMyReactor)block);
                }
                else if (block is IMySolarPanel)
                {
                    solarPanels.Add((IMySolarPanel)block);
                }
            }

            float value = windTurbines.Sum(block => block.MaxOutput);
            value += solarPanels.Sum(block => block.MaxOutput);
            value += hydrogenEngines.Sum(block => block.MaxOutput);
            value += batteries.Sum(block => block.MaxOutput);
            value += reactors.Sum(block => block.MaxOutput);

            return value;
        }

        public static float GetCurrentOutput(List<IMyPowerProducer> powerProducers)
        {
            List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
            List<IMyPowerProducer> windTurbines = new List<IMyPowerProducer>();
            List<IMyPowerProducer> hydrogenEngines = new List<IMyPowerProducer>();
            List<IMySolarPanel> solarPanels = new List<IMySolarPanel>();
            List<IMyReactor> reactors = new List<IMyReactor>();

            foreach (IMyPowerProducer block in powerProducers)
            {
                if (block is IMyBatteryBlock)
                {
                    batteries.Add((IMyBatteryBlock)block);
                }
                else if (((MyCubeBlock)block).BlockDefinition.Id.SubtypeName.Contains("Wind"))
                {
                    windTurbines.Add(block);
                }
                else if (((MyCubeBlock)block).BlockDefinition.Id.SubtypeName.Contains("Hydrogen"))
                {
                    hydrogenEngines.Add(block);
                }
                else if (block is IMyReactor)
                {
                    reactors.Add((IMyReactor)block);
                }
                else if (block is IMySolarPanel)
                {
                    solarPanels.Add((IMySolarPanel)block);
                }
            }

            float value = windTurbines.Sum(block => block.CurrentOutput);
            value += solarPanels.Sum(block => block.CurrentOutput);
            value += hydrogenEngines.Sum(block => block.CurrentOutput);
            value += batteries.Sum(block => block.CurrentOutput);
            value += reactors.Sum(block => block.CurrentOutput);
            value -= batteries.Sum(block => block.CurrentInput);

            return value;
        }

        public static float GetPowerTimeLeft(List<IMyPowerProducer> powerProducers)
        {
            var timeRemaining = 0.0f;

            try
            {
                List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
                List<IMyReactor> reactors = new List<IMyReactor>();

                foreach (var block in powerProducers)
                {
                    if (block is IMyBatteryBlock)
                        batteries.Add((IMyBatteryBlock)block);
                    else if (block is IMyReactor)
                        reactors.Add((IMyReactor)block);
                }

                // Calculate time left depending on stored Power in batteries
                if (batteries.Count > 0)
                {
                    var currentBatteryInput = batteries.Sum(block => block.CurrentInput);
                    var currentBatteryOutput = batteries.Sum(block => block.CurrentOutput);
                    var currentStoredPower = batteries.Sum(block => block.CurrentStoredPower);
                    var maximumStoredPower = batteries.Sum(block => block.MaxStoredPower);
                    // Only take battery input into account, when actually loading, not when hopping forward and back <2% close to maxStorage to minimize output stutter.
                    var absoluteBatteryOutput = currentBatteryOutput - (currentStoredPower / maximumStoredPower > 0.98 ? 0 : currentBatteryInput);

                    if (absoluteBatteryOutput > 0)
                    {
                        timeRemaining += (currentStoredPower / absoluteBatteryOutput) * 3600;
                    }
                }
                // If there are no batteries on this grid, try to get a rough estimation from the installed reactors or engines
                else
                {
                    // Reactors & Uranium are only used, if there are no batteries
                    if (reactors.Count > 0)
                    {
                        var currentReactorOutput = reactors.Sum(block => block.CurrentOutput);
                        var currentReactorMaxOutput = reactors.Sum(block => block.MaxOutput);

                        if (currentReactorOutput > 0)
                        {
                            timeRemaining = (MahUtillities.GetItemAmountFromBlockList(reactors.Select(x => x as IMyCubeBlock).ToList(), "Ingot", "Uranium") / currentReactorOutput) * 3600;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenPowerSummary: Caught Exception while calculating PowerTimeLeft: {e.ToString()}");
            }

            return timeRemaining;
        }

        public static float GetItemAmountFromBlockList(List<IMyCubeBlock> blocks, string targetTypeId, string targetSubtypeId)
        {
            var amount = 0.0f;

            try
            {
                List<IMyInventory> inventorys = new List<IMyInventory>();
                List<VRage.Game.ModAPI.Ingame.MyInventoryItem> inventoryItems = new List<VRage.Game.ModAPI.Ingame.MyInventoryItem>();

                foreach (var block in blocks)
                {
                    if (block == null) continue;
                    if (!block.HasInventory) continue;

                    for (int i = 0; i < block.InventoryCount; i++)
                    {
                        inventorys.Add(block.GetInventory(i));
                    }
                }

                foreach (var inventory in inventorys)
                {
                    if (inventory == null) continue;
                    if (inventory.ItemCount == 0) continue;

                    inventory.GetItems(inventoryItems);

                    foreach (var item in inventoryItems.OrderBy(i => i.Type.SubtypeId))
                    {
                        if (item == null) continue;

                        var typeId = item.Type.TypeId.Split('_')[1];
                        var subtypeId = item.Type.SubtypeId;
                        var currentAmount = item.Amount.ToIntSafe();

                        if (typeId == targetTypeId || typeId.Contains(targetTypeId))
                        {
                            if (subtypeId == targetSubtypeId || subtypeId.Contains(targetSubtypeId))
                            {
                                amount += currentAmount;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenPowerSummary: Caught Exception while GetItemAmountFromBlockList: {e.ToString()}");
            }

            return amount;
        }

        public static int GetIceAmountFromBlockList(List<IMyGasGenerator> generators)
        {
            var amount = 0;

            try
            {
                List<VRage.Game.ModAPI.Ingame.MyInventoryItem> inventoryItems = new List<VRage.Game.ModAPI.Ingame.MyInventoryItem>();
                foreach (var generator in generators)
                {
                    if (generator == null) continue;

                    generator.GetInventory(0).GetItems(inventoryItems);

                    foreach (var item in inventoryItems.OrderBy(i => i.Type.SubtypeId))
                    {
                        if (item == null) continue;

                        var subtypeId = item.Type.SubtypeId;

                        if (subtypeId.Contains("Ice"))
                        {
                            amount += item.Amount.ToIntSafe();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenPowerSummary: Caught Exception while GetItemAmountFromBlockList: {e.ToString()}");
            }

            return amount;
        }

        // Overload that accepts List<MyCubeBlock> and creates temporary list internally
        public static GridIceData GetGridIceData(List<MyCubeBlock> blocks, float iceItemVolumeL)
        {
            List<VRage.Game.ModAPI.Ingame.MyInventoryItem> items = new List<VRage.Game.ModAPI.Ingame.MyInventoryItem>();
            return GetGridIceData(blocks, iceItemVolumeL, items);
        }

        // Overload that accepts List<IMyCubeBlock> and creates temporary list internally
        public static GridIceData GetGridIceData(List<IMyCubeBlock> blocks, float iceItemVolumeL)
        {
            List<VRage.Game.ModAPI.Ingame.MyInventoryItem> items = new List<VRage.Game.ModAPI.Ingame.MyInventoryItem>();
            return GetGridIceData(blocks, iceItemVolumeL, items);
        }

        // Core implementation accepting List<MyCubeBlock> with reusable item list
        public static GridIceData GetGridIceData(List<MyCubeBlock> blocks, float iceItemVolumeL, List<VRage.Game.ModAPI.Ingame.MyInventoryItem> reusableItemList)
        {
            float currentVol = 0f;
            float maxVol = 0f;
            int itemCount = 0;

            try
            {
                foreach (var block in blocks)
                {
                    var tb = block as IMyTerminalBlock;
                    if (tb == null || !tb.HasInventory) continue;

                    for (int invIdx = 0; invIdx < tb.InventoryCount; invIdx++)
                    {
                        var inv = tb.GetInventory(invIdx);
                        if (inv == null) continue;

                        maxVol += (float)inv.MaxVolume * 1000f;
                        if (inv.ItemCount == 0) continue;

                        reusableItemList.Clear();
                        inv.GetItems(reusableItemList);
                        foreach (var item in reusableItemList)
                        {
                            if (item.Type.SubtypeId.IndexOf("Ice", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                int amount = item.Amount.ToIntSafe();
                                itemCount += amount;
                                if (iceItemVolumeL > 0f)
                                    currentVol += amount * iceItemVolumeL;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.MahUtillities: Caught Exception while GetGridIceData: {e.ToString()}");
            }

            return new GridIceData { currentIceVolumeL = currentVol, maxIceVolumeL = maxVol, iceItemCount = itemCount };
        }

        // Core implementation accepting List<IMyCubeBlock> with reusable item list
        public static GridIceData GetGridIceData(List<IMyCubeBlock> blocks, float iceItemVolumeL, List<VRage.Game.ModAPI.Ingame.MyInventoryItem> reusableItemList)
        {
            float currentVol = 0f;
            float maxVol = 0f;
            int itemCount = 0;

            try
            {
                foreach (var block in blocks)
                {
                    var tb = block as IMyTerminalBlock;
                    if (tb == null || !tb.HasInventory) continue;

                    for (int invIdx = 0; invIdx < tb.InventoryCount; invIdx++)
                    {
                        var inv = tb.GetInventory(invIdx);
                        if (inv == null) continue;

                        maxVol += (float)inv.MaxVolume * 1000f;
                        if (inv.ItemCount == 0) continue;

                        reusableItemList.Clear();
                        inv.GetItems(reusableItemList);
                        foreach (var item in reusableItemList)
                        {
                            if (item.Type.SubtypeId.IndexOf("Ice", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                int amount = item.Amount.ToIntSafe();
                                itemCount += amount;
                                if (iceItemVolumeL > 0f)
                                    currentVol += amount * iceItemVolumeL;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.MahUtillities: Caught Exception while GetGridIceData: {e.ToString()}");
            }

            return new GridIceData { currentIceVolumeL = currentVol, maxIceVolumeL = maxVol, iceItemCount = itemCount };
        }

        // Overload that accepts List<MyCubeBlock> and creates temporary list internally
        public static GridOrganicData GetGridOrganicData(List<MyCubeBlock> blocks, float organicItemVolumeL)
        {
            List<VRage.Game.ModAPI.Ingame.MyInventoryItem> items = new List<VRage.Game.ModAPI.Ingame.MyInventoryItem>();
            return GetGridOrganicData(blocks, organicItemVolumeL, items);
        }

        // Overload that accepts List<IMyCubeBlock> and creates temporary list internally
        public static GridOrganicData GetGridOrganicData(List<IMyCubeBlock> blocks, float organicItemVolumeL)
        {
            List<VRage.Game.ModAPI.Ingame.MyInventoryItem> items = new List<VRage.Game.ModAPI.Ingame.MyInventoryItem>();
            return GetGridOrganicData(blocks, organicItemVolumeL, items);
        }

        // Core implementation accepting List<MyCubeBlock> with reusable item list
        public static GridOrganicData GetGridOrganicData(List<MyCubeBlock> blocks, float organicItemVolumeL, List<VRage.Game.ModAPI.Ingame.MyInventoryItem> reusableItemList)
        {
            float currentVol = 0f;
            float maxVol = 0f;
            int itemCount = 0;

            try
            {
                foreach (var block in blocks)
                {
                    var tb = block as IMyTerminalBlock;
                    if (tb == null || !tb.HasInventory) continue;

                    for (int invIdx = 0; invIdx < tb.InventoryCount; invIdx++)
                    {
                        var inv = tb.GetInventory(invIdx);
                        if (inv == null) continue;

                        maxVol += (float)inv.MaxVolume * 1000f;
                        if (inv.ItemCount == 0) continue;

                        reusableItemList.Clear();
                        inv.GetItems(reusableItemList);
                        foreach (var item in reusableItemList)
                        {
                            if (item.Type.TypeId.ToString().Contains("Ore") && 
                                item.Type.SubtypeId.Equals("Organic", StringComparison.OrdinalIgnoreCase))
                            {
                                int amount = item.Amount.ToIntSafe();
                                itemCount += amount;
                                if (organicItemVolumeL > 0f)
                                    currentVol += amount * organicItemVolumeL;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.MahUtillities: Caught Exception while GetGridOrganicData: {e.ToString()}");
            }

            return new GridOrganicData { currentOrganicVolumeL = currentVol, maxOrganicVolumeL = maxVol, organicItemCount = itemCount };
        }

        // Core implementation accepting List<IMyCubeBlock> with reusable item list
        public static GridOrganicData GetGridOrganicData(List<IMyCubeBlock> blocks, float organicItemVolumeL, List<VRage.Game.ModAPI.Ingame.MyInventoryItem> reusableItemList)
        {
            float currentVol = 0f;
            float maxVol = 0f;
            int itemCount = 0;

            try
            {
                foreach (var block in blocks)
                {
                    var tb = block as IMyTerminalBlock;
                    if (tb == null || !tb.HasInventory) continue;

                    for (int invIdx = 0; invIdx < tb.InventoryCount; invIdx++)
                    {
                        var inv = tb.GetInventory(invIdx);
                        if (inv == null) continue;

                        maxVol += (float)inv.MaxVolume * 1000f;
                        if (inv.ItemCount == 0) continue;

                        reusableItemList.Clear();
                        inv.GetItems(reusableItemList);
                        foreach (var item in reusableItemList)
                        {
                            if (item.Type.TypeId.ToString().Contains("Ore") && 
                                item.Type.SubtypeId.Equals("Organic", StringComparison.OrdinalIgnoreCase))
                            {
                                int amount = item.Amount.ToIntSafe();
                                itemCount += amount;
                                if (organicItemVolumeL > 0f)
                                    currentVol += amount * organicItemVolumeL;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.MahUtillities: Caught Exception while GetGridOrganicData: {e.ToString()}");
            }

            return new GridOrganicData { currentOrganicVolumeL = currentVol, maxOrganicVolumeL = maxVol, organicItemCount = itemCount };
        }

        // Overload that accepts List<MyCubeBlock> and creates temporary list internally
        public static GridHydroPelletsData GetGridHydroPelletsData(List<MyCubeBlock> blocks, float hydroPelletsItemVolumeL)
        {
            List<VRage.Game.ModAPI.Ingame.MyInventoryItem> items = new List<VRage.Game.ModAPI.Ingame.MyInventoryItem>();
            return GetGridHydroPelletsData(blocks, hydroPelletsItemVolumeL, items);
        }

        // Overload that accepts List<IMyCubeBlock> and creates temporary list internally
        public static GridHydroPelletsData GetGridHydroPelletsData(List<IMyCubeBlock> blocks, float hydroPelletsItemVolumeL)
        {
            List<VRage.Game.ModAPI.Ingame.MyInventoryItem> items = new List<VRage.Game.ModAPI.Ingame.MyInventoryItem>();
            return GetGridHydroPelletsData(blocks, hydroPelletsItemVolumeL, items);
        }

        // Core implementation accepting List<MyCubeBlock> with reusable item list
        public static GridHydroPelletsData GetGridHydroPelletsData(List<MyCubeBlock> blocks, float hydroPelletsItemVolumeL, List<VRage.Game.ModAPI.Ingame.MyInventoryItem> reusableItemList)
        {
            float currentVol = 0f;
            float maxVol = 0f;
            int itemCount = 0;

            try
            {
                foreach (var block in blocks)
                {
                    var tb = block as IMyTerminalBlock;
                    if (tb == null || !tb.HasInventory) continue;

                    for (int invIdx = 0; invIdx < tb.InventoryCount; invIdx++)
                    {
                        var inv = tb.GetInventory(invIdx);
                        if (inv == null) continue;

                        maxVol += (float)inv.MaxVolume * 1000f;
                        if (inv.ItemCount == 0) continue;

                        reusableItemList.Clear();
                        inv.GetItems(reusableItemList);
                        foreach (var item in reusableItemList)
                        {
                            if (item.Type.TypeId.ToString().Contains("Ore") && 
                                item.Type.SubtypeId.Equals("HydroPellets", StringComparison.OrdinalIgnoreCase))
                            {
                                int amount = item.Amount.ToIntSafe();
                                itemCount += amount;
                                if (hydroPelletsItemVolumeL > 0f)
                                    currentVol += amount * hydroPelletsItemVolumeL;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.MahUtillities: Caught Exception while GetGridHydroPelletsData: {e.ToString()}");
            }

            return new GridHydroPelletsData { currentHydroPelletsVolumeL = currentVol, maxHydroPelletsVolumeL = maxVol, hydroPelletsItemCount = itemCount };
        }

        // Core implementation accepting List<IMyCubeBlock> with reusable item list
        public static GridHydroPelletsData GetGridHydroPelletsData(List<IMyCubeBlock> blocks, float hydroPelletsItemVolumeL, List<VRage.Game.ModAPI.Ingame.MyInventoryItem> reusableItemList)
        {
            float currentVol = 0f;
            float maxVol = 0f;
            int itemCount = 0;

            try
            {
                foreach (var block in blocks)
                {
                    var tb = block as IMyTerminalBlock;
                    if (tb == null || !tb.HasInventory) continue;

                    for (int invIdx = 0; invIdx < tb.InventoryCount; invIdx++)
                    {
                        var inv = tb.GetInventory(invIdx);
                        if (inv == null) continue;

                        maxVol += (float)inv.MaxVolume * 1000f;
                        if (inv.ItemCount == 0) continue;

                        reusableItemList.Clear();
                        inv.GetItems(reusableItemList);
                        foreach (var item in reusableItemList)
                        {
                            if (item.Type.TypeId.ToString().Contains("Ore") && 
                                item.Type.SubtypeId.Equals("HydroPellets", StringComparison.OrdinalIgnoreCase))
                            {
                                int amount = item.Amount.ToIntSafe();
                                itemCount += amount;
                                if (hydroPelletsItemVolumeL > 0f)
                                    currentVol += amount * hydroPelletsItemVolumeL;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.MahUtillities: Caught Exception while GetGridHydroPelletsData: {e.ToString()}");
            }

            return new GridHydroPelletsData { currentHydroPelletsVolumeL = currentVol, maxHydroPelletsVolumeL = maxVol, hydroPelletsItemCount = itemCount };
        }

        // Config helper methods to reduce boilerplate config reading code
        public static bool TryGetConfigBool(MyIni config, string section, string key, ref bool value, ref bool errorFlag)
        {
            if (config.ContainsKey(section, key))
            {
                value = config.Get(section, key).ToBoolean();
                return true;
            }
            errorFlag = true;
            return false;
        }

        public static bool TryGetConfigInt(MyIni config, string section, string key, ref int value, ref bool errorFlag)
        {
            if (config.ContainsKey(section, key))
            {
                value = config.Get(section, key).ToInt32();
                return true;
            }
            errorFlag = true;
            return false;
        }

        public static bool TryGetConfigFloat(MyIni config, string section, string key, ref float value, ref bool errorFlag)
        {
            if (config.ContainsKey(section, key))
            {
                value = config.Get(section, key).ToSingle();
                return true;
            }
            errorFlag = true;
            return false;
        }

        public static bool TryGetConfigString(MyIni config, string section, string key, ref string value, ref bool errorFlag)
        {
            if (config.ContainsKey(section, key))
            {
                value = config.Get(section, key).ToString();
                return true;
            }
            errorFlag = true;
            return false;
        }

        // Block filtering helper structures and methods
        public struct PowerBlocks
        {
            public List<IMyBatteryBlock> Batteries;
            public List<IMyPowerProducer> WindTurbines;
            public List<IMyPowerProducer> HydrogenEngines;
            public List<IMySolarPanel> SolarPanels;
            public List<IMyReactor> Reactors;
            public List<IMyPowerProducer> AllPowerProducers;
        }

        public static PowerBlocks GetPowerBlocks(List<MyCubeBlock> blocks)
        {
            var result = new PowerBlocks
            {
                Batteries = new List<IMyBatteryBlock>(),
                WindTurbines = new List<IMyPowerProducer>(),
                HydrogenEngines = new List<IMyPowerProducer>(),
                SolarPanels = new List<IMySolarPanel>(),
                Reactors = new List<IMyReactor>(),
                AllPowerProducers = new List<IMyPowerProducer>()
            };

            foreach (var block in blocks)
            {
                if (block == null) continue;

                if (block is IMyPowerProducer)
                {
                    var producer = (IMyPowerProducer)block;
                    result.AllPowerProducers.Add(producer);

                    if (block is IMyBatteryBlock)
                    {
                        result.Batteries.Add((IMyBatteryBlock)block);
                    }
                    else if (block.BlockDefinition.Id.SubtypeName.Contains("Wind"))
                    {
                        result.WindTurbines.Add(producer);
                    }
                    else if (block.BlockDefinition.Id.SubtypeName.Contains("Hydrogen"))
                    {
                        result.HydrogenEngines.Add(producer);
                    }
                    else if (block is IMyReactor)
                    {
                        result.Reactors.Add((IMyReactor)block);
                    }
                    else if (block is IMySolarPanel)
                    {
                        result.SolarPanels.Add((IMySolarPanel)block);
                    }
                }
            }

            return result;
        }

        public struct SeparatedGasTanks
        {
            public List<IMyGasTank> HydrogenTanks;
            public List<IMyGasTank> OxygenTanks;
            public int HydrogenCount;
            public int OxygenCount;
        }

        public static SeparatedGasTanks SeparateGasTanks(List<IMyGasTank> allTanks)
        {
            var result = new SeparatedGasTanks
            {
                HydrogenTanks = new List<IMyGasTank>(),
                OxygenTanks = new List<IMyGasTank>(),
                HydrogenCount = 0,
                OxygenCount = 0
            };

            foreach (var tank in allTanks)
            {
                if (tank == null) continue;

                if (tank.BlockDefinition.SubtypeName.Contains("Hydrogen"))
                {
                    result.HydrogenTanks.Add(tank);
                    result.HydrogenCount++;
                }
                else
                {
                    result.OxygenTanks.Add(tank);
                    result.OxygenCount++;
                }
            }

            return result;
        }

        public static string GetSubstring(string s, SurfaceDrawer.SurfaceData surfaceData, bool cutLeft = false)
        {
            int maxLength = (int)(surfaceData.titleOffset / MahDefinitions.pixelPerChar * surfaceData.textSize);
            s = s.Length <= maxLength ? s : s.Substring(cutLeft ? 0 : (s.Length - maxLength - 1), maxLength);

            return s;
        }

        public static List<MyCubeBlock> GetBlocks (MyCubeGrid cubeGrid, string searchId, List<string> excludeIds, ref Sandbox.ModAPI.Ingame.MyShipMass gridMass, bool includeSubGrids = false, bool includeDocked = false)
        {
            if (cubeGrid == null) return null;

            var myFatBlocks = cubeGrid.GetFatBlocks().Where(block => block is IMyTerminalBlock);
            List<MyCubeBlock> allBlocks = new List<MyCubeBlock>();
            List<MyCubeGrid> scannedGrids = new List<MyCubeGrid>();

            scannedGrids.Add(cubeGrid);
            
            try
            {
                foreach (var block in myFatBlocks)
                {
                    if (block == null) continue;

                    if (block is IMyShipController)
                    {
                        gridMass = (block as IMyShipController).CalculateShipMass();
                        continue;
                    }

                    // If docked grids should be included.
                    if (includeDocked)
                    {
                        // Try get a ship connector
                        if (block is IMyShipConnector)
                        {
                            IMyShipConnector connector = block as IMyShipConnector;

                            // Check if connector is connected to something.
                            if (connector.IsConnected)
                            {
                                // Get the connected IMyShipConnector.
                                IMyShipConnector otherConnector = connector.OtherConnector;

                                if (otherConnector != null)
                                {
                                    // Get the grid connected to the IMyShipConnector.
                                    MyCubeGrid connectedGrid = otherConnector.CubeGrid as MyCubeGrid;

                                    // If there is a grid connected, try scanning that.
                                    if (connectedGrid != null)
                                    {
                                        // Abort if the grid of the base has been scanned before.
                                        if (!scannedGrids.Contains(connectedGrid))
                                        {
                                            // Scan all blocks from the connectedGrid, but disable showDocked for this scan...otherwise an endless loop is produced crashing the game.
                                            var connectedBlocks = GetBlocks(connectedGrid, searchId, excludeIds, ref gridMass, includeSubGrids, false);
                                            if (connectedBlocks != null)
                                            {
                                                allBlocks.AddRange(connectedBlocks);
                                            }
                                        }

                                        // Add the other grid to the allready scanned grids.
                                        scannedGrids.Add(connectedGrid);
                                    }
                                }
                            }
                        }
                    }

                    // Scan SubGrids if this is either a baseBlock or topBlock of some mechanical connection.
                    if (includeSubGrids)
                    {
                        // Block is rotor, piston or hinge base
                        if (block is IMyMechanicalConnectionBlock)
                        {
                            // Get block as base.
                            IMyMechanicalConnectionBlock baseBlock = block as IMyMechanicalConnectionBlock;

                            // Get the top of that base.
                            var topBlock = baseBlock.Top;

                            // Get the grid of that top.
                            MyCubeGrid subGrid = topBlock != null ? topBlock.CubeGrid as MyCubeGrid : null;

                            // Scan all blocks of that top/subGrid (only if subGrid is valid).
                            if (subGrid != null)
                            {
                                var subGridBlocks = GetBlocks(subGrid, searchId, excludeIds, ref gridMass, true, includeDocked);
                                if (subGridBlocks != null)
                                {
                                    allBlocks.AddRange(subGridBlocks);
                                }
                            }
                        }
                    }

                    // Check if block is valid or should be ignored.
                    if (!HasValidId(block as IMyTerminalBlock, searchId, excludeIds)) continue;

                    allBlocks.Add(block as MyCubeBlock);
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.MahUtillities: Caught Exception while GetBlocks: {e.ToString()}");
            }

            return allBlocks;
        }

        public static List<IMyInventory> GetInventories(MyCubeGrid cubeGrid, string searchId, List<string> excludeIds, ref Sandbox.ModAPI.Ingame.MyShipMass gridMass, bool includeSubGrids = false, bool includeDocked = false)
        {
            List<IMyInventory> inventories = new List<IMyInventory>();

            if (cubeGrid == null) return inventories;

            // Only grab those terminal blocks that actually have an inventory.
            var myFatBlocks = GetBlocks(cubeGrid, searchId, excludeIds, ref gridMass, includeSubGrids, includeDocked).Where(block => block.HasInventory).ToList();

            foreach (var block in myFatBlocks)
            {
                if (block == null) continue;
                if (!HasValidId(block as IMyTerminalBlock, searchId, excludeIds)) continue;

                for (int i = 0; i < block.InventoryCount; i++)
                {
                    inventories.Add(block.GetInventory(i));
                }
            }

            return inventories;
        }

        public static bool HasValidId (IMyTerminalBlock block, string searchId, List<string> excludeIds)
        {
            if (block == null) return false;

            // If this is a multi-layer searchId, scan every one of them
            if (searchId.Contains(",")) return HasValidId(block, searchId.Split(',').ToList(), excludeIds);

            string blockId = block.CustomName.ToLower();
            searchId = searchId.ToLower();
            
            if (searchId != "*" && searchId.Trim() != "")
            {
                if (!blockId.Contains(searchId))
                {
                    return false;
                }
            }
            
            foreach (string s in excludeIds)
            {
                string ex = s.ToLower();

                if (ex == "*") continue;
                if (ex == searchId) continue;
                if (ex.Trim().Length <= 0) continue;
                if (ex.Contains("<")) continue;
                if (blockId.Contains(ex))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool HasValidId (IMyTerminalBlock block, List<string> searchIds, List<string> excludeIds)
        {
            if (block == null) return false;

            string blockId = block.CustomName.ToLower();

            foreach (string s in excludeIds)
            {
                string excludeId = s.ToLower();

                if (excludeId == "*") continue;
                if (excludeId.Trim().Length <= 0) continue;
                if (excludeId.Contains("<")) continue;
                if (searchIds.Contains(s)) continue;
                if (searchIds.Contains(s.ToLower())) continue;
                if (blockId.Contains(excludeId))
                {
                    return false;
                }
            }

            foreach (string s in searchIds)
            {
                string searchId = s.Trim().ToLower();
                if (searchId == "*") return true;
                if (String.IsNullOrEmpty(searchId) || searchId == "" || searchId.Length < 3) continue;

                if (blockId.Contains(searchId))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Centralized sorting utilities for blocks and items across all LCD screens
    /// </summary>
    public static class MahSorting
    {
        /// <summary>
        /// Sorting modes for items (components, ores, ingots, ammo, etc.)
        /// </summary>
        public enum ItemSortMode
        {
            /// <summary>Sort by subtype ID alphabetically (default behavior)</summary>
            SubtypeId,
            /// <summary>Sort by display name alphabetically</summary>
            DisplayName,
            /// <summary>Sort by amount (descending)</summary>
            Amount,
            /// <summary>Sort by type ID, then subtype ID</summary>
            TypeThenSubtype
        }

        /// <summary>
        /// Sorting modes for blocks (air vents, farm plots, etc.)
        /// </summary>
        public enum BlockSortMode
        {
            /// <summary>Sort by custom name alphabetically (default for blocks)</summary>
            CustomName,
            /// <summary>Sort by functional status (working first, then non-working)</summary>
            Status,
            /// <summary>Sort by priority (from BlockStateData), then custom name</summary>
            Priority
        }

        /// <summary>
        /// Sort a list of terminal blocks by their custom name
        /// </summary>
        public static void SortBlocksByName<T>(List<T> blocks) where T : IMyTerminalBlock
        {
            if (blocks == null || blocks.Count <= 1) return;
            blocks.Sort((a, b) => 
            {
                if (a == null) return b == null ? 0 : 1;
                if (b == null) return -1;
                return string.Compare(a.CustomName, b.CustomName, StringComparison.OrdinalIgnoreCase);
            });
        }

        /// <summary>
        /// Sort inventory items by the specified mode
        /// </summary>
        public static IEnumerable<VRage.Game.ModAPI.Ingame.MyInventoryItem> SortItems(
            IEnumerable<VRage.Game.ModAPI.Ingame.MyInventoryItem> items,
            ItemSortMode sortMode = ItemSortMode.SubtypeId)
        {
            if (items == null) return Enumerable.Empty<VRage.Game.ModAPI.Ingame.MyInventoryItem>();

            switch (sortMode)
            {
                case ItemSortMode.SubtypeId:
                    return items.OrderBy(i => i.Type.SubtypeId);

                case ItemSortMode.DisplayName:
                    return items.OrderBy(i => 
                    {
                        var def = MahDefinitions.GetDefinition(i.Type.TypeId.Split('_')[1], i.Type.SubtypeId);
                        return def?.displayName ?? i.Type.SubtypeId;
                    });

                case ItemSortMode.Amount:
                    return items.OrderByDescending(i => i.Amount.ToIntSafe());

                case ItemSortMode.TypeThenSubtype:
                    return items.OrderBy(i => i.Type.TypeId).ThenBy(i => i.Type.SubtypeId);

                default:
                    return items.OrderBy(i => i.Type.SubtypeId);
            }
        }

        /// <summary>
        /// Sort CargoItemType dictionary entries by the specified mode
        /// </summary>
        public static IEnumerable<KeyValuePair<string, CargoItemType>> SortCargoItems(
            Dictionary<string, CargoItemType> cargo,
            ItemSortMode sortMode = ItemSortMode.SubtypeId)
        {
            if (cargo == null || cargo.Count == 0) 
                return Enumerable.Empty<KeyValuePair<string, CargoItemType>>();

            switch (sortMode)
            {
                case ItemSortMode.SubtypeId:
                    return cargo.OrderBy(kv => kv.Key);

                case ItemSortMode.DisplayName:
                    return cargo.OrderBy(kv => kv.Value.definition?.displayName ?? kv.Key);

                case ItemSortMode.Amount:
                    return cargo.OrderByDescending(kv => kv.Value.amount);

                case ItemSortMode.TypeThenSubtype:
                    return cargo.OrderBy(kv => kv.Value.item.Type.TypeId).ThenBy(kv => kv.Key);

                default:
                    return cargo.OrderBy(kv => kv.Key);
            }
        }

        /// <summary>
        /// Sort BlockStateData by the specified mode
        /// </summary>
        public static void SortBlockStateData(List<BlockStateData> blocks, BlockSortMode sortMode = BlockSortMode.CustomName)
        {
            if (blocks == null || blocks.Count <= 1) return;

            switch (sortMode)
            {
                case BlockSortMode.CustomName:
                    blocks.Sort((a, b) => string.Compare(a.CustomName, b.CustomName, StringComparison.OrdinalIgnoreCase));
                    break;

                case BlockSortMode.Status:
                    blocks.Sort((a, b) =>
                    {
                        // Working blocks first, then by name
                        if (a.IsWorking != b.IsWorking)
                            return b.IsWorking.CompareTo(a.IsWorking);
                        return string.Compare(a.CustomName, b.CustomName, StringComparison.OrdinalIgnoreCase);
                    });
                    break;

                case BlockSortMode.Priority:
                    blocks.Sort((a, b) =>
                    {
                        // Sort by priority first, then by name
                        if (a.priority != b.priority)
                            return a.priority.CompareTo(b.priority);
                        return string.Compare(a.CustomName, b.CustomName, StringComparison.OrdinalIgnoreCase);
                    });
                    break;
            }
        }
    }

    public class CargoItemType
    {
        public VRage.Game.ModAPI.Ingame.MyInventoryItem item;
        public CargoItemDefinition definition;
        public int amount;
    }

    public class CargoItemDefinition
    {
        public int minAmount;
        public float volume;
        public string typeId;
        public string subtypeId;
        public string displayName;
        public string sortId; // grouping / sorting category (e.g., "ore"), empty string if not categorized
    }

    public enum Unit
    {
        None,
        Count,
        Percent,
        Kilograms,
        Liters,
        Watt,
        WattHours,
    }

    public struct GasTankVolumes
    {
        public float currentVolume;
        public float totalVolume;
        public float ratio => (currentVolume / totalVolume) * 100;
    }

    public struct GridIceData
    {
        public float currentIceVolumeL;
        public float maxIceVolumeL;
        public int iceItemCount;
    }

    public struct GridOrganicData
    {
        public float currentOrganicVolumeL;
        public float maxOrganicVolumeL;
        public int organicItemCount;
    }

    public struct GridHydroPelletsData
    {
        public float currentHydroPelletsVolumeL;
        public float maxHydroPelletsVolumeL;
        public int hydroPelletsItemCount;
    }

    public struct BlockStateData
    {
        public IMyTerminalBlock block;
        public IMySlimBlock slimBlock;
        public int priority;

        public BlockStateData (IMyTerminalBlock block)
        {
            this.block = block;
            slimBlock = MahUtillities.GetSlimblock(block);

            if (block is IMyUserControllableGun)
                priority = 0;
            else if (block is IMyPowerProducer)
                priority = 0;
            else if (block is IMyShipToolBase)
                priority = 1;
            else if (block is IMyCockpit)
                priority = 2;
            else if (block is IMyCryoChamber)
                priority = 2;
            else if (block is IMyMedicalRoom)
                priority = 2;
            else if (block is IMyGasGenerator)
                priority = 3;
            else if (block is IMyGasTank)
                priority = 3;
            else if (block is IMyAirVent)
                priority = 3;
            else if (block is IMyOxygenFarm)
                priority = 3;
            else if (block is IMyDoor)
                priority = 3;
            else if (block is IMyCargoContainer)
                priority = 4;
            else if (block is IMyProductionBlock)
                priority = 4;
            else
                priority = 10;
        }

        public bool IsNull => block == null || slimBlock == null;
        public bool IsWorking => block != null ? block.IsWorking : false;
        public bool IsFunctional => block != null ? block.IsFunctional : false;
        public bool IsBeingHacked => block != null ? block.IsBeingHacked : false;
        public bool IsFullIntegrity => slimBlock != null ? slimBlock.IsFullIntegrity : true;
        public bool IsWeapon => block != null ? block is IMyUserControllableGun : false;
        public bool IsRecharging => block != null ? block.DetailedInfo.Contains("Fully recharged in:") && !block.DetailedInfo.Contains("Fully recharged in: 0 sec") : false;
        public float MaxIntegrity => slimBlock != null ? slimBlock.MaxIntegrity : 1;
        public float CurrentIntegrity => slimBlock != null ? slimBlock.CurrentDamage <= 0 ? slimBlock.BuildIntegrity : slimBlock.MaxIntegrity - slimBlock.CurrentDamage : 0;
        public string CustomName => block != null ? block.CustomName : "Unknown";
    }

    public struct BlockInventoryData
    {
        public IMyTerminalBlock block;
        public IMyInventory[] inventories;

        public double CurrentVolume => inventories.Sum(x => (double)x.CurrentVolume);
        public double MaxVolume => inventories.Sum(x => (double)x.MaxVolume);
    }
}
