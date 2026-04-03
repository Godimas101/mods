<p align="center">
  <img src="https://capsule-render.vercel.app/api?type=rounded&height=170&color=0:0B1021,45:0F766E,100:2563EB&text=🛸%20Space%20Engineers%20Mods&fontColor=ffffff&fontAlignY=35&fontSize=30&desc=InfoLCD,%20server%20tweaks,%20and%20compatibility%20patches&descAlignY=57&descSize=18" />
</p>

> **"LCD scripts, server tweaks, and compatibility patches for engineers who like their bases slightly overengineered."**

This repo is the main home for my Space Engineers mod collection, now split out as the standalone `se-mods` repo. The flagship project is **InfoLCD**, backed up by server-focused balance work, compatibility patches, and a handful of odd little utility mods.

## 🚀 Quick Start

1. Browse the `Mods/` folder for the mod or project you want
2. Use the Space Engineers Mod SDK to test changes locally or prep Workshop updates
3. If you need asset prep, use the companion tools:
   - [`universal-image-converter`](https://github.com/Godimas101/universal-image-converter/releases/latest)
   - [`universal-audio-converter`](https://github.com/Godimas101/universal-audio-converter/releases/latest)

## ✨ What's in the Hangar

### 📊 InfoLCD Series

| Mod | Status | Notes |
|-----|--------|-------|
| **InfoLCD - Apex Update** | Active development | Built for the Apex Update modpack |
| **InfoLCD - Apex Advanced** | Maintenance only | Kept in sync for the Apex Advanced pack |

20+ specialized screens live across the InfoLCD work: items, cargo, power, production, ammo, components, doors, damage, gas, life support, and more. Expect scrolling lists, category filtering, subgrid scanning, and CustomData-driven configuration.

For the actual scripts, SBCs, and mod assets, start in [`Mods/InfoLCD - Apex Update/`](Mods/InfoLCD%20-%20Apex%20Update/).

---

### ⚙️ Sturmgrenadier Core Series

Core gameplay overhaul work for the Sturmgrenadier server ecosystem.

| Mod | Purpose |
|-----|---------|
| Sturmgrenadier Core Mod | Base overhaul |
| Sturmgrenadier Core Power | Power system changes |
| Sturmgrenadier Core Production | Production and crafting tweaks |
| Sturmgrenadier Core Survival | Survival balance |
| Sturmgrenadier Core Vanilla Combat | Combat rebalance |

---

### 🔧 Mod Adjusters

Balance and compatibility patches for third-party mods used on Sturmgrenadier servers.

| Mod | Adjusts |
|-----|---------|
| Artillery MKII Turret - Goliath | Weapon balance |
| Dense Colorable Solar Panels | Power output |
| Federal Industrial - Utilities | Compatibility |
| Isy's Dense Solar Panels | Power output |
| Life'Tech - Algaetechnology | Farming and resources |
| ModCubeBlocks Refinery x10 | Refinery speed |
| ModCubeBlocks Upgrade Module | Module balance |
| More Engineer Characters | Character compatibility |
| More Wind Turbines | Wind power output |
| [Mafoo] More Batteries | Battery capacity and charge |

---

### ✨ Other Mods

| Mod | Description |
|-----|-------------|
| Not Just For Looks | Gives DLC cosmetic blocks actual useful stats |
| Universal Image Converter | Workshop LCD image pack — separate from the standalone tool repo of the same name |

---

## 🧰 Companion Tools

These tools used to live alongside the mods, but now have their own homes:

| Repo | Use case |
|------|----------|
| [`universal-image-converter`](https://github.com/Godimas101/universal-image-converter/releases/latest) | Convert images to `.dds` textures or pasteable LCD text |
| [`universal-audio-converter`](https://github.com/Godimas101/universal-audio-converter/releases/latest) | Convert, edit, and generate audio assets for SE mods |
| [`space-engineers-modders-tool-kit`](https://github.com/Godimas101/space-engineers-modders-tool-kit) | Grab the broader toolbox in one place |

---

## 🛠️ Development Setup

**Prerequisites**
- Space Engineers + Mod SDK (installed through Steam)
- Visual Studio or VS Code for C# work

**SDK location**
```text
D:\SteamLibrary\steamapps\common\SpaceEngineersModSDK\
```

**Typical workflow**
1. Reference the SDK DLLs for IntelliSense and API access
2. Write scripts in `Data/Scripts/<namespace>/`
3. Register LCD scripts in `Data/TextSurfaceScripts.sbc`
4. Test through `%AppData%\SpaceEngineers\Mods\` or via the Steam Workshop pipeline

---

## 🔗 Key Resources

- [Space Engineers Modding Wiki](https://spaceengineers.wiki.gg/wiki/Modding/Reference)
- [Mod API Documentation](https://keensoftwarehouse.github.io/SpaceEngineersModAPI/api/index.html)
- [Steam Workshop](https://steamcommunity.com/app/244850/workshop/)

## 🧡 Support

All mods are free and always will be. If they’ve added something fun to your save, consider supporting on Patreon — it helps keep updates and new projects coming.

[![Support on Patreon](https://raw.githubusercontent.com/Godimas101/personal-projects/main/patreon/images/buttons/patreon-medium.png)](https://patreon.com/Godimas101)

*Building better blocks, one mod at a time.*
