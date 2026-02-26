<#
verify_cargo_volumes.ps1
Compares the volumes defined in InfoLCD_Config.cs cargoItemDefinitions against the reference CSV extraction(s).
Outputs a mismatch report CSV and a human-readable summary.
#>
[CmdletBinding()]
param(
    [string]$ConfigPath = "../InfoLCD - Apex Update/InfoLCD_Config.cs",
    [string]$ReferenceBaseCsv = "../[REFERENCE FILES]/Refernece Sheets/reference_files_definitions.csv",
    [string]$ReferenceModsCsv = "../[REFERENCE FILES]/Refernece Sheets/reference_mods_definitions.csv",
    [string]$OutCsv = "../[REFERENCE FILES]/Refernece Sheets/cargo_volume_verification.csv",
    [switch]$ReportOrphans,
    [string]$OutOrphansCsv = "../[REFERENCE FILES]/Refernece Sheets/cargo_volume_orphans.csv",
    [string]$ExcludeTypeIds = "treeobject", # comma-separated list of typeIds to exclude from orphan reporting
    [string]$ExcludeSubtypes = "cubeplaceritem" # comma-separated list of subtypeIds to exclude from orphan reporting
)

function Resolve-PathSafe {
    param([string]$Path)
    return [IO.Path]::GetFullPath((Join-Path $PSScriptRoot $Path))
}

$ConfigPath = Resolve-PathSafe $ConfigPath
$ReferenceBaseCsv = Resolve-PathSafe $ReferenceBaseCsv
$ReferenceModsCsv = Resolve-PathSafe $ReferenceModsCsv
$OutCsv = Resolve-PathSafe $OutCsv
if ($ReportOrphans) { $OutOrphansCsv = Resolve-PathSafe $OutOrphansCsv }

if (!(Test-Path $ConfigPath)) { throw "Config file not found: $ConfigPath" }
if (!(Test-Path -LiteralPath $ReferenceBaseCsv)) { throw "Reference base CSV not found: $ReferenceBaseCsv" }
if (!(Test-Path -LiteralPath $ReferenceModsCsv)) { Write-Verbose "Mods CSV missing (ok)" }

# 1. Parse Config.cs for cargo definitions
$definitions = @()
Get-Content $ConfigPath | ForEach-Object {
    $line = $_.Trim()
    if ($line -match 'new\s+CargoItemDefinition') {
        # Extract key fields via regex for robustness
        $typeId = if ($line -match 'typeId\s*=\s*"([^"]+)"') { $Matches[1] } else { $null }
        $subtypeId = if ($line -match 'subtypeId\s*=\s*"([^"]+)"') { $Matches[1] } else { $null }
        $volume = if ($line -match 'volume\s*=\s*([0-9]+\.?[0-9]*)f') { [float]$Matches[1] } else { $null }
        if ($typeId -and $subtypeId -and $volume -ne $null) {
            $definitions += [pscustomobject]@{ TypeId=$typeId; SubtypeId=$subtypeId; ConfigVolume=$volume }
        }
    }
}

if ($definitions.Count -eq 0) { throw "No CargoItemDefinition entries parsed from $ConfigPath" }

# 2. Load reference volumes (aggregate both CSVs)
function Get-ReferenceCsvMap {
    param([string]$Path)
    if (!(Test-Path -LiteralPath $Path)) { return @{} }
    $map = @{}
    Import-Csv -LiteralPath $Path | ForEach-Object {
        $typeId = $_.TypeId
        $subtypeId = $_.SubtypeId
        $volRaw = $_.Volume
        if ([string]::IsNullOrWhiteSpace($volRaw)) { return }
        if (-not $typeId -or -not $subtypeId) { return }
        if ($volRaw -notmatch '^[0-9]+(\.[0-9]+)?$') { return }
        $key = ("{0}|{1}" -f $typeId,$subtypeId).ToLower()
        # Prefer first unless overwritten later (mods override base)
        $map[$key] = [float]$volRaw
    }
    return $map
}
$refBase = Get-ReferenceCsvMap -Path $ReferenceBaseCsv
$refMods = Get-ReferenceCsvMap -Path $ReferenceModsCsv
$allRef = @{}
# Merge base then mods (mods override)
foreach ($k in $refBase.Keys) { $allRef[$k] = $refBase[$k] }
foreach ($k in $refMods.Keys) { $allRef[$k] = $refMods[$k] }

# 3. Compare
$mismatches = @()
foreach ($def in $definitions) {
    $key = ("{0}|{1}" -f $def.TypeId,$def.SubtypeId).ToLower()
    if ($allRef.ContainsKey($key)) {
        $refVol = $allRef[$key]
        $diff = $def.ConfigVolume - $refVol
        $status = if ([math]::Abs($diff) -lt 0.0001) { 'Match' } else { 'Mismatch' }
        if ($status -eq 'Mismatch') {
            $mismatches += [pscustomobject]@{ TypeId=$def.TypeId; SubtypeId=$def.SubtypeId; ConfigVolume=$def.ConfigVolume; ReferenceVolume=$refVol; Difference=$diff }
        }
    } else {
        $mismatches += [pscustomobject]@{ TypeId=$def.TypeId; SubtypeId=$def.SubtypeId; ConfigVolume=$def.ConfigVolume; ReferenceVolume=$null; Difference=$null }
    }
}

# 4. Output CSV
New-Item -ItemType Directory -Force -Path ([IO.Path]::GetDirectoryName($OutCsv)) | Out-Null
$mismatches | Sort-Object TypeId,SubtypeId | Export-Csv -LiteralPath $OutCsv -NoTypeInformation

Write-Host "Analyzed $($definitions.Count) cargo entries. Mismatches: $($mismatches.Count)." -ForegroundColor Cyan
if ($mismatches.Count -gt 0) {
    Write-Host "Top 15 mismatches:" -ForegroundColor Yellow
    $mismatches | Select-Object -First 15 | Format-Table -AutoSize | Out-String | Write-Host
    Write-Host "Full mismatch report written: $OutCsv" -ForegroundColor Green
} else {
    Write-Host "All cargo volumes match references." -ForegroundColor Green
}

# 5. Suggest patch snippet for Prototech (example) if any mismatches include those
$proto = $mismatches | Where-Object { $_.SubtypeId -like 'Prototech*' }
if ($proto) {
    Write-Host "Prototech volume corrections suggested:" -ForegroundColor Magenta
    foreach ($p in $proto) {
    if ($null -ne $p.ReferenceVolume) {
            Write-Host ("Subtype {0}: config {1} -> ref {2}" -f $p.SubtypeId,$p.ConfigVolume,$p.ReferenceVolume)
        }
    }
}

# 6. Orphan detection (reference entries not present in config) if requested
if ($ReportOrphans) {
    $configKeySet = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($d in $definitions) { [void]$configKeySet.Add(("{0}|{1}" -f $d.TypeId,$d.SubtypeId)) }
    $orphans = @()
    $excludeSet = ($ExcludeTypeIds -split ',') | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | ForEach-Object { $_.Trim().ToLower() } | Sort-Object -Unique
    $excludeSubtypeSet = ($ExcludeSubtypes -split ',') | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | ForEach-Object { $_.Trim().ToLower() } | Sort-Object -Unique
    foreach ($k in $allRef.Keys) {
        if (-not $configKeySet.Contains($k)) {
            $parts = $k.Split('|',2)
            $type = $parts[0]
            $sub = $parts[1]
            # Exclude unwanted TypeIds
            if ($excludeSet -contains $type.ToLower()) { continue }
            # Exclude specific subtypeIds
            if ($excludeSubtypeSet -contains $sub.ToLower()) { continue }
            # Normalize ingot scrap: ignore ingot|scrap if ore|Scrap exists in config
            if ($type.Equals('ingot',[StringComparison]::OrdinalIgnoreCase) -and $sub.Equals('scrap',[StringComparison]::OrdinalIgnoreCase)) {
                if ($configKeySet.Contains('Ore|Scrap')) { continue }
            }
            $orphans += [pscustomobject]@{ TypeId=$type; SubtypeId=$sub; ReferenceVolume=$allRef[$k] }
        }
    }
    if ($orphans.Count -gt 0) {
        New-Item -ItemType Directory -Force -Path ([IO.Path]::GetDirectoryName($OutOrphansCsv)) | Out-Null
        $orphans | Sort-Object TypeId,SubtypeId | Export-Csv -LiteralPath $OutOrphansCsv -NoTypeInformation
        Write-Host "Orphan reference entries (not in config): $($orphans.Count). CSV: $OutOrphansCsv (Excluded types: $ExcludeTypeIds; excluded subtypes: $ExcludeSubtypes)" -ForegroundColor DarkCyan
        $orphans | Select-Object -First 12 | Format-Table -AutoSize | Out-String | Write-Host
    } else {
        Write-Host "No orphan reference entries detected (Excluded types: $ExcludeTypeIds; excluded subtypes: $ExcludeSubtypes)." -ForegroundColor DarkCyan
    }
}
