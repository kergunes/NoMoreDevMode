param(
    [string]$OutputPath
)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
foreach ($version in @('1.6', '1.7')) {
    $assembly = Join-Path $repoRoot "$version/Assemblies/EarnedResurrection.dll"
    if (!(Test-Path -LiteralPath $assembly)) {
        throw "Build RimWorldVersion=$version before packaging. Missing: $assembly"
    }
}
if (!$OutputPath) {
    $OutputPath = Join-Path $repoRoot 'artifacts/EarnedResurrection-1.6-1.7.zip'
}
$OutputPath = [IO.Path]::GetFullPath($OutputPath)
if (Test-Path -LiteralPath $OutputPath) { throw "Package already exists: $OutputPath" }
$staging = Join-Path $repoRoot ('artifacts/package-' + [Guid]::NewGuid().ToString('N'))
$payload = Join-Path $staging 'EarnedResurrection'
New-Item -ItemType Directory -Path $payload -Force | Out-Null
foreach ($name in @('About', 'Common', 'docs', 'LoadFolders.xml', 'README.md', 'LICENSE')) {
    Copy-Item -LiteralPath (Join-Path $repoRoot $name) -Destination $payload -Recurse
}
foreach ($version in @('1.6', '1.7')) {
    $target = Join-Path $payload "$version/Assemblies"
    New-Item -ItemType Directory -Path $target -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $repoRoot "$version/Assemblies/EarnedResurrection.dll") -Destination $target
    $defs = Join-Path $repoRoot "$version/Defs"
    if (Test-Path -LiteralPath $defs) {
        Copy-Item -LiteralPath $defs -Destination (Join-Path $payload $version) -Recurse
    }
}
New-Item -ItemType Directory -Path (Split-Path $OutputPath -Parent) -Force | Out-Null
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory($staging, $OutputPath)
Write-Output $OutputPath
