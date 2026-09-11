$ErrorActionPreference = 'Stop'

$outputDir = Join-Path $PSScriptRoot 'bin'
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'

if (-not (Test-Path -LiteralPath $compiler)) {
    throw 'The Windows C# compiler was not found.'
}
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

$references = @(
    'C:\Windows\Microsoft.NET\assembly\GAC_64\PresentationCore\v4.0_4.0.0.0__31bf3856ad364e35\PresentationCore.dll',
    'C:\Windows\Microsoft.NET\assembly\GAC_MSIL\PresentationFramework\v4.0_4.0.0.0__31bf3856ad364e35\PresentationFramework.dll',
    'C:\Windows\Microsoft.NET\assembly\GAC_MSIL\WindowsBase\v4.0_4.0.0.0__31bf3856ad364e35\WindowsBase.dll',
    'C:\Windows\Microsoft.NET\assembly\GAC_MSIL\System.Xaml\v4.0_4.0.0.0__b77a5c561934e089\System.Xaml.dll',
    'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.IO.Compression.dll',
    'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.IO.Compression.FileSystem.dll',
    'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Web.Extensions.dll'
)
foreach ($reference in $references) {
    if (-not (Test-Path -LiteralPath $reference)) { throw "Required Windows desktop assembly not found: $reference" }
}

$common = @(
    '/nologo',
    '/platform:x64',
    '/optimize+',
    '/warn:4'
)
$referenceArgs = @($references | ForEach-Object { "/reference:$_" })
$source = Join-Path $PSScriptRoot 'MyVibe.cs'
$icon = Join-Path $PSScriptRoot 'assets\myvibe.ico'
if (-not (Test-Path -LiteralPath $icon)) { throw "MyVibe icon not found: $icon" }

$selfTest = Join-Path $outputDir 'MyVibe.SelfTest.exe'
& $compiler @common @referenceArgs '/target:exe' "/out:$selfTest" $source
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& $selfTest --self-test
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Remove-Item -LiteralPath $selfTest -Force

$exe = Join-Path $outputDir 'MyVibe.exe'
$manifest = Join-Path $PSScriptRoot 'app.manifest'
& $compiler @common @referenceArgs '/target:winexe' "/win32manifest:$manifest" "/win32icon:$icon" "/out:$exe" $source
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Get-Item -LiteralPath $exe | Select-Object FullName, Length, LastWriteTime
