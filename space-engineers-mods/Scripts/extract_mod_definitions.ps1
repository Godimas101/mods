# Extract definitions from Mods .sbc files
# Writes reference_mods_definitions.csv with columns:
# File,Mod,NodeType,TypeId,SubtypeId,DisplayName,Volume

$refDir = Join-Path $PSScriptRoot '[REFERENCE FILES]'
$modsDir = Join-Path $refDir 'Mods'
$outFile = Join-Path $PSScriptRoot 'reference_mods_definitions.csv'
$debugFile = Join-Path $PSScriptRoot 'reference_mods_definitions_debug.txt'
$patterns = @('Definition','PhysicalItem','Component','PhysicalMaterial')

if (Test-Path $debugFile) { Remove-Item $debugFile -Force }
"File,Mod,Definition,PhysicalItem,Component,PhysicalMaterial,TotalMatches" | Out-File -FilePath $debugFile -Encoding UTF8

if (!(Test-Path -LiteralPath $modsDir)) {
    Write-Error "Mods folder not found: $modsDir"
    exit 1
}

if (Test-Path $outFile) { Remove-Item $outFile -Force }
"File,Mod,NodeType,TypeId,SubtypeId,DisplayName,Volume" | Out-File -FilePath $outFile -Encoding UTF8

Get-ChildItem -LiteralPath $modsDir -Recurse -Filter *.sbc -File | ForEach-Object {
    $file = $_.FullName
    try {
        $content = Get-Content -LiteralPath $file -Encoding UTF8 | Out-String
    } catch {
        $content = Get-Content -LiteralPath $file | Out-String
    }

    # infer mod name: folder directly under [REFERENCE FILES]\Mods\
    $modName = ''
    try {
        $afterMods = $file -replace [regex]::Escape((Join-Path $refDir 'Mods') + '\\'), ''
        $modName = ($afterMods -split '\\')[0]
    } catch {
        $modName = ''
    }

    $counts = @{}
    foreach ($node in $patterns) {
        $nodeRegex = [regex]::new("<" + $node + "\b[^>]*>.*?</" + $node + ">", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase -bor [System.Text.RegularExpressions.RegexOptions]::Singleline)
        $matches = $nodeRegex.Matches($content)
        $counts[$node] = $matches.Count
        foreach ($m in $matches) {
            $block = $m.Value
            $typeId = ''
            $subtypeId = ''
            $displayName = ''
            $volume = ''

            $mType = [regex]::Match($block,'<TypeId>\s*(.*?)\s*</TypeId>', [System.Text.RegularExpressions.RegexOptions]::Singleline -bor [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
            if ($mType.Success) { $typeId = $mType.Groups[1].Value.Trim() } else {
                $xsiPattern = '<\s*' + $node + '\b[^>]*\bxsi:type\s*=\s*"(.*?)"'
                $mXsi = [regex]::Match($block, $xsiPattern, [System.Text.RegularExpressions.RegexOptions]::Singleline -bor [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
                if ($mXsi.Success) { $typeId = $mXsi.Groups[1].Value.Trim() }
            }

            $mSub = [regex]::Match($block,'<SubtypeId>\s*(.*?)\s*</SubtypeId>', [System.Text.RegularExpressions.RegexOptions]::Singleline -bor [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
            if ($mSub.Success) { $subtypeId = $mSub.Groups[1].Value.Trim() }

            $mDisp = [regex]::Match($block,'<DisplayName>\s*(.*?)\s*</DisplayName>', [System.Text.RegularExpressions.RegexOptions]::Singleline -bor [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
            if ($mDisp.Success) { $displayName = $mDisp.Groups[1].Value.Trim() }

            $mVol = [regex]::Match($block,'<Volume>\s*(.*?)\s*</Volume>', [System.Text.RegularExpressions.RegexOptions]::Singleline -bor [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
            if ($mVol.Success) { $volume = $mVol.Groups[1].Value.Trim() }

            function Escape-CSV([string]$s) {
                if ($null -eq $s) { return '""' }
                $s = $s -replace '"', '""'
                return '"' + $s + '"'
            }

            $rel = $file.Replace($PSScriptRoot + '\\','')
            $line = "$(Escape-CSV $rel),$(Escape-CSV $modName),$(Escape-CSV $node),$(Escape-CSV $typeId),$(Escape-CSV $subtypeId),$(Escape-CSV $displayName),$(Escape-CSV $volume)"
            $line | Out-File -FilePath $outFile -Encoding UTF8 -Append
        }
    }

    $def = $counts['Definition'] -as [int]
    $pi = $counts['PhysicalItem'] -as [int]
    $comp = $counts['Component'] -as [int]
    $pm = $counts['PhysicalMaterial'] -as [int]
    $total = ($def + $pi + $comp + $pm)
    $rel = $file.Replace($PSScriptRoot + '\\','')
    "$rel,$modName,$def,$pi,$comp,$pm,$total" | Out-File -FilePath $debugFile -Encoding UTF8 -Append
}

Write-Output "Wrote: $outFile"