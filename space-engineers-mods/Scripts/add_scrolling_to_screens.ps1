Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

<#
.SYNOPSIS
    Adds scrolling functionality to Ingots, Ores, and Ammo LCD screens
.DESCRIPTION
    This script applies the same scrolling implementation that was added to Components screen
    to the Ingots, Ores, and Ammo screens. These screens have identical structure.
#>

$screenMappings = @{
    "Ingots" = @{
        File = "MahLCDs_Summary_Ingots.cs"
        ClassName = "LCDIngotsSummaryInfo"
        ConfigSection = "SettingsIngotsSummary"
        DisplayName = "INGOTS"
        ItemType = "Ingots"
    }
    "Ores" = @{
        File = "MahLCDs_Summary_Ores.cs"
        ClassName = "LCDOresSummaryInfo"
        ConfigSection = "SettingsOresSummary"
        DisplayName = "ORES"
        ItemType = "Ores"
    }
    "Ammo" = @{
        File = "MahLCDs_Summary_Ammo.cs"
        ClassName = "LCDAmmoSummaryInfo"
        ConfigSection = "SettingsAmmoSummary"
        DisplayName = "AMMO"
        ItemType = "Ammo"
    }
}

$basePath = "c:\Users\Chris Carpenter\My-Space-Engineers-Mods\InfoLCD - Apex Update\Data\Scripts\SG"

Write-Host "Adding scrolling to Ingots, Ores, and Ammo screens..." -ForegroundColor Cyan

foreach ($screenName in $screenMappings.Keys) {
    $screen = $screenMappings[$screenName]
    $filePath = Join-Path $basePath $screen.File
    
    Write-Host "`nProcessing $screenName screen ($($screen.File))..." -ForegroundColor Yellow
    
    if (-not (Test-Path $filePath)) {
        Write-Warning "File not found: $filePath"
        continue
    }
    
    $content = Get-Content $filePath -Raw
    
    # 1. Add scrolling config output (after UseColors config)
    Write-Host "  - Adding scrolling config output..." -ForegroundColor Gray
    $pattern1 = '(ConfigHelpers\.AppendUseColorsConfig\(sb, surfaceData\.useColors\);)\s+sb\.AppendLine\(\);\s+sb\.AppendLine\("; \[ ' + $screen.DisplayName + ' - LAYOUT OPTIONS \]"\);'
    $replacement1 = '$1

            sb.AppendLine();
            ConfigHelpers.AppendScrollingConfig(sb);

            sb.AppendLine("; [ ' + $screen.DisplayName + ' - LAYOUT OPTIONS ]");'
    $content = $content -replace $pattern1, $replacement1
    
    # 2. Add scrolling state fields (after gridMass)
    Write-Host "  - Adding scrolling state fields..." -ForegroundColor Gray
    $pattern2 = '(bool configError = false;\s+bool compactMode = false;\s+bool isStation = false;\s+Sandbox\.ModAPI\.Ingame\.MyShipMass gridMass;)'
    $replacement2 = '$1

        // Scrolling state
        bool toggleScroll = false;
        bool reverseDirection = false;
        int scrollSpeed = 60;
        int scrollLines = 1;
        int scrollOffset = 0;
        int ticksSinceLastScroll = 0;'
    $content = $content -replace $pattern2, $replacement2
    
    # 3. Add scrolling config loading (after UseSubtypeId)
    Write-Host "  - Adding scrolling config loading..." -ForegroundColor Gray
    $pattern3 = '(if \(config\.ContainsKey\(CONFIG_SECTION_ID, "UseSubtypeId"\)\)\s+surfaceData\.useSubtypeId = config\.Get\(CONFIG_SECTION_ID, "UseSubtypeId"\)\.ToBoolean\(\);)\s+(surfaceData\.newLine = new Vector2)'
    $replacement3 = '$1

                    // Scrolling options (optional; default false/60/1)
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ToggleScroll"))
                        toggleScroll = config.Get(CONFIG_SECTION_ID, "ToggleScroll").ToBoolean(false);
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ReverseDirection"))
                        reverseDirection = config.Get(CONFIG_SECTION_ID, "ReverseDirection").ToBoolean(false);
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ScrollSpeed"))
                        scrollSpeed = Math.Max(1, config.Get(CONFIG_SECTION_ID, "ScrollSpeed").ToInt32(60));
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ScrollLines"))
                        scrollLines = Math.Max(1, config.Get(CONFIG_SECTION_ID, "ScrollLines").ToInt32(1));

                    $2'
    $content = $content -replace $pattern3, $replacement3
    
    # 4. Add scroll update logic in Run() (after UpdateContents)
    Write-Host "  - Adding scroll update logic in Run()..." -ForegroundColor Gray
    $pattern4 = '(LoadConfig\(\);\s+UpdateInventories\(\);\s+UpdateContents\(\);)\s+(var myFrame = mySurface\.DrawFrame\(\);)'
    $replacement4 = '$1

            // Update scroll offset if scrolling is enabled
            if (toggleScroll)
            {
                ticksSinceLastScroll++;
                if (ticksSinceLastScroll >= scrollSpeed)
                {
                    ticksSinceLastScroll = 0;
                    if (reverseDirection)
                        scrollOffset -= scrollLines;
                    else
                        scrollOffset += scrollLines;
                    
                    // Scroll offset will wrap around in the draw methods based on actual item count
                }
            }
            else
            {
                // Reset scroll when disabled
                scrollOffset = 0;
                ticksSinceLastScroll = 0;
            }

            $2'
    $content = $content -replace $pattern4, $replacement4
    
    # Save the file
    Set-Content -Path $filePath -Value $content -NoNewline
    Write-Host "  ✓ Basic scrolling infrastructure added" -ForegroundColor Green
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Phase 1 complete: Config, state, and Run() updates applied" -ForegroundColor Green
Write-Host "Phase 2 required: Update DrawAllKnownSprite and DrawAllAvailableSprite methods" -ForegroundColor Yellow
Write-Host "  (These methods are too complex for regex - need manual update or targeted script)" -ForegroundColor Yellow
Write-Host "========================================`n" -ForegroundColor Cyan
