<#!
Generates a comparative report between base game thrusters and the mod's Upgraded / Advanced versions.

Output: thruster_comparison_report.csv

For each SubtypeId (thruster):
  - Base stats (Force, Mass, Power, N_per_kg, kN_per_MW)
  - Upgraded stats + multipliers vs base
  - Advanced stats + multipliers vs base

Tier detection:
  - File name containing 'Upgraded' => Tier = Upgraded
  - File name containing 'Advanced' => Tier = Advanced
  - Others ignored for tiered comparison

Assumptions:
  - Mod reuses the same SubtypeIds as base; tiers are implemented by overriding.
  - Component mass approximation derived from base Components.sbc.
  - Script is read-only with respect to reference files.

Usage:
  powershell -NoProfile -ExecutionPolicy Bypass -File .\thruster_comparison_report.ps1
  (Can override paths with parameters.)
#>

param(
  # Root of the repository (scripts folder parent)
  [string] $RepoRoot = (Split-Path $PSScriptRoot -Parent),
  # Base game files directory
  [string] $BaseFilesDir = $(Join-Path (Join-Path ((Split-Path $PSScriptRoot -Parent)) '[REFERENCE FILES]') 'Base Game Files'),
  # Mod CubeBlocks directory
  [string] $ModCubeBlocksDir = $(Join-Path (Join-Path ((Split-Path $PSScriptRoot -Parent)) 'Not Just For Looks') 'Data\CubeBlocks'),
  # Explicit components file path (optional)
  [string] $ComponentsFile,
  # Reference sheets directory (new standard) with fallback to legacy misspelling then repo root.
  [string] $ReferenceSheetsDir = (foreach ($p in @(
      (Join-Path (Join-Path ((Split-Path $PSScriptRoot -Parent)) '[REFERENCE FILES]') 'Reference Sheets'),
      (Join-Path (Join-Path ((Split-Path $PSScriptRoot -Parent)) '[REFERENCE FILES]') 'Refernece Sheets'),
      $RepoRoot)) { if (Test-Path -LiteralPath $p) { $p; break } }),
  # Output CSV now defaults inside reference sheets
  [string] $OutCsv = (Join-Path $ReferenceSheetsDir 'thruster_comparison_report.csv'),
  # Which standalone base thruster families (by ThrusterType) to include even if no tiers exist
  [string[]] $IncludeBaseThrusterTypes = @('Prototech'),
  # Multiplier thresholds for anomaly detection
  [double] $AnomalyForceThreshold = 5.0,
  [double] $AnomalyEfficiencyThreshold = 3.0,
  [double] $AnomalyNPerKgThreshold = 3.0
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-ComponentMap {
  param([string] $Path)
  if (-not $Path) {
    $Path = Join-Path $BaseFilesDir 'Components.sbc'
  }
  if (-not (Test-Path -LiteralPath $Path)) {
    Write-Warning "Components file not found: $Path (mass unavailable)"; return @{}
  }
  $text = Get-Content -LiteralPath $Path -Encoding UTF8
  $joined = $text -join "`n"
  $pattern = '<Component>(.*?)</Component>'
  $regexMatches = [regex]::Matches($joined,$pattern,'Singleline')
  $map = @{}
  foreach ($m in $regexMatches) {
    $block = $m.Groups[1].Value
    $sub = [regex]::Match($block,'<SubtypeId>([^<]+)</SubtypeId>').Groups[1].Value
    if (-not $sub) { continue }
    $massM = [regex]::Match($block,'<Mass>([^<]+)</Mass>')
    $volM  = [regex]::Match($block,'<Volume>([^<]+)</Volume>')
    $mass = if ($massM.Success) { [double]$massM.Groups[1].Value } else { $null }
    $vol  = if ($volM.Success)  { [double]$volM.Groups[1].Value } else { $null }
    $map[$sub] = [pscustomobject]@{Mass=$mass;Volume=$vol}
  }
  return $map
}

function Get-ThrustersFromFile {
  param([string] $FilePath)
  $raw = Get-Content -LiteralPath $FilePath -Encoding UTF8
  $joined = $raw -join "`n"
  $pattern = '<Definition xsi:type="MyObjectBuilder_ThrustDefinition">(.*?)</Definition>'
  $defs = [regex]::Matches($joined,$pattern,'Singleline')
  foreach ($d in $defs) {
    $block = $d.Groups[1].Value
    $sub = [regex]::Match($block,'<SubtypeId>([^<]+)</SubtypeId>').Groups[1].Value
    if (-not $sub) { continue }
    $cube = [regex]::Match($block,'<CubeSize>([^<]+)</CubeSize>').Groups[1].Value
    $force = [regex]::Match($block,'<ForceMagnitude>([^<]+)</ForceMagnitude>').Groups[1].Value
    $pwr   = [regex]::Match($block,'<MaxPowerConsumption>([^<]+)</MaxPowerConsumption>').Groups[1].Value
    $thrusterType = [regex]::Match($block,'<ThrusterType>([^<]+)</ThrusterType>').Groups[1].Value
    $compPattern = '<Component\s+Subtype="([^"]+)"\s+Count="([0-9]+)"'
    $compMatches = [regex]::Matches($block,$compPattern)
    $compAgg = @{}
    foreach ($cm in $compMatches) {
      $cSub = $cm.Groups[1].Value
      $cnt = [int]$cm.Groups[2].Value
      if ($compAgg.ContainsKey($cSub)) { $compAgg[$cSub] += $cnt } else { $compAgg[$cSub] = $cnt }
    }
    [pscustomobject]@{
      FilePath=$FilePath
      SubtypeId=$sub
      CubeSize=$cube
      ThrusterType=$thrusterType
      Force=[double]$force
      MaxPower=[double]$pwr
      Components=$compAgg
    }
  }
}

function Get-ThrusterMassMetrics {
  param([pscustomobject] $Thruster,[hashtable] $ComponentMap)
  $mass=0.0;$vol=0.0;$break=@();
  foreach ($kv in $Thruster.Components.GetEnumerator()) {
    $cSub=$kv.Key;$cnt=$kv.Value
    if ($ComponentMap.ContainsKey($cSub)) {
      $cMass=$ComponentMap[$cSub].Mass
      $cVol=$ComponentMap[$cSub].Volume
      if ($cMass) { $mass += $cMass * $cnt }
      if ($cVol)  { $vol  += $cVol  * $cnt }
    }
    $break += "$cSub*$cnt"
  }
  $nPerKg = if ($mass -gt 0) { $Thruster.Force / $mass } else { $null }
  $kNperMW = if ($Thruster.MaxPower -gt 0) { ($Thruster.Force/1000)/$Thruster.MaxPower } else { $null }
  return [pscustomobject]@{
    SubtypeId=$Thruster.SubtypeId
    CubeSize=$Thruster.CubeSize
    ThrusterType=$Thruster.ThrusterType
    Force=$Thruster.Force
    MaxPower=$Thruster.MaxPower
    TotalMass=[math]::Round($mass,2)
    TotalVolume=[math]::Round($vol,2)
    N_per_kg= if ($nPerKg){[math]::Round($nPerKg,2)} else {$null}
    kN_per_MW= if ($kNperMW){[math]::Round($kNperMW,2)} else {$null}
    ComponentsBreakdown= ($break -join ';')
  }
}

Write-Host "Loading component definitions..." -ForegroundColor Cyan
$compMap = Get-ComponentMap -Path $ComponentsFile

if (-not (Test-Path -LiteralPath $BaseFilesDir)) { throw "Base files dir not found: $BaseFilesDir" }
if (-not (Test-Path -LiteralPath $ModCubeBlocksDir)) { throw "Mod cube blocks dir not found: $ModCubeBlocksDir" }

Write-Host "Scanning base thrusters..." -ForegroundColor Cyan
if (-not (Test-Path -LiteralPath $BaseFilesDir)) { throw "Base files dir not found (after relocation fix attempt): $BaseFilesDir" }
$baseThrusterFiles = Get-ChildItem -LiteralPath $BaseFilesDir -Filter 'CubeBlocks_*.sbc' -File
$baseThrusters = foreach ($bf in $baseThrusterFiles) { Get-ThrustersFromFile -FilePath $bf.FullName }
$baseMetrics = @{}
foreach ($t in $baseThrusters) {
  $enriched = Get-ThrusterMassMetrics -Thruster $t -ComponentMap $compMap
  $key = "$($t.SubtypeId)|$($t.CubeSize)"
  $baseMetrics[$key] = $enriched
}

Write-Host "Scanning mod thrusters..." -ForegroundColor Cyan
if (-not (Test-Path -LiteralPath $ModCubeBlocksDir)) { throw "Mod cube blocks dir not found (after relocation fix attempt): $ModCubeBlocksDir" }
$modThrusterFiles = Get-ChildItem -LiteralPath $ModCubeBlocksDir -Filter '*.sbc' -File
$modThrusters = foreach ($mf in $modThrusterFiles) { Get-ThrustersFromFile -FilePath $mf.FullName }

# Partition mod thrusters into tiers
$tiered = @{}
foreach ($mt in $modThrusters) {
  $tier = if ($mt.FilePath -match 'Advanced') { 'Advanced' } elseif ($mt.FilePath -match 'Upgraded') { 'Upgraded' } else { 'Other' }
  if ($tier -eq 'Other') { continue }
  $enriched = Get-ThrusterMassMetrics -Thruster $mt -ComponentMap $compMap
  $key = "$($mt.SubtypeId)|$($mt.CubeSize)"
  if (-not $tiered.ContainsKey($key)) { $tiered[$key] = @{} }
  $tiered[$key][$tier] = $enriched
}

Write-Host "Building comparison rows..." -ForegroundColor Cyan
$rows = @()
foreach ($key in ($tiered.Keys | Sort-Object)) {
  $base = $baseMetrics[$key]
  $up   = $tiered[$key]['Upgraded']
  $adv  = $tiered[$key]['Advanced']
  $parts = $key.Split('|'); $sub=$parts[0]; $cube=$parts[1]
  $row = [ordered]@{
    SubtypeId = $sub
    CubeSize  = $cube
    Base_Force = if ($base){$base.Force}else{$null}
    Upgraded_Force = if ($up){$up.Force}else{$null}
    Advanced_Force = if ($adv){$adv.Force}else{$null}
    Base_Mass = if ($base){$base.TotalMass}else{$null}
    Upgraded_Mass = if ($up){$up.TotalMass}else{$null}
    Advanced_Mass = if ($adv){$adv.TotalMass}else{$null}
    Base_N_per_kg = if ($base){$base.N_per_kg}else{$null}
    Upgraded_N_per_kg = if ($up){$up.N_per_kg}else{$null}
    Advanced_N_per_kg = if ($adv){$adv.N_per_kg}else{$null}
    Base_kN_per_MW = if ($base){$base.kN_per_MW}else{$null}
    Upgraded_kN_per_MW = if ($up){$up.kN_per_MW}else{$null}
    Advanced_kN_per_MW = if ($adv){$adv.kN_per_MW}else{$null}
    BaseOnly = $false
    # Pre-create multiplier & anomaly columns so they appear in header regardless of first-row tier composition
    Upgraded_Force_x = $null
    Upgraded_Mass_x = $null
    Upgraded_N_per_kg_x = $null
    Upgraded_kN_per_MW_x = $null
    Advanced_Force_x = $null
    Advanced_Mass_x = $null
    Advanced_N_per_kg_x = $null
    Advanced_kN_per_MW_x = $null
    AnomalyFlag = $false
  }
  # Multipliers
  if ($base -and $up) {
    $row.Upgraded_Force_x = [math]::Round($up.Force / $base.Force,2)
    $row.Upgraded_Mass_x  = if ($base.TotalMass -gt 0) { [math]::Round($up.TotalMass / $base.TotalMass,2) } else { $null }
    $row.Upgraded_N_per_kg_x = if ($base.N_per_kg) { [math]::Round($up.N_per_kg / $base.N_per_kg,2) } else { $null }
    $row.Upgraded_kN_per_MW_x = if ($base.kN_per_MW) { [math]::Round($up.kN_per_MW / $base.kN_per_MW,2) } else { $null }
  }
  if ($base -and $adv) {
    $row.Advanced_Force_x = [math]::Round($adv.Force / $base.Force,2)
    $row.Advanced_Mass_x  = if ($base.TotalMass -gt 0) { [math]::Round($adv.TotalMass / $base.TotalMass,2) } else { $null }
    $row.Advanced_N_per_kg_x = if ($base.N_per_kg) { [math]::Round($adv.N_per_kg / $base.N_per_kg,2) } else { $null }
    $row.Advanced_kN_per_MW_x = if ($base.kN_per_MW) { [math]::Round($adv.kN_per_MW / $base.kN_per_MW,2) } else { $null }
  }
  # Anomaly detection (after multipliers computed)
  $isAnomaly = $false
  if ($row.Upgraded_Force_x -and $row.Upgraded_Force_x -gt $AnomalyForceThreshold) { $isAnomaly = $true }
  if ($row.Advanced_Force_x -and $row.Advanced_Force_x -gt $AnomalyForceThreshold) { $isAnomaly = $true }
  if ($row.Upgraded_kN_per_MW_x -and $row.Upgraded_kN_per_MW_x -gt $AnomalyEfficiencyThreshold) { $isAnomaly = $true }
  if ($row.Advanced_kN_per_MW_x -and $row.Advanced_kN_per_MW_x -gt $AnomalyEfficiencyThreshold) { $isAnomaly = $true }
  if ($row.Upgraded_N_per_kg_x -and $row.Upgraded_N_per_kg_x -gt $AnomalyNPerKgThreshold) { $isAnomaly = $true }
  if ($row.Advanced_N_per_kg_x -and $row.Advanced_N_per_kg_x -gt $AnomalyNPerKgThreshold) { $isAnomaly = $true }
  $row.AnomalyFlag = $isAnomaly
  $rows += [pscustomobject]$row
}

# Add base-only rows for included thruster types (e.g., Prototech) not already in tiered set
foreach ($kv in $baseMetrics.GetEnumerator()) {
  $baseSubtype,$baseCube = $kv.Key.Split('|')
  $baseObj = $kv.Value
  if (-not $IncludeBaseThrusterTypes -or $IncludeBaseThrusterTypes.Count -eq 0) { continue }
  # Need original thruster type; enrich objects stored lacked thruster type so recalc mapping
}

# Re-parse base thrusters for type mapping
$baseTypeLookup = @{}
foreach ($t in $baseThrusters) { $baseTypeLookup["$($t.SubtypeId)|$($t.CubeSize)"] = $t.ThrusterType }

foreach ($kv in $baseMetrics.GetEnumerator()) {
  $key = $kv.Key
  if ($tiered.ContainsKey($key)) { continue }
  $thrusterType = $baseTypeLookup[$key]
  if ($IncludeBaseThrusterTypes -contains $thrusterType) {
    $parts = $key.Split('|'); $sub=$parts[0]; $cube=$parts[1]
    $base = $kv.Value
    $row = [ordered]@{
      SubtypeId = $sub
      CubeSize  = $cube
      Base_Force = $base.Force
      Upgraded_Force = $null
      Advanced_Force = $null
      Base_Mass = $base.TotalMass
      Upgraded_Mass = $null
      Advanced_Mass = $null
      Base_N_per_kg = $base.N_per_kg
      Upgraded_N_per_kg = $null
      Advanced_N_per_kg = $null
      Base_kN_per_MW = $base.kN_per_MW
      Upgraded_kN_per_MW = $null
      Advanced_kN_per_MW = $null
      BaseOnly = $true
      Upgraded_Force_x = $null
      Upgraded_Mass_x = $null
      Upgraded_N_per_kg_x = $null
      Upgraded_kN_per_MW_x = $null
      Advanced_Force_x = $null
      Advanced_Mass_x = $null
      Advanced_N_per_kg_x = $null
      Advanced_kN_per_MW_x = $null
      AnomalyFlag = $false
    }
    $rows += [pscustomobject]$row
  }
}

if (-not $rows) { Write-Warning "No comparison rows produced."; exit }
$rows | Sort-Object SubtypeId | Export-Csv -Path $OutCsv -NoTypeInformation -Encoding UTF8
Write-Host "Comparison report written: $OutCsv" -ForegroundColor Green
