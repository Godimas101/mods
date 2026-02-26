<#!
Synopsis:
  Analyze Space Engineers thruster definitions in the Not Just For Looks mod and
  compute approximate block mass from component lists plus thrust-to-mass ratios.

Notes:
  - Mass & Volume are pulled directly from the base game's Components.sbc file under
    [REFERENCE FILES] (which is ignored by Git). This script will gracefully degrade if
    that file is not present: mass & volume become null and derived ratios limited.
  - ForceMagnitude is treated as Newtons (Space Engineers internal unit). Thrust-to-mass ratio
    (N_per_kg) is ForceMagnitude / TotalComponentMassKg. kN_per_tonne is numerically identical.
  - Power efficiency metric: kN per MW = (ForceMagnitude / 1000) / MaxPowerConsumption.

Output:
  thruster_mass_report.csv in repository root.

Usage:
  powershell -ExecutionPolicy Bypass -File .\thruster_mass_report.ps1

#>

param(
    [string] $RepoRoot = (Split-Path $PSScriptRoot -Parent),
    [string] $ModCubeBlocksPath = (Join-Path (Join-Path $RepoRoot 'Not Just For Looks') 'Data\CubeBlocks'),
    # Prefer new "Reference Sheets" directory inside [REFERENCE FILES]; fall back to historical misspelling or repo root.
    [string] $ReferenceSheetsDir = (foreach ($p in @(
        (Join-Path (Join-Path $RepoRoot '[REFERENCE FILES]') 'Reference Sheets'),
        (Join-Path (Join-Path $RepoRoot '[REFERENCE FILES]') 'Refernece Sheets'),
        $RepoRoot)) { if (Test-Path -LiteralPath $p) { $p; break } }),
    [string] $OutputCsv = (Join-Path $ReferenceSheetsDir 'thruster_mass_report.csv'),
    [string] $ComponentsFilePath,
    [switch] $IncludePrototech,
    [switch] $IncludeBase,
    [string] $BaseFilesDir = (Join-Path (Join-Path $RepoRoot '[REFERENCE FILES]') 'Base Game Files')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-ComponentDefinitions {
    param([string] $FilePath)
    if (-not (Test-Path -LiteralPath $FilePath)) {
        Write-Warning "Components file not found: $FilePath (mass/volume unavailable)"
        return @{}
    }
    $text = Get-Content -LiteralPath $FilePath -Encoding UTF8
    $joined = $text -join "`n"

    # Generic <Component>...</Component> blocks (base game omits xsi:type)
    $pattern = '<Component>(.*?)</Component>'
    $regexMatches = [System.Text.RegularExpressions.Regex]::Matches($joined, $pattern, 'Singleline')
    $dict = @{}
    foreach ($m in $regexMatches) {
        $block = $m.Groups[1].Value
        $subtype = [regex]::Match($block, '<SubtypeId>([^<]+)</SubtypeId>').Groups[1].Value
        if (-not $subtype) { continue }
        $massMatch = [regex]::Match($block, '<Mass>([^<]+)</Mass>')
        $volMatch  = [regex]::Match($block, '<Volume>([^<]+)</Volume>')
        $mass = if ($massMatch.Success) { [double]$massMatch.Groups[1].Value } else { $null }
        $vol  = if ($volMatch.Success)  { [double]$volMatch.Groups[1].Value }  else { $null }
        $dict[$subtype] = [pscustomobject]@{ Mass = $mass; Volume = $vol }
    }
    return $dict
}

function Get-ThrusterDefinitionsFromFile {
    param([string] $FilePath)
    $raw = Get-Content -LiteralPath $FilePath -Encoding UTF8 -ErrorAction Stop
    $joined = $raw -join "`n"
    $pattern = '<Definition xsi:type="MyObjectBuilder_ThrustDefinition">(.*?)</Definition>'
    $regexMatches = [System.Text.RegularExpressions.Regex]::Matches($joined, $pattern, 'Singleline')
    foreach ($m in $regexMatches) {
        $block = $m.Groups[1].Value
        $subtype = [regex]::Match($block, '<SubtypeId>([^<]+)</SubtypeId>').Groups[1].Value
        if (-not $subtype) { continue }
        $cubeSize = [regex]::Match($block, '<CubeSize>([^<]+)</CubeSize>').Groups[1].Value
        $force    = [regex]::Match($block, '<ForceMagnitude>([^<]+)</ForceMagnitude>').Groups[1].Value
        $power    = [regex]::Match($block, '<MaxPowerConsumption>([^<]+)</MaxPowerConsumption>').Groups[1].Value

        # Components (can repeat subtype lines)
        $compPattern = '<Component\s+Subtype="([^"]+)"\s+Count="([0-9]+)"'
        $compMatches = [System.Text.RegularExpressions.Regex]::Matches($block, $compPattern)
        $compAgg = @{}
        foreach ($c in $compMatches) {
            $cSubtype = $c.Groups[1].Value
            $count = [int]$c.Groups[2].Value
            if ($compAgg.ContainsKey($cSubtype)) { $compAgg[$cSubtype] += $count } else { $compAgg[$cSubtype] = $count }
        }
        [pscustomobject]@{
            FilePath          = $FilePath
            SubtypeId         = $subtype
            CubeSize          = $cubeSize
            ForceMagnitudeN   = if ($force) { [double]$force } else { $null }
            MaxPowerMW        = if ($power) { [double]$power } else { $null }
            Components        = $compAgg
        }
    }
}

# Infer default Components.sbc path if not provided
if (-not $ComponentsFilePath) {
    $componentsCandidate = Join-Path $BaseFilesDir 'Components.sbc'
    $ComponentsFilePath = $componentsCandidate
}

Write-Host "Using Components file: $ComponentsFilePath"
$componentMap = Get-ComponentDefinitions -FilePath $ComponentsFilePath

if (-not (Test-Path $ModCubeBlocksPath)) {
    throw "Mod cube blocks path not found: $ModCubeBlocksPath"
}

if (-not (Test-Path -LiteralPath $ModCubeBlocksPath)) { throw "Mod cube blocks path not found: $ModCubeBlocksPath" }

$thrusterFiles = Get-ChildItem -LiteralPath $ModCubeBlocksPath -Filter *.sbc -File -ErrorAction Stop
$thrusters = @()
foreach ($f in $thrusterFiles) { $thrusters += Get-ThrusterDefinitionsFromFile -FilePath $f.FullName | ForEach-Object { $_ | Add-Member -NotePropertyName Origin -NotePropertyValue 'Mod' -PassThru } }

# Optionally include base standard (ion/hydrogen/atmo) thrusters
if ($IncludeBase) {
    $baseStandardFile = Join-Path $BaseFilesDir 'CubeBlocks_Thrusters.sbc'
    if (Test-Path -LiteralPath $baseStandardFile) {
        $baseStandardThrusters = Get-ThrusterDefinitionsFromFile -FilePath $baseStandardFile | ForEach-Object { $_ | Add-Member -NotePropertyName Origin -NotePropertyValue 'Base' -PassThru }
        # Add regardless of duplicate subtype so we can compare base vs mod override
        $thrusters += $baseStandardThrusters
    } else {
        Write-Warning "Base thruster file not found: $baseStandardFile"
    }
}

# Optionally include Prototech (or future base) thrusters for comparison
if ($IncludePrototech) {
    $protoFile = Join-Path $BaseFilesDir 'CubeBlocks_Prototech.sbc'
    if (Test-Path -LiteralPath $protoFile) {
        $protoThrusters = Get-ThrusterDefinitionsFromFile -FilePath $protoFile | ForEach-Object { $_ | Add-Member -NotePropertyName Origin -NotePropertyValue 'BasePrototech' -PassThru }
        # De-duplicate: if mod overrides subtype, keep mod version
        $existingKeys = $thrusters | ForEach-Object { "$($_.SubtypeId)|$($_.CubeSize)" } | Sort-Object -Unique
        foreach ($pt in $protoThrusters) {
            $key = "$($pt.SubtypeId)|$($pt.CubeSize)"
            if ($existingKeys -notcontains $key) { $thrusters += $pt }
        }
    } else {
        Write-Warning "Prototech file not found at $protoFile; skipping"
    }
}

if (-not $thrusters) {
    Write-Warning "No thruster definitions found."; return
}

$rows = @()
foreach ($t in $thrusters) {
    $totalMass = 0.0
    $totalVol  = 0.0
    $componentsBreakdown = @()
    foreach ($kvp in $t.Components.GetEnumerator()) {
        $sub = $kvp.Key; $count = $kvp.Value
        $massEach = $null; $volEach = $null
        if ($componentMap.ContainsKey($sub)) {
            $massEach = $componentMap[$sub].Mass
            $volEach  = $componentMap[$sub].Volume
        }
        if ($massEach) { $totalMass += ($massEach * $count) }
        if ($volEach)  { $totalVol  += ($volEach * $count) }
        $componentsBreakdown += "${sub}*${count}" + ($(if (-not $massEach) { '(!)' } else { '' }))
    }
    $nPerKg = $null
    if ($totalMass -gt 0 -and $t.ForceMagnitudeN) { $nPerKg = $t.ForceMagnitudeN / $totalMass }
    $kNperMW = $null
    if ($t.MaxPowerMW -gt 0 -and $t.ForceMagnitudeN) { $kNperMW = ($t.ForceMagnitudeN / 1000.0) / $t.MaxPowerMW }

    $rows += [pscustomobject]@{
        ThrusterSubtype        = $t.SubtypeId
        CubeSize               = $t.CubeSize
        ForceMagnitudeN        = $t.ForceMagnitudeN
        MaxPowerMW             = $t.MaxPowerMW
        TotalComponentMassKg   = [math]::Round($totalMass,2)
        TotalComponentVolume   = [math]::Round($totalVol,2)
        N_per_kg               = if ($nPerKg) { [math]::Round($nPerKg,2) } else { $null }
        kN_per_MW              = if ($kNperMW) { [math]::Round($kNperMW,2) } else { $null }
        ComponentsBreakdown    = ($componentsBreakdown -join ';')
        Origin                 = if ($t.PSObject.Properties.Name -contains 'Origin') { $t.Origin } else { 'Mod' }
        SourceFile             = $t.FilePath
    }
}

$rows | Sort-Object ThrusterSubtype | Export-Csv -Path $OutputCsv -NoTypeInformation -Encoding UTF8
Write-Host "Report written: $OutputCsv" -ForegroundColor Green

if ($rows | Where-Object { $_.N_per_kg -eq $null }) {
    Write-Warning "Some thrusters missing mass data (component mass not resolved). Components marked with (!) need mass lookup." }
