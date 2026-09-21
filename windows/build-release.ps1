param(
    [switch]$Stable,
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'

if ($Stable -and -not $SkipBuild) {
    throw 'Build MyVibe, sign bin\MyVibe.exe, then run build-release.ps1 -Stable -SkipBuild so the signed executable is not overwritten.'
}
if (-not $SkipBuild) {
    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'build.ps1')
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

$dist = Join-Path $PSScriptRoot 'dist'
$releaseName = if ($Stable) { 'MyVibe-0.5.1-win-x64' } else { 'MyVibe-0.5.1-beta.1-win-x64' }
$staging = Join-Path $dist $releaseName
$archive = Join-Path $dist ($releaseName + '.zip')
$archiveHash = $archive + '.sha256'
foreach ($path in @($staging, $archive, $archiveHash)) {
    if (Test-Path -LiteralPath $path) { throw "Release output already exists: $path" }
}

if ($Stable) {
    $signature = Get-AuthenticodeSignature -LiteralPath (Join-Path $PSScriptRoot 'bin\MyVibe.exe')
    if ($signature.Status -ne 'Valid') {
        throw 'Stable packaging requires MyVibe.exe to have a valid, publicly trusted Authenticode signature. Sign it first with sign-windows.ps1.'
    }
}

New-Item -ItemType Directory -Force -Path $staging | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'bin\MyVibe.exe') -Destination $staging
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'BETA-README.txt') -Destination $staging

$exeHash = (Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $staging 'MyVibe.exe')).Hash
[IO.File]::WriteAllText((Join-Path $staging 'SHA256SUMS.txt'), "$exeHash  MyVibe.exe`r`n")

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory($staging, $archive, [IO.Compression.CompressionLevel]::Optimal, $false)
$zipHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $archive).Hash
[IO.File]::WriteAllText($archiveHash, "$zipHash  $([IO.Path]::GetFileName($archive))`r`n")

Get-Item -LiteralPath $archive,$archiveHash | Select-Object FullName,Length,LastWriteTime
