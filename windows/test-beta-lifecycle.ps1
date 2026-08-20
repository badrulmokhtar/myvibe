param(
    [string]$ArchivePath,
    [string]$CatalogPath,
    [string]$CatalogSignaturePath,
    [string]$PreviewPath
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($ArchivePath)) { $ArchivePath = Join-Path $PSScriptRoot 'dist\MyVibe-0.4.0-beta.1-win-x64.zip' }
if ([string]::IsNullOrWhiteSpace($CatalogPath)) { $CatalogPath = Join-Path (Split-Path $PSScriptRoot -Parent) 'catalog-v2.json' }
if ([string]::IsNullOrWhiteSpace($CatalogSignaturePath)) { $CatalogSignaturePath = Join-Path (Split-Path $PSScriptRoot -Parent) 'catalog-v2.json.sig' }

if (-not (Test-Path -LiteralPath $ArchivePath)) { throw "Release archive not found: $ArchivePath" }
if (-not (Test-Path -LiteralPath $CatalogPath)) { throw "Catalog not found: $CatalogPath" }
if (-not (Test-Path -LiteralPath $CatalogSignaturePath)) { throw "Catalog signature not found: $CatalogSignaturePath" }

$testRoot = Join-Path ([IO.Path]::GetTempPath()) ('myvibe-beta-test-' + [Guid]::NewGuid().ToString('N'))
try {
    New-Item -ItemType Directory -Path $testRoot | Out-Null
    Expand-Archive -LiteralPath $ArchivePath -DestinationPath $testRoot

    $expected = @('BETA-README.txt', 'MyVibe.exe', 'SHA256SUMS.txt')
    $actual = @(Get-ChildItem -LiteralPath $testRoot -File | Select-Object -ExpandProperty Name | Sort-Object)
    if (Compare-Object ($expected | Sort-Object) $actual) { throw 'The release archive contains unexpected or missing files.' }

    $exe = Join-Path $testRoot 'MyVibe.exe'
    $version = [Diagnostics.FileVersionInfo]::GetVersionInfo($exe).FileVersion
    if ($version -ne '0.4.0.0') { throw "Unexpected MyVibe file version: $version" }

    $checksumLine = (Get-Content -LiteralPath (Join-Path $testRoot 'SHA256SUMS.txt') -Raw).Trim()
    $parts = $checksumLine -split '\s+', 2
    if ($parts.Count -ne 2 -or $parts[1] -ne 'MyVibe.exe') { throw 'SHA256SUMS.txt is malformed.' }
    $actualHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $exe).Hash
    if ($actualHash -ne $parts[0]) { throw 'MyVibe.exe does not match SHA256SUMS.txt.' }

    $selfTest = Start-Process -FilePath $exe -ArgumentList '--self-test' -WindowStyle Hidden -Wait -PassThru
    if ($selfTest.ExitCode -ne 0) { throw 'MyVibe self-test failed.' }

    $catalogArguments = @('--verify-catalog', ('"{0}"' -f $CatalogPath), ('"{0}"' -f $CatalogSignaturePath))
    $catalogTest = Start-Process -FilePath $exe -ArgumentList $catalogArguments -WindowStyle Hidden -Wait -PassThru
    if ($catalogTest.ExitCode -ne 0) { throw 'MyVibe catalog verification failed.' }

    $legacyCatalog = Join-Path $PSScriptRoot 'catalog.test.json'
    $legacySignature = Join-Path $PSScriptRoot 'catalog.test.json.sig'
    $legacyTest = Start-Process -FilePath $exe -ArgumentList @('--verify-catalog', ('"{0}"' -f $legacyCatalog), ('"{0}"' -f $legacySignature)) -WindowStyle Hidden -Wait -PassThru
    if ($legacyTest.ExitCode -ne 0) { throw 'MyVibe v1 rollback catalog verification failed.' }

    $preview = Join-Path $testRoot 'preview.png'
    $previewArguments = @('--screenshot', ('"{0}"' -f $preview), '1080', '720')
    $previewTest = Start-Process -FilePath $exe -ArgumentList $previewArguments -WindowStyle Hidden -Wait -PassThru
    if ($previewTest.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $preview) -or (Get-Item -LiteralPath $preview).Length -eq 0) {
        throw 'MyVibe could not render its main window in the isolated test profile.'
    }
    if (-not [string]::IsNullOrWhiteSpace($PreviewPath)) { Copy-Item -LiteralPath $preview -Destination $PreviewPath }

    [pscustomobject]@{
        Result = 'Passed'
        Version = $version
        ExeSha256 = $actualHash
        ArchiveSha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $ArchivePath).Hash
        PreviewBytes = (Get-Item -LiteralPath $preview).Length
    } | Format-List
}
finally {
    if (Test-Path -LiteralPath $testRoot) {
        for ($attempt = 0; $attempt -lt 10; $attempt++) {
            try {
                Remove-Item -LiteralPath $testRoot -Recurse -Force
                break
            }
            catch {
                if ($attempt -eq 9) { throw }
                Start-Sleep -Milliseconds 250
            }
        }
    }
}
