<#
extract_mod_ammo_volumes.ps1
Scans Mods directory for *.sbc ammo magazine definitions and appends/updates rows in reference_mods_definitions.csv
Format aligned with existing reference CSV: File,NodeType,TypeId,SubtypeId,DisplayName,Volume
#>
[CmdletBinding()]
param(
  [string]$ModsRoot = "../[REFERENCE FILES]/Mods",
  [string]$OutCsv = "../[REFERENCE FILES]/Refernece Sheets/reference_mods_definitions.csv",
  [string]$ConfigPath = "../InfoLCD - Apex Update/InfoLCD_Config.cs",
  [switch]$FillMissingFromConfig
)
function Resolve-PathSafe { param([string]$Path) [IO.Path]::GetFullPath((Join-Path $PSScriptRoot $Path)) }
$modsRoot = Resolve-PathSafe $ModsRoot
$outCsv = Resolve-PathSafe $OutCsv
if (!(Test-Path -LiteralPath $modsRoot)) { throw "Mods root not found: $modsRoot" }

$existing = @{}
if (Test-Path -LiteralPath $outCsv) {
  Import-Csv -LiteralPath $outCsv | ForEach-Object {
    $k = ($_.TypeId + '|' + $_.SubtypeId).ToLower()
    if (-not $existing.ContainsKey($k)) { $existing[$k] = $_ }
  }
}

$results = @()
$results += $existing.Values

# Parse ammo magazines
Get-ChildItem -LiteralPath $modsRoot -Recurse -Filter *.sbc -ErrorAction SilentlyContinue | ForEach-Object {
  $file = $_.FullName
  $text = Get-Content -LiteralPath $file -Raw
  if ($text -notmatch '<AmmoMagazines>') { return }
  # crude split
  $text -split '<AmmoMagazine>' | Select-Object -Skip 1 | ForEach-Object { $block = $_
    $typeId = 'AmmoMagazine'
    if ($block -match '<SubtypeId>([^<]+)</SubtypeId>') { $subtype=$Matches[1].Trim() } else { return }
    if ($block -match '<DisplayName>([^<]+)</DisplayName>') { $disp=$Matches[1].Trim() } else { $disp=$subtype }
    $vol = $null
    if ($block -match '<Volume>([^<]+)</Volume>') { $vol = $Matches[1].Trim() }
    if (-not $vol) { return }
    if ($vol -notmatch '^[0-9]+(\.[0-9]+)?$') { return }
    $key = ($typeId + '|' + $subtype).ToLower()
    $obj = [pscustomobject]@{ File=$file; NodeType='AmmoMagazine'; TypeId=$typeId; SubtypeId=$subtype; DisplayName=$disp; Volume=$vol }
    $existing[$key] = $obj
  }
}

# Optionally fill in missing base ammo mags from config (fallback if base AmmoMagazines.sbc not present)
if ($FillMissingFromConfig) {
  $cfgPathFull = Resolve-PathSafe $ConfigPath
  if (Test-Path -LiteralPath $cfgPathFull) {
    Get-Content -LiteralPath $cfgPathFull | ForEach-Object {
      $line = $_.Trim()
      if ($line -match 'new\s+CargoItemDefinition' -and $line -match 'typeId\s*=\s*"AmmoMagazine"') {
        $subtype = if ($line -match 'subtypeId\s*=\s*"([^"]+)"') { $Matches[1] } else { $null }
        $vol = if ($line -match 'volume\s*=\s*([0-9]+\.?[0-9]*)f') { $Matches[1] } else { $null }
        if ($subtype -and $vol) {
          $key = ('AmmoMagazine|' + $subtype).ToLower()
          if (-not $existing.ContainsKey($key)) {
            $obj = [pscustomobject]@{ File=$cfgPathFull; NodeType='AmmoMagazine(FallbackConf)'; TypeId='AmmoMagazine'; SubtypeId=$subtype; DisplayName=$subtype; Volume=$vol }
            $existing[$key] = $obj
          }
        }
      }
    }
  } else {
    Write-Warning "Config file not found for fallback: $cfgPathFull"
  }
}

$final = $existing.Values | Sort-Object TypeId,SubtypeId,DisplayName
$final | Export-Csv -LiteralPath $outCsv -NoTypeInformation
Write-Host "Updated ammo magazine references. Total rows now: $($final.Count) (FillMissingFromConfig=$FillMissingFromConfig)" -ForegroundColor Green
