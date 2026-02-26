param(
  [string] $CsvPath = (foreach ($p in @(
      (Join-Path (Join-Path (Split-Path $PSScriptRoot -Parent) '[REFERENCE FILES]') 'Reference Sheets\thruster_thrust_to__mass_ratios.csv'),
      (Join-Path (Join-Path (Split-Path $PSScriptRoot -Parent) '[REFERENCE FILES]') 'Refernece Sheets\thruster_thrust_to__mass_ratios.csv'),
      (Join-Path (Split-Path $PSScriptRoot -Parent) 'thruster_thrust_to__mass_ratios.csv'))) { if (Test-Path -LiteralPath $p) { $p; break } }),
  [double] $UpgradedMultiplier = 2.0,
  [double] $AdvancedMultiplier = 3.0,
  [string] $RecalcThrustCol = 'Recalc Thrust',
  [string] $RecalcRatioCol = 'Recalc Thrust To Mass',
  [switch] $DryRun,
  [string] $OutCsv
)

if (-not (Test-Path -LiteralPath $CsvPath)) { throw "CSV not found: $CsvPath" }
$lines = Get-Content -LiteralPath $CsvPath -Encoding UTF8
if (-not $lines) { throw 'Empty CSV' }
$header = $lines[0]
$headers = $header.Split(',')
$existing = [System.Collections.Generic.HashSet[string]]::new([string[]]$headers)
$addColumns = @()
if (-not $existing.Contains($RecalcThrustCol)) { $addColumns += $RecalcThrustCol }
if (-not $existing.Contains($RecalcRatioCol)) { $addColumns += $RecalcRatioCol }
if ($addColumns.Count -eq 0) { Write-Host 'Recalculated columns already exist; will recompute values.' -ForegroundColor Yellow }
$newHeader = if ($addColumns.Count -gt 0) { ($header + ',' + ($addColumns -join ',')) } else { $header }

function Parse-Number([string]$v){ if ([string]::IsNullOrWhiteSpace($v)) { return $null }; [double]::Parse($v,[System.Globalization.CultureInfo]::InvariantCulture) }
function Format-Int($v){ [string]([int][math]::Round($v,0)) }
function Format-Ratio($v){ if ($null -eq $v) { return '' }; $ri=[math]::Round($v,0); if ([math]::Abs($v-$ri) -lt 1e-9){ return $ri.ToString([System.Globalization.CultureInfo]::InvariantCulture)}; $s=[math]::Round($v,9); return $s.ToString([System.Globalization.CultureInfo]::InvariantCulture).TrimEnd('0').TrimEnd('.') }

# Build index map after potentially adding columns
$baseIdxMap = @{}
for ($i=0;$i -lt $headers.Length;$i++){ $baseIdxMap[$headers[$i]] = $i }

# Ensure we know where recalculated columns will be
$recalcThrustIndex = if ($baseIdxMap.ContainsKey($RecalcThrustCol)) { $baseIdxMap[$RecalcThrustCol] } else { $headers.Length } # if new, appended at end
$recalcRatioIndex  = if ($baseIdxMap.ContainsKey($RecalcRatioCol))  { $baseIdxMap[$RecalcRatioCol] }  else { $headers.Length + ( [int](-not $existing.Contains($RecalcThrustCol)) ) }

$outLines = @($newHeader)
$upCount=0;$advCount=0
for ($l=1;$l -lt $lines.Count;$l++) {
  $line = $lines[$l]
  if ([string]::IsNullOrWhiteSpace($line)) { $outLines += $line; continue }
  $parts = $line.Split(',')
  # Category header lines (like 'Ion Thrusters...') have blank SubtypeId
  if ($parts[0] -eq 'Ion Thrusters' -or $parts[0] -eq 'Hydrogen Thrusters' -or $parts[0] -eq 'Atmo Thrusters') { 
    # pad if needed
    if ($addColumns.Count -gt 0) { $parts = $parts + (@('' * $addColumns.Count)) }
    $outLines += ($parts -join ','); continue
  }
  # Align to header width before append
  while ($parts.Length -lt $headers.Length) { $parts += '' }
  $thrusterType = $parts[$baseIdxMap['Thruster Type']]
  $baseRatio = Parse-Number $parts[$baseIdxMap['Base Thrust To Mass']]
  $tierMass  = Parse-Number $parts[$baseIdxMap['Upgraded Mass']]
  $mult = $null
  switch ($thrusterType) {
    'Upgraded' { $mult = $UpgradedMultiplier }
    'Advanced' { $mult = $AdvancedMultiplier }
    default { $mult = $null }
  }
  if ($mult -and $baseRatio -and $tierMass -gt 0) {
    $targetRatio = $baseRatio * $mult
    $targetThrust = $targetRatio * $tierMass
    if ($thrusterType -eq 'Upgraded') { $upCount++ } elseif ($thrusterType -eq 'Advanced') { $advCount++ }
    # Append or replace recalc columns
    if ($addColumns.Count -gt 0) {
      $parts = $parts + @((Format-Int $targetThrust),(Format-Ratio $targetRatio))
    } else {
      $parts[$recalcThrustIndex] = (Format-Int $targetThrust)
      $parts[$recalcRatioIndex]  = (Format-Ratio $targetRatio)
    }
  } else {
    if ($addColumns.Count -gt 0) { $parts = $parts + @('', '') }
    else {
      if ($recalcThrustIndex -lt $parts.Length) { $parts[$recalcThrustIndex] = '' }
      if ($recalcRatioIndex  -lt $parts.Length) { $parts[$recalcRatioIndex]  = '' }
    }
  }
  $outLines += ($parts -join ',')
}

if ($DryRun) {
  Write-Host "DryRun preview (first 30 lines with new columns)" -ForegroundColor Cyan
  $outLines | Select-Object -First 30 | ForEach-Object { Write-Host $_ }
  Write-Host "Computed Upgraded rows: $upCount  Advanced rows: $advCount" -ForegroundColor Yellow
} else {
  $target = if ($OutCsv) { $OutCsv } else { $CsvPath }
  Set-Content -LiteralPath $target -Value $outLines -Encoding UTF8
  Write-Host "CSV updated with recalculated columns: $target" -ForegroundColor Green
  Write-Host "Computed Upgraded rows: $upCount  Advanced rows: $advCount" -ForegroundColor Cyan
}
