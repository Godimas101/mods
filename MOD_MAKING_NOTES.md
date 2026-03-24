# Space Engineers - Mod Making Notes

Consolidated notes for all mods in this workspace. See individual mod CLAUDE.md files for AI assistant context.

---

## Table of Contents
- [InfoLCD - Apex Update / Apex Advanced](#infolcd---apex-update--apex-advanced)
  - [Scrolling Feature - Evaluation](#scrolling-feature---evaluation)
  - [Scrolling Feature - Implementation Reference](#scrolling-feature---implementation-reference)
- [Mod Adjuster Mods](#mod-adjuster-mods)
- [Sturmgrenadier Core Mods](#sturmgrenadier-core-mods)

---

## InfoLCD - Apex Update / Apex Advanced

**Apex Update** is the active development version. **Apex Advanced** has intentionally diverged — scrolling was removed and an Extension screen was added. Once scrolling is stable and fully tested in Apex Update, it will be ported to Apex Advanced. Until then, the two versions are not kept in sync.

See `Mods/InfoLCD - Apex Update/CLAUDE.md` for the AI assistant context file.

---

### Scrolling Feature - Evaluation

Analysis of which screens are compatible with each scrolling approach.

#### ✅ UNIFIED LIST SCROLLING (Approach 1)
Screens with a single scrolling list where all items share the same scroll offset.

| Screen | Compatibility | Status | Notes |
|---|---|---|---|
| **DoorMonitor** | ⭐ Highly Compatible | ✅ Done | Simple door list, each door = one line |
| **DamageMonitor** | ⚠️ Partially | ✅ Done | Category headers scroll away — acceptable |

#### ✅ MULTI-CATEGORY WITH MAXLISTLINES (Approach 2)
Screens with multiple distinct categories needing independent space management.

| Screen | Compatibility | Status | Notes |
|---|---|---|---|
| **Production** | ⭐ Highly Compatible | ✅ Done | Refineries, assemblers, generators, farms, etc. Each category gets MaxListLines limit |
| **GasProduction** | ⭐ Highly Compatible | ✅ Done | Fixed bars (H2/O2/Ice) always visible; generator/farm lists scroll with MaxListLines |
| **LifeSupport** | ⭐ Highly Compatible | ✅ Done | Fixed bars (Battery/O2/H2/Ice) stay pinned; air vents list scrolls. No MaxListLines — position-based space calc |
| **Cargo** | ⭐ Highly Compatible | ✅ Done | Many categories (Ore, Ingots, Components, Ammo, Food, Seeds, etc.). Flat category list; no MaxListLines — position-based space calc |
| **Container** | ⭐ Highly Compatible | ✅ Done | Single flat list of containers. No MaxListLines — single list, screen height caps naturally |
| **Farming** | ⭐ Highly Compatible | ✅ Done | Fixed header + ice/water bars always visible; farm plot list scrolls. **4 lines per entry** (name, badge+hydration, water+growth, blank spacer). linesPerEntry=4, no MaxListLines |
| **Weapons** | ✅ Compatible (overhaul) | ✅ Done | Per-category scrolling with shared scroll offset. **3 lines per weapon entry** (badge+name, ammo type, ammo bar). Compact mode already works fine. Only detailed mode scrolls |

#### ✅ NEWLY COMPATIBLE (post-audit)

| Screen | Compatibility | Status | Notes |
|---|---|---|---|
| **AirlockMonitor** | ⭐ Simple | ⏳ Pending | Door list = 1 line per door. Air vent = single pressure bar (always visible, no scrolling needed). SearchId defaults to "Airlock" — monitors all doors/vents matching that name. Approach 1, door list only |
| **DetailedInfo** | ⭐ Simple | ✅ Done | Displays one block's DetailedInfo as scrollable text lines. Approach 1 (flat line list, no MaxListLines). Update100 screen — tick increment is `+= 100`. BuildInfo data NOT accessible (see Known Issues) |
| **GridInfo** | ⚠️ Partial | ✅ Done | Jump drives pinned at top (always visible). Connector list scrolls below. Approach 1 for connector section. Pre-filters connectors (vessels hide unconnected ones) before scroll logic |

#### ❌ NOT SUITABLE FOR SCROLLING

| Screen | Reason |
|---|---|
| **Systems** | Purely fixed dashboard — shows category-level integrity summaries (Power, Movement, Weapons, etc.), never individual blocks. At most ~15 categories, always fits on screen. Adding scrolling would require a full redesign to show per-block detail, which is out of scope |

#### 🔑 KEY DISCOVERY: Multi-Line Per Entry Scrolling

Originally we assumed scrolling only worked for single-line-per-item lists (like Items, Ammo, etc.). The Farming screen proved this wrong.

**The insight:** As long as each entry takes a **uniform, known number of lines**, scrolling works perfectly. The scroll unit becomes "per entry" instead of "per line":

```csharp
const int linesPerEntry = 4;  // however many lines each entry takes
int availableSlots = availableDataLines / linesPerEntry;
// scroll by entry index, not by line index
```

This opens up any screen where blocks are displayed with consistent multi-line layouts — including Weapons (3 lines/entry) and potentially others.

#### Approach 1 vs Approach 2 Decision Guide

**Use Unified List (Approach 1) when:**
- Screen shows one primary list (Items, Components, Ingots, etc.)
- All items are the same type/category
- Screen space is dedicated to that single list

**Use Multi-Category with MaxListLines (Approach 2) when:**
- Screen has multiple distinct categories (batteries, solar, wind, reactors)
- Each category needs a separate section with a header
- Need to balance viewing multiple categories vs depth per category
- Users may have dozens of items per category

---

### Scrolling Feature - Implementation Reference

#### Implementation Status

| Screen | Approach | Status |
|---|---|---|
| Items | Approach 1 | ✅ Completed & Tested |
| Power | Approach 2 | ✅ Completed & Tested |
| Components | Approach 1 | ✅ Done |
| Ingots | Approach 1 | ✅ Done |
| Ores | Approach 1 | ✅ Done |
| Ammo | Approach 1 | ✅ Done |
| DoorMonitor | Approach 1 | ✅ Done |
| DamageMonitor | Approach 1 | ✅ Done |
| Production | Approach 2 | ✅ Done |
| GasProduction | Approach 2 | ✅ Done |
| Cargo | Approach 2 (no MaxListLines) | ✅ Done |
| LifeSupport | Approach 2 (no MaxListLines) | ✅ Done |
| Container | Approach 2 (no MaxListLines) | ✅ Done |
| Farming | Approach 2 (no MaxListLines, linesPerEntry=4) | ✅ Done |
| Weapons | Approach 2 (linesPerEntry=3, shared scroll offset) | ✅ Done |
| AirlockMonitor | Approach 1 (door list, 1 line/entry) | ⏳ Pending |
| DetailedInfo | Approach 1 (text line array, Update100) | ✅ Done |
| GridInfo | Approach 1 (connector list, jump drives pinned) | ✅ Done |
| Systems | — | ⏸ Skipped (no individual block list) |

---

#### User Guide: Enabling Scrolling on LCD Blocks

**Step 1:** Open the LCD block → Script dropdown → Select InfoLCD script.

**Step 2:** Open CustomData → find the scrolling section (e.g., `; [ POWER - SCROLLING OPTIONS ]`) → edit parameters.

**Step 3:** Config templates:

```ini
; Fast scrolling (~0.5 sec per step)
ToggleScroll=True
ScrollSpeed=30
ScrollLines=1
MaxListLines=5

; Slow scrolling (~2 sec per step)
ToggleScroll=True
ScrollSpeed=120
ScrollLines=1
MaxListLines=5

; Show more per category
ToggleScroll=True
ScrollSpeed=60
ScrollLines=1
MaxListLines=10

; No list limit (use all available space)
ToggleScroll=True
ScrollSpeed=60
ScrollLines=1
MaxListLines=0

; Disable scrolling
ToggleScroll=False
```

**Single List Screens (Approach 1):** Items, Components, Ingots, Ores, Ammo, DoorMonitor, DamageMonitor, Container, Farming, AirlockMonitor, DetailedInfo, GridInfo — no `MaxListLines`.

**Multi-Category Screens (Approach 2):** Power, Production, GasProduction, Cargo, LifeSupport, Weapons — `MaxListLines` limits items shown per category.

---

#### Code Implementation Reference

**Approach 1: Unified List Scrolling**

State fields:
```csharp
bool toggleScroll = false;
bool reverseDirection = false;
int scrollSpeed = 60;
int scrollLines = 1;
int scrollOffset = 0;
int ticksSinceLastScroll = 0;
```

Config loading:
```csharp
if (config.ContainsKey(CONFIG_SECTION_ID, "ToggleScroll"))
    toggleScroll = config.Get(CONFIG_SECTION_ID, "ToggleScroll").ToBoolean(false);
if (config.ContainsKey(CONFIG_SECTION_ID, "ReverseDirection"))
    reverseDirection = config.Get(CONFIG_SECTION_ID, "ReverseDirection").ToBoolean(false);
if (config.ContainsKey(CONFIG_SECTION_ID, "ScrollSpeed"))
    scrollSpeed = Math.Max(1, config.Get(CONFIG_SECTION_ID, "ScrollSpeed").ToInt32(60));
if (config.ContainsKey(CONFIG_SECTION_ID, "ScrollLines"))
    scrollLines = Math.Max(1, config.Get(CONFIG_SECTION_ID, "ScrollLines").ToInt32(1));
```

Scroll update in Run() — **use `+= 10` for Update10 screens**:
```csharp
if (toggleScroll)
{
    ticksSinceLastScroll += 10;  // Update10 = 10 game ticks per call
    if (ticksSinceLastScroll >= scrollSpeed)
    {
        scrollOffset += reverseDirection ? -scrollLines : scrollLines;
        ticksSinceLastScroll = 0;
    }
}
else { scrollOffset = 0; }
```

Drawing with wraparound:
```csharp
int totalItems = itemList.Count;
int normalizedOffset = ((scrollOffset % totalItems) + totalItems) % totalItems;

for (int i = 0; i < totalItems && linesDrawn < availableLines; i++)
{
    int itemIndex = (normalizedOffset + i) % totalItems;
    // Draw itemList[itemIndex]
    linesDrawn++;
}
```

---

**Approach 2: Multi-Category Scrolling with MaxListLines**

Additional state field:
```csharp
int maxListLines = 5;
```

Additional config loading:
```csharp
if (config.ContainsKey(CONFIG_SECTION_ID, "MaxListLines"))
    maxListLines = Math.Max(0, config.Get(CONFIG_SECTION_ID, "MaxListLines").ToInt32(5));
```

Drawing each category:
```csharp
// Calculate available lines from CURRENT position (not screen top)
float screenHeight = mySurface.SurfaceSize.Y;
float lineHeight = 30 * surfaceData.textSize;
float currentY = position.Y - surfaceData.viewPortOffsetY;
float remainingHeight = screenHeight - currentY;
int availableDataLines = Math.Max(1, (int)(remainingHeight / lineHeight));

// Apply user-configured max list lines (0 = no limit)
if (maxListLines > 0)
    availableDataLines = Math.Min(availableDataLines, maxListLines);

// Apply scrolling
int totalDataLines = categoryItems.Count;
int startIndex = 0;
if (toggleScroll && totalDataLines > 0)
{
    int normalizedOffset = ((scrollOffset % totalDataLines) + totalDataLines) % totalDataLines;
    startIndex = normalizedOffset;
}

int linesDrawn = 0;
for (int i = 0; i < totalDataLines && linesDrawn < availableDataLines; i++)
{
    int itemIndex = (startIndex + i) % totalDataLines;
    // Draw categoryItems[itemIndex]
    position += surfaceData.newLine;
    linesDrawn++;
}
```

---

#### Known Issues & Solutions

**Update10 Scroll Speed Bug**
- **Problem:** Using `ticksSinceLastScroll++` on an Update10 screen makes `scrollSpeed=60` take ~10 seconds instead of ~1 second.
- **Root Cause:** Update10 fires once every 10 game ticks. Incrementing by 1 means you're measuring calls, not ticks.
- **Fix:** Always increment by the tick interval: `ticksSinceLastScroll += 10` for Update10, `+= 100` for Update100, `+= 1` for Update1.

**Multi-Category Space Calculation**
- **Problem:** Categories calculating space from screen top caused overlapping content.
- **Fix:** Calculate remaining space from current drawing position: `float currentY = position.Y - surfaceData.viewPortOffsetY;`

**Item Type Collisions in Items Screen**
- **Problem:** Items with same SubtypeId but different TypeId (e.g., `ConsumableItem_Fruit` vs `SeedItem_Fruit`) caused doubled counts.
- **Fix:** Use composite keys: `string cargoKey = $"{typeId}_{subtypeId}";`

---

**Update100 Tick Increment**
- **Rule:** DetailedInfo uses `ScriptUpdate.Update100` (fires every 100 game ticks). Must use `ticksSinceLastScroll += 100`, NOT `+= 10`.
- `scrollSpeed=60` on an Update100 screen means it scrolls on every single update call (~1.67s/line). Users may want `ScrollSpeed=200`+ for reading screens.

**BuildInfo Custom Info Not Accessible from Scripts**
- **Problem:** BuildInfo's extra block details (Type, Inventory, Ammo, etc.) don't appear in `block.DetailedInfo` when read from a script.
- **Root cause:** BuildInfo subscribes to `block.AppendingCustomInfo` only while the player has that block open in the terminal panel (`newBlock.AppendingCustomInfo += CustomInfo` on terminal select, `oldBlock.AppendingCustomInfo -= CustomInfo` on deselect). When the terminal is closed, BuildInfo is unsubscribed — so calling `block.RefreshCustomInfo()` from a script fires no callbacks and `CustomInfo` stays empty.
- **Conclusion:** Cannot reliably display BuildInfo data from a text surface script. The feature was investigated and removed from DetailedInfo.

**MaxListLines=0 for Single-List Screens**
- **Rule:** Single-list screens (Container, Farming, DetailedInfo, GridInfo, AirlockMonitor) should pass `maxListLines: 0` to `AppendScrollingConfig()`. The helper now skips writing `MaxListLines` entirely when the value is 0, keeping the config clean.
- Multi-category screens (Weapons, Power, Production, etc.) should pass their actual `maxListLines` field.

#### ConfigHelpers.AppendScrollingConfig() Reference

```csharp
public static void AppendScrollingConfig(
    StringBuilder sb,
    string sectionPrefix,       // e.g. "ITEMS", "POWER" — used in the ; [ X - SCROLLING OPTIONS ] header
    bool toggleScroll = false,
    bool reverseDirection = false,
    int scrollSpeed = 60,
    int scrollLines = 1,
    int maxListLines = 5  // Only for multi-category screens
)
```

CustomData options:
- `ToggleScroll` — Enable/disable (default: false)
- `ReverseDirection` — Scroll direction (default: false = up)
- `ScrollSpeed` — Ticks between scrolls (60 ≈ 1 second)
- `ScrollLines` — Lines per scroll step (default: 1)
- `MaxListLines` — Max items per category (default: 5, 0 = unlimited) *[Multi-category only]*

---

## Mod Adjuster Mods

Mods in the `[Mod Name] [Mod Adjuster For SG]` pattern are balance/compatibility adjustments for third-party mods to work with the Sturmgrenadier core mods.

| Mod | Notes |
|---|---|
| Artillery MKII Turret - Goliath | Weapon balance adjustment |
| Dense Colorable Solar Panels | Power output adjustment |
| Federal Industrial - Utilities | Compatibility patch |
| Isy's Dense Solar Panels | Power output adjustment |
| Life'Tech - Algaetechnology | Farming/resource adjustment |
| ModCubeBlocks Refinery x10 | Refinery speed adjustment |
| ModCubeBlocks Upgrade Module | Module balance adjustment |
| More Engineer Characters | Character mod compatibility |
| More Wind Turbines | Wind power output adjustment |
| [Mafoo] More Batteries | Battery capacity/charge rate adjustment |

---

## Sturmgrenadier Core Mods

The SG Core mods form the base gameplay overhaul. Individual notes go here as they accumulate.

| Mod | Purpose |
|---|---|
| Sturmgrenadier Core Mod | Base overhaul — core gameplay changes |
| Sturmgrenadier Core Power | Power system changes |
| Sturmgrenadier Core Production | Production/crafting changes |
| Sturmgrenadier Core Survival | Survival mechanic changes |
| Sturmgrenadier Core Vanilla Combat | Combat balance changes |
| Not Just For Looks | Cosmetic/decorative block changes |

---

*Add new mod-specific notes under their respective sections as they accumulate.*

---

## Session Log

### 2026-03-13 — Scroll Timer Bug Fix
- **Bug:** `ticksSinceLastScroll++` on Update10 screens — incremented by 1 per call instead of 10, making scrolling run at 1/10th intended speed
- **Fix:** Changed to `ticksSinceLastScroll += 10` across all scrolling screens
- **Fix:** Default `scrollSpeed` corrected from `5` → `60` (with `++` bug, `5` meant ~0.8s; with `+= 10` fix, `5` would fire every frame since 10 ≥ 5 immediately)
- **Screens fixed:** Ammo, Components, Ingots, Ores (Items and Power were already correct)
- **Also added:** Dedicated server guard to `DetailedInfo.cs` Run()

### 2026-03-15 (Session 2) — Weapons, DetailedInfo, GridInfo Done + Airlock Researched

- **Weapons scrolling completed:** All four detailed draw methods updated (turrets, interior turrets, cannons, custom turret controllers). Each uses linesPerEntry=3 with shared scrollOffset/maxListLines. Previous session left DrawCannonsDetailedSprite with a missing `}` for the scope block and missing `slotsDrawn++` — fixed.
- **DetailedInfo scrolling implemented:** Builds flat display line list first (applying word wrap), then scrolls through it. Update100 screen — `ticksSinceLastScroll += 100`. No MaxListLines.
- **BuildInfo investigation:** Attempted to expose BuildInfo's extra block data via `RefreshCustomInfo()` + `CustomInfo`. Found that BuildInfo only subscribes to `AppendingCustomInfo` while the block is selected in the terminal panel — unsubscribes on deselect. Calling `RefreshCustomInfo()` from a script fires nothing. Feature removed from DetailedInfo. See Known Issues.
- **GridInfo scrolling implemented:** Jump drives moved to pin above connector list (always visible). Connector list pre-filtered (vessels hide unconnected), then scrolls with Approach 1. Viewport-aware remainingHeight calculation using `(mySurface.TextureSize.Y - mySurface.SurfaceSize.Y) / 2f`.
- **AppendScrollingConfig fix:** Now skips writing `MaxListLines` entirely when `maxListLines == 0`. Single-list screens (Container, Farming, DetailedInfo, GridInfo) pass `0` explicitly.
- **Farming compile bug caught:** `maxListLines` variable referenced in `AppendScrollingConfig` call after field was removed — silent compile failure if cached. Fixed by passing `0` literally.
- **AirlockMonitor fully understood:** Door list (1 line/door) + single pressure bar from first matching air vent. SearchId matches block name prefix (default: "Airlock"). Straightforward Approach 1, door list only. Ready to implement next session.

### 2026-03-15 — Multi-Line Entry Scrolling Discovery + Container & Farming Done

- **Container scrolling implemented:** Single flat list. Each entry = 1 line (cargo containers) or 1+N lines (production blocks with multiple inventories). Built a flat `rowBlocks`/`rowIndices` list to handle mixed heights. No MaxListLines — screen height caps naturally. Scroll by row index.
- **Farming scrolling implemented:** Fixed section (header, ice bar, water bar, H2O production) always visible. Farm plot list scrolls. Each entry = **4 lines** (name, badge+hydration, water+growth, blank spacer). `linesPerEntry=4`, `availableSlots = availableDataLines / 4`. No MaxListLines.
- **Key discovery:** Multi-line-per-entry scrolling works. Any screen with uniform entry height can scroll — scroll unit is "per entry" not "per line". This overturns the earlier assumption that only single-line-per-item screens were compatible.
- **Weapons screen re-evaluated:** Now planned as Approach 2 per-category scrolling with shared scroll offset. Each weapon entry = **3 lines** in detailed mode. Compact mode already works; only detailed draw methods need updating.
- **Bug caught during implementation:** `AppendScrollingConfig` call in Container was still passing `maxListLines` variable after the field was removed — caused compile error. Fixed by dropping the argument (uses default).
- **Typo debugging:** Farming ToggleScroll=True was entered as "Ttrue" — `ToBoolean("Ttrue")` returns false silently. Scrolling appeared broken. Lesson: always check manually-typed config values for typos first.

### 2026-03-23 — InfoLCD Fill Bar Oscillation Bug Fix

- **User report:** Fill bar on Items/Ores/Ingots/Components/Ammo screens bouncing rapidly between different percentages (~6Hz). Started after scrolling was added. Could not reproduce in-house — waiting for user's CustomData to confirm root cause.
- **Bug 1 — Items.cs `scrollOffset` field mutation:** Draw methods (`DrawAllKnownSprite`, `DrawAllAvailableSprite`) were writing back `scrollOffset = ((scrollOffset % totalDataLines) + totalDataLines) % totalDataLines` instead of using a local `normalizedOffset` variable. Ores.cs was already correct. Fixed to match canonical pattern.
- **Bug 2 — `unknownItemDefinitions.Clear()` placement (Ores, Ingots, Components, Ammo, Cargo):** `Clear()` was called inside `UpdateContents()` instead of at the top of `Run()` before `LoadConfig()`. For modded items not in `MahDefinitions`, this caused `minAmount` to oscillate between the user's configured value and the hardcoded default (1000) on every Update10 tick — fill bar bounces between two values at 6Hz. Items.cs already had the correct placement. Fixed all five affected screens.
- **Scan finding:** Weapons.cs has `unknownItemDefinitions` declared and searched but nothing is ever added to it — dead code, harmless, left as-is.
- **Patch released to Workshop.** Waiting on reporter to confirm fix.

### 2026-03-18 (Session 2) — Image Converter v1.3 + Mipmap Quality Investigation

#### Universal Image Converter v1.3

- **Background color picker added (Image to DDS):** Swatch + hex label in Output Settings row 4. `_on_pick_bg()` uses `colorchooser.askcolor`. `bg_color` passed through to `convert_image()` and both compose functions.
- **Setup screen rewritten:** Matches audio converter pattern — live texconv detection with green ✓ / red ✗ badge. Shows install instructions and download link only when not found. Full "HOW TO ADD A TOOL TO PATH" 10-step card added.
- **Version bumped:** `screen_home.py` updated to v1.3.
- **Format corrected:** texconv output changed from `BC7_UNORM` → `BC7_UNORM_SRGB` (DXGI 99). This matches what Keen ships in vanilla SE LCD textures.
- **DXT5 fallback encoder fixed:** Color endpoint selection was using per-channel max/min (synthetic colors that don't exist in the block). Changed to luminance-based pixel selection — picks actual brightest/darkest pixels as endpoints. Reduces banding at lower mip levels.

#### Mipmap Quality Investigation — Root Cause Found

- **Symptom:** Images converted with texconv (BC7) looked fine close-up but turned to "mud" at any game distance. The same source image exported from Paint.NET looked fine at distance.
- **Root cause:** texconv uses **premultiplied alpha weighting** when generating mip levels. With our `alpha=1` (value 1 out of 255 ≈ 0.4% opacity), texconv multiplied all RGB values by ~0 during downsampling. Mip 0 reads from the PNG directly (correct), but all lower mips had RGB ≈ 0 (near-black mud).
- **Why Paint.NET was fine:** Paint.NET's DDS plugin uses straight alpha (not premultiplied) for mip generation. Alpha=255 also avoids the problem since premultiplied math is a no-op at full opacity.
- **Fix:** Added `-sepalpha` flag to texconv command — generates RGB mip channels independently from alpha, bypassing premultiplied alpha weighting entirely. Colors now average correctly at every mip level, alpha=1 preserved throughout.
- **Additional improvements applied during investigation:**
  - `-if CUBIC` — bicubic mip filter (better than default FANT for photographic content)
  - `-bc x` — maximum quality BC7 compression
  - `-sepalpha` — the actual root cause fix
- **Lesson learned:** Any tool that generates DDS mipmaps may use premultiplied alpha internally. With SE's inverse-emissivity alpha=1 (near-zero), this is catastrophic for mip quality. Always use `-sepalpha` or equivalent when alpha has a non-transparency semantic meaning.

#### Space Engineers Skill

- **Check #4 updated:** Workshop directory detection now looks for numbered subfolder structure (10+ numeric subfolders). Blocks catalogue creation if workshop directory not found in workspace.
- **Check #5 added:** AskUserQuestion on startup — asks what kind of SE project the user is working on (Mod project / Mod Adjuster / PB Script / Torch or Pulsar plugin).
- **README updated:** Workshop directory row marked as required for catalogue. "What Claude Will Do Automatically" section updated.

---

### 2026-03-18 — Claude Engineers LCD Mod + Image Converter Bug Fix

- **Claude Engineers mod created:** LCD texture mod with two images (`claude_engineer01.dds`, `claude_engineer02.dds`). `LCDTextures.sbc` written with SubtypeIds `Claude Engineer 01` / `Claude Engineer 02`. DDS files in both `Textures/Models/` and `Textures/Sprites/`. Tested in-game — working.
- **Universal Image Converter v1.2.1:** Fixed two bugs in the Custom size path:
  1. `_load_and_compose_custom` was not creating a full-size canvas — returned the scaled image at whatever size it ended up, causing SE to stretch it to fill the panel (squished result).
  2. UI was locking height=width when "Preserve Aspect Ratio" was checked, preventing non-square custom targets (e.g. 512×1024).
  - Fix: always output a canvas exactly `target_w × target_h`; letterbox source when preserve aspect is on. Height field is now always independently editable.
- **AirlockMonitor scrolling:** Still pending — next up for InfoLCD Apex Update.

### 2026-03-18 (Session 3) — Universal Audio Converter UI Polish

#### Audio Editor — Selection Button Icons
- Added `|` brackets to Play Selection, Select All, and Clear Selection button icons so they visually read as selection-scoped actions (e.g. `|▶|`, `|⊞|`, `|✕|`).

#### Audio Editor — File Open Behavior
- Changed file-open to start with **no selection** (previously selected all by default). `_sel_start` and `_sel_end` set to `0`; calls `clear_selection()` instead of `select_all()`.

#### Audio Editor — Info Strip
- Removed filename from the waveform info strip. Strip now shows only numeric data: duration, sample rate, channel count. Filename already visible in the header label above the waveform — duplication removed.

#### Audio Editor — Info Popup Quality Upgrade
- Replaced the plain messagebox info popup with a proper `AudioEditorReferenceWindow` class in `se_audio_theme.py`. Matches the image converter's reference window: dark-themed resizable Toplevel (560×580), styled Text widget with `section`/`op_name`/`op_desc` tags, CLOSE button footer.
- Info button upgraded to `ttk.Button` with `Info.TButton` style (ⓘ). Singleton pattern — re-opens and lifts existing window instead of spawning duplicates.

#### Audio Editor — `_apply()` Selection Preservation
- **Problem:** Every edit operation (trim, fade, normalize, etc.) was calling `select_all()` after applying, which forced a full waveform selection the user didn't ask for.
- **Fix:** `_apply()` now saves the previous selection before the operation, then:
  - If there **was** a selection **and** audio length is unchanged → restores the clamped selection via new `set_selection_frames(start, end)` method
  - Otherwise (no selection, or length changed by the operation) → calls `clear_selection()`
- **New waveform method:** Added `set_selection_frames(start, end)` to the `WaveformWidget` class — converts frame indices to pixel positions and fires the selection event.

---

### 2026-03-19 — SE Skill Expansion + README + Catalogue Updates

#### Root README
- `mods/README.md` was essentially a stub. Rewrote as a proper landing page covering: InfoLCD, Claude Engineers, Not Just For Looks (both variants), Sturmgrenadier Core series, Mod Adjusters, Universal Image Converter (v1.3), Universal Audio Converter (pre-release), and Scripts.

#### Space Engineers Skill — Three New Reference Files
All three files written from local workshop mod data + web research, then synced to VS Code projects copy.

- **`DLC_CATALOGUE.md`** — Full listing of all 20 SE DLC packs (SubtypeIds + AppIds + free/paid content breakdown), sourced directly from `DLCs.sbc`. Includes patch detection instructions: on skill load, compare `DLCs.sbc` SubtypeIds against known list; if new ones appear, prompt user to research new content.
- **`MES.md`** — Modular Encounters System modding guide. Covers: how MES reads `[Key:Value]` tags from `<Description>` fields, all profile types (SpawnGroup, Behavior, Autopilot, Trigger, Action, SpawnConditions) with real SBC examples sourced from Robot Raider Pods and Zombie Attack mods. Key note: MES disables vanilla cargo ship/encounter/creature spawners on load.
- **`AI_ENABLED.md`** — AI Enabled modding guide. Covers: bot definition SBC format, character SBC, MES integration via `[BotProfiles:]` and `[AiEnabledReady:true]`, child mods (Crew Enabled, Infestation Enabled, Zombie Attack). Key note: Zombie Attack has been removed from Steam and is incompatible with current SE.

#### SKILL.md Updates
- **Check #5 (new):** DLC/patch detection — reads `DLCs.sbc` and compares SubtypeIds against `DLC_CATALOGUE.md` on every skill load.
- **Check #6 (was #5):** "What Are We Working On?" — added **MES / AI Enabled mod** as an explicit project type option.
- Key Reference Files table updated with `DLCs.sbc`, MES, and AI Enabled workshop IDs.
- Supporting Reference Files updated with links to all three new files.

#### MOD_CATALOGUE.md Refresh
- Categories expanded: added **MES**, **AI Enabled**, **Scenario** (split out from the old `NPC/AI` catch-all).
- Detection rules added to SKILL.md: `Profiles/` subfolder or `[Modular Encounters SpawnGroup]` in SBC → MES; `AnimalBotDefinition` or `AnimationControllers/` → AI Enabled.
- 11 entries re-identified and re-categorized (names were wrong — pulled from wrong files during original catalogue build):
  - `Disposable Beacon` → **Modular Encounters System** (MES)
  - `Ai Enabled` → **Bot_spawner** (AI Enabled — dependency only)
  - `AiEnabled Combat Bot Material` → **AI Enabled** (framework)
  - `Raiders` → **Robot Raider Pods** (MES)
  - `Infestation` → **Infestation Enabled** (AI Enabled)
  - `Stuff n Things` → **Planet Creature Spawner** (MES)
  - `Grinder Engineers (Spawn)` → **Populated Worlds** (AI Enabled)
  - `FAF Founder` → **Ares at War (Scenario)** — this is a world save, not a mod
  - `Configuration Script` → **Crew Enabled** (AI Enabled)
  - `Credits Display` → **NPC Programming Extender** (MES)
  - `ARYLYN Drive Systems` (NPC/AI) → kept, but category review noted
- 3 entries added (were in workshop folder but missing from catalogue): **Independent Contractors**, **Orks**, **Reavers** — all MES encounter packs.
- Mod Groups section updated with proper MES and AI Enabled groupings.
- Total: 295 → 298 mods. Date updated to 2026-03-19.

---

### 2026-03-19 (Session 4) — Universal Audio Converter Stereo Waveform + Channel UI

#### Waveform — Orange Selection Highlight
- Selected region of the waveform now turns orange; unselected portions stay grey.
- Selection tint (stippled orange rectangle) drawn behind the waveform lines so lines sit on top.
- Replaced the old blue stipple overlay with orange throughout.

#### Waveform — Stereo Split View
- Stereo files now show two waveform lanes: R on top, L on bottom, divided by a `BORDER` line.
- Mono files keep the single full-height waveform view.
- Lane height is half of `WAVEFORM_HEIGHT` (80px each at default 160px).

#### Channel Toggle Buttons (R / L)
- Two toggle buttons appear to the left of the waveform when a stereo file is loaded; hidden for mono.
- Active (on) = orange background / dark text. Inactive (off) = PANEL background / muted text.
- Buttons occupy a fixed 32px-wide frame packed `before=self._waveform` using `pack_propagate(False)`.
- Toggling a channel controls whether that lane gets the orange selection highlight.
- Buttons auto-reset to both-on when a new stereo file loads or a channel op changes the file to stereo.

#### SWAP / EXTRACT / SOLO Button State
- Swap, Extract, and Solo buttons now disable automatically when a mono file is loaded.
- Re-enable when a stereo file is loaded or restored via undo.
- No-file state keeps all buttons active (as before).

#### Extract / Solo Consolidated
- Removed separate Extract L, Extract R, Solo L, Solo R buttons.
- Replaced with single `⊟ EXTRACT` and `◎ SOLO` buttons that read the L/R channel toggle state.
- Both active or both inactive → blocked with a log message.
- Solo is selection-aware: with a time selection, only that region is affected; no selection = whole file.

#### Bug Fix — Waveform Selection Lost After Layout Change
- Root cause: packing/unpacking the channel button frame caused tkinter to fire multiple `Configure` events on the canvas, sometimes with a transient small width. Simple clamping would shrink `_sel_end_px` during the transient, and the final event wouldn't restore it.
- Fix: `_on_resize` now proportionally scales both selection pixel positions on every resize, so any sequence of transient events correctly resolves to the final canvas width.

#### Style Guide
- Added **Toggle Button** pattern (tk.Button, orange/BG when active, PANEL/MUTED when inactive).
- Added **Canvas Widget** section: waveform colour conventions table (background, centre line, unselected/selected waveform, tint, handles, playhead) + the proportional resize scaling gotcha with code example.

---

### 2026-03-14 — CustomData Section Header Standardization
- **Change:** All CustomData section headers now follow consistent `; [ SCREENNAME - CATEGORY ]` pattern
- **Scrolling headers:** Were mixed (`; [ SCROLLING OPTIONS ]`, `; [ SCREENNAME - SCROLLING OPTIONS ]`) — now all use `; [ SCREENNAME - SCROLLING OPTIONS ]`
- **Ordering fix:** DamageMonitor, DoorMonitor, LifeSupport, Production had SCROLLING at the end — moved before LAYOUT OPTIONS (correct order: GENERAL → SCROLLING → LAYOUT → everything else)
- **AppendScrollingConfig:** Added `sectionPrefix` parameter so the helper generates the correct per-screen header
- **Also fixed:** CLAUDE.md was accidentally committed to git — added `.gitignore` with `CLAUDE.md` at repo root and subfolder level
