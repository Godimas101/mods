# Extract definitions from Base Game .sbc files
# Writes reference_files_definitions.csv with columns:
# File,NodeType,TypeId,SubtypeId,DisplayName,Volume

$refDir = Join-Path $PSScriptRoot '[REFERENCE FILES]'
$baseDir = Join-Path $refDir 'Base Game Files'
$outFile = Join-Path $PSScriptRoot 'reference_files_definitions.csv'
$patterns = @('Definition','PhysicalItem','Component','PhysicalMaterial')
$debugFile = Join-Path $PSScriptRoot 'reference_files_definitions_debug.txt'

if (Test-Path $debugFile) { Remove-Item $debugFile -Force }
"File,Definition,PhysicalItem,Component,PhysicalMaterial,TotalMatches" | Out-File -FilePath $debugFile -Encoding UTF8

if (!(Test-Path -LiteralPath $baseDir)) {
    Write-Error "Base game folder not found: $baseDir"
    exit 1
}

if (Test-Path $outFile) { Remove-Item $outFile -Force }

# Header
"File,NodeType,TypeId,SubtypeId,DisplayName,Volume" | Out-File -FilePath $outFile -Encoding UTF8

Get-ChildItem -LiteralPath $baseDir -Recurse -Filter *.sbc -File | ForEach-Object {
    $file = $_.FullName
    try {
        # Read entire file as a single string in a PS-version-compatible way
        $content = Get-Content -LiteralPath $file -Encoding UTF8 | Out-String
    } catch {
        # fallback to default encoding if UTF8 fails
        $content = Get-Content -LiteralPath $file | Out-String
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
            $line = "$(Escape-CSV $rel),$(Escape-CSV $node),$(Escape-CSV $typeId),$(Escape-CSV $subtypeId),$(Escape-CSV $displayName),$(Escape-CSV $volume)"
            $line | Out-File -FilePath $outFile -Encoding UTF8 -Append
        }
    }

    # write debug line for this file
    $def = $counts['Definition'] -as [int]
    $pi = $counts['PhysicalItem'] -as [int]
    $comp = $counts['Component'] -as [int]
    $pm = $counts['PhysicalMaterial'] -as [int]
    $total = ($def + $pi + $comp + $pm)
    $rel = $file.Replace($PSScriptRoot + '\\','')
    "$rel,$def,$pi,$comp,$pm,$total" | Out-File -FilePath $debugFile -Encoding UTF8 -Append
}

Write-Output "Wrote: $outFile" 
