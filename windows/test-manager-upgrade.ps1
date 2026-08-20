param(
    [Parameter(Mandatory = $true)]
    [string]$ExistingManagerPath,
    [string]$CatalogPath,
    [string]$CatalogSignaturePath,
    [string]$ExpectedVersion = '0.3.1'
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($CatalogPath)) { $CatalogPath = Join-Path $PSScriptRoot 'catalog.test.json' }
if ([string]::IsNullOrWhiteSpace($CatalogSignaturePath)) { $CatalogSignaturePath = Join-Path $PSScriptRoot 'catalog.test.json.sig' }

foreach ($path in @($ExistingManagerPath, $CatalogPath, $CatalogSignaturePath)) {
    if (-not (Test-Path -LiteralPath $path)) { throw "Required test file not found: $path" }
}

$assembly = [Reflection.Assembly]::LoadFile((Resolve-Path -LiteralPath $ExistingManagerPath))
$flags = [Reflection.BindingFlags]'Public, NonPublic, Static, Instance'
$client = $assembly.GetType('MyVibe.CatalogClient', $true)
$parse = $client.GetMethod('ParseAndValidateVerified', $flags)
$build = $client.GetMethod('BuildResult', $flags)
if ($null -eq $parse -or $null -eq $build) { throw 'The existing manager does not expose the verified catalog update path.' }

[object[]]$parseArguments = @(
    [IO.File]::ReadAllBytes($CatalogPath),
    [IO.File]::ReadAllBytes($CatalogSignaturePath)
)
$catalog = $parse.Invoke($null, $parseArguments)
[object[]]$buildArguments = @($catalog, $false)
$result = $build.Invoke($null, $buildArguments)
$managerUpdate = $result.GetType().GetField('ManagerUpdate', $flags).GetValue($result)
if ($null -eq $managerUpdate) { throw "The existing manager did not detect MyVibe $ExpectedVersion." }

$detectedVersion = $managerUpdate.GetType().GetField('version', $flags).GetValue($managerUpdate)
if ($detectedVersion -ne $ExpectedVersion) { throw "Expected manager update $ExpectedVersion, detected $detectedVersion." }

[pscustomobject]@{
    Result = 'Passed'
    ExistingManager = [Diagnostics.FileVersionInfo]::GetVersionInfo($ExistingManagerPath).FileVersion
    DetectedUpdate = $detectedVersion
    DownloadUrl = $managerUpdate.GetType().GetField('downloadUrl', $flags).GetValue($managerUpdate)
} | Format-List
