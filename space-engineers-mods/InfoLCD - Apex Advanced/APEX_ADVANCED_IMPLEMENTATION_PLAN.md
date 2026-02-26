# InfoLCD Apex Advanced - Implementation Plan

**Date Created**: December 2, 2025  
**Mod Target**: Apex Advanced! (Full Release)

---

## Key Changes in Apex Advanced!
- **Water → HydroSolution** (gas type, stored in HydroSolution tanks)
- **Irrigation System** now consumes HydroPellets → produces HydroSolution
- **Composter** (new block) creates HydroPellets from Ice + Organic
- **Composter** also creates Organic from food items

---

## Testing Fixes Applied (December 2, 2025)

### Farming Screen
- [x] Changed "H2O" bar label to "HydS" (for HydroSolution)
- [x] Changed "H2O Production" label to "HydS Production"

### Production Screen
- [x] Hidden Composters from assembler list (excluded via IsComposter check)
- [x] Composters still appear on Systems screen under production category (as intended)

### Damage Control Screen
- [x] Added Composter detection to production category (shows damaged composters)

---

## Implementation Steps

### **Step 1: Item Definitions & Base Functions**

#### 1a - Uncomment Apex Advanced Items
- [x] Uncomment `HydroPellets` (Ore, 0.37L)
- [x] Uncomment `Organic` (Ore, 0.37L)
- [x] Uncomment Apex Advanced consumables: BioPaste, SparklingWater, MycoBoost, FruitTea
- [x] **DELETE** all Engineered Coffee mod lines (Coffee, CoffeeBean, MealPack_RoastedCoffee, MealPack_CoffeeCake, MealPack_CoffeeBrisket, etc.)
- [x] Verify volumes and minAmount values match Apex Advanced! definitions

#### 1b - HydroSolution Calculation Function
- [x] Convert existing water volume function to HydroSolution calculation
- [x] Verify HydroSolution tanks are detected (code already checks both Water and HydroSolution)
- [x] Update gas type recognition to include HydroSolution in producer/consumer detection
- [x] Update Irrigation System recognition (consumes HydroPellets, produces HydroSolution)

#### 1c - Tracking Functions
- [x] Create `GetGridOrganicData()` function (similar to `GetGridIceData`)
  - Input: block list, organic item volume
  - Output: current volume, max volume, item count
- [x] Create `GetGridHydroPelletsData()` function
  - Input: block list, pellets item volume
  - Output: current volume, max volume, item count
- [x] Add GridOrganicData and GridHydroPelletsData structs

---

### **Step 2: Non-Farming Screens**

#### 2a - Ores/Ingots Screens
- [x] Do NOT show Organics on Ore screen (reserved for farming)
- [x] Do NOT show HydroPellets on Ore screen (reserved for farming)
- [x] Verify Ice still shows correctly

#### 2b - Irrigation System Updates
- [x] Recognize Irrigation System as HydroSolution producer (like O2/H2 generator)
- [x] Track operational status
- [x] Display HydroPellets consumption/availability
- [x] Show HydroSolution production rate

---

### **Step 3: Farming Screen**

#### 3a - Resource Tracking Updates
- [x] Replace Water tracking with HydroSolution tracking
- [x] Add Organics tracking (for composting)
- [x] Add HydroPellets tracking (for irrigation)
- [x] Update display to show all three resources
- [x] Verify farm plot integration still works

---

### **Step 4: Composter Block (TBD - Requires In-Game Testing)**

#### 4a - Basic Tracking
- [x] Add show/hide composter option
- [x] Track composter operational status
- [x] Recognize block as Assembler type with "Composting" blueprint class
- [x] Show active/total composter count

---

## Notes
- InfoLCD Apex Advanced is **completely separate** from InfoLCD Apex Update
- No shared code changes between the two mods
- All edits isolated to Apex Advanced version
- HydroSolution tanks may already work with existing water detection logic (verify)

---

## Testing Checklist
- [x] Ice tracking still works correctly
- [x] HydroSolution tanks detected and volume calculated
- [x] Irrigation System recognized and tracked
- [x] Farm plots still work with HydroSolution
- [x] Composter block recognized
- [x] All Apex Advanced food items track correctly
- [x] No Engineered Coffee items remain
