param(
  [string] $CsvPath = (foreach ($p in @(
      (Join-Path (Join-Path (Split-Path $PSScriptRoot -Parent) '[REFERENCE FILES]') 'Reference Sheets\thruster_thrust_to__mass_ratios.csv'),
      (Join-Path (Join-Path (Split-Path $PSScriptRoot -Parent) '[REFERENCE FILES]') 'Refernece Sheets\thruster_thrust_to__mass_ratios.csv'),
      (Join-Path (Split-Path $PSScriptRoot -Parent) 'thruster_thrust_to__mass_ratios.csv'))) { if (Test-Path -LiteralPath $p) { $p; break } }), 
    
  [double] $UpgradedMultiplier = 2.0,
  [double] $AdvancedMultiplier = 3.0,
  [switch] $DryRun,
  [switch] $SkipExistingAccurate, # if within tolerance already, skip rewrite
  [double] $TolerancePct = 1.5,    # percent deviation allowed before change
  [string] $OutCsv                # optional separate output path
)

if (-not (Test-Path -LiteralPath $CsvPath)) { throw "CSV not found: $CsvPath" }
$lines = Get-Content -LiteralPath $CsvPath -Encoding UTF8
if (-not $lines) { throw 'CSV empty' }
$header = $lines[0]
# Expected header columns
$cols = $header.Split(',')
# Build index map
$idxMap = @{}
for ($i=0;$i -lt $cols.Length;$i++){ $idxMap[$cols[$i]] = $i }
$required = 'SubtypeId','Thruster Type','CubeSize','Base Thrust','Upgraded Thrust','Base Mass','Upgraded Mass','Base Thrust To Mass','Upgraded Thrust To Mass'
foreach ($r in $required){ if (-not $idxMap.ContainsKey($r)) { throw "Missing expected column: $r" }}

$new = @($header)

function Convert-ToNumber {
  param([string] $v)
  if ([string]::IsNullOrWhiteSpace($v)) { return $null }
  return [double]::Parse($v,[System.Globalization.CultureInfo]::InvariantCulture)
}
function Convert-ToIntString { param($v) return [string]([int][math]::Round($v,0)) }
function Convert-ToRatioString { param($v)
  if ($null -eq $v) { return '' }
  # If effectively an integer, return full integer (prevents 300 -> 3 collapse)
  $roundedInt = [math]::Round($v,0)
  if ([math]::Abs($v - $roundedInt) -lt 1e-9) {
    return ($roundedInt.ToString([System.Globalization.CultureInfo]::InvariantCulture))
  }
  $s = [string]::Format([System.Globalization.CultureInfo]::InvariantCulture,'{0}',[math]::Round($v,9))
  return $s.TrimEnd('0').TrimEnd('.')
}

$_changedUpgraded = 0
$_changedAdvanced = 0
$_skipped = 0
for ($l=1; $l -lt $lines.Count; $l++) {
  $line = $lines[$l]
  if ([string]::IsNullOrWhiteSpace($line)) { $new += $line; continue }
  $parts = $line.Split(',')
  if ($parts.Length -lt $cols.Length) { $new += $line; continue }
  $thrusterType = $parts[$idxMap['Thruster Type']]
  if ($thrusterType -in @('Upgraded','Advanced')) {
    $baseRatio = Convert-ToNumber $parts[$idxMap['Base Thrust To Mass']]
    $upMass    = Convert-ToNumber $parts[$idxMap['Upgraded Mass']]
    $currentUpThrust = Convert-ToNumber $parts[$idxMap['Upgraded Thrust']]
    $currentUpRatio  = Convert-ToNumber $parts[$idxMap['Upgraded Thrust To Mass']]
    if ($baseRatio -and $upMass -and $upMass -gt 0) {
      $mult = if ($thrusterType -eq 'Upgraded') { $UpgradedMultiplier } else { $AdvancedMultiplier }
      $desiredRatio = $baseRatio * $mult
      $desiredThrust = $desiredRatio * $upMass
      $needsChange = $true
      if ($SkipExistingAccurate -and $currentUpRatio) {
        $pctDiff = [math]::Abs(($currentUpRatio - $desiredRatio) / $desiredRatio * 100)
        if ($pctDiff -le $TolerancePct) { $needsChange = $false; $_skipped++ }
      }
      if ($needsChange) {
        $parts[$idxMap['Upgraded Thrust']] = Convert-ToIntString $desiredThrust
        $parts[$idxMap['Upgraded Thrust To Mass']] = Convert-ToRatioString $desiredRatio
        if ($thrusterType -eq 'Upgraded') { $_changedUpgraded++ } else { $_changedAdvanced++ }
      }
    }
  }
  $new += ($parts -join ',')
}

if ($DryRun) {
  Write-Host 'DryRun: Preview first 25 lines:' -ForegroundColor Cyan
  ($new | Select-Object -First 25) | ForEach-Object { Write-Host $_ }
  Write-Host "Would change Upgraded: $_changedUpgraded  Advanced: $_changedAdvanced  Skipped (within tolerance): $_skipped" -ForegroundColor Yellow
  $flat = $new | Where-Object { $_ -match 'FlatAtmospheric' }
  if ($flat) {
    Write-Host '--- Flat Atmospheric Rows (post-calculation) ---' -ForegroundColor Cyan
    $flat | ForEach-Object { Write-Host $_ }
  }
}
else {
  $target = if ($OutCsv) { $OutCsv } else { $CsvPath }
  Set-Content -LiteralPath $target -Value $new -Encoding UTF8
  Write-Host "Updated CSV written: $target" -ForegroundColor Green
  Write-Host "Changed Upgraded: $_changedUpgraded  Advanced: $_changedAdvanced  Skipped: $_skipped" -ForegroundColor Cyan
}
