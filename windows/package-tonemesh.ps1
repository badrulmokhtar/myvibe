param(
    [Parameter(Mandatory = $true)]
    [string]$ToneMeshRoot,
    [string]$Version = '0.9.3',
    [string]$CertificatePath = (Join-Path $PSScriptRoot 'signing\release-cep.p12'),
    [string]$CertificatePassword = $env:MYVIBE_ZXP_CERT_PASSWORD,
    [string]$TimestampUrl = '',
    [string]$OutputPath = ''
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $PSScriptRoot ("dist\ToneMesh-$Version-beta-win-x64.zip")
}

$signer = Join-Path $PSScriptRoot 'tools\ZXPSignCmd.exe'
$aip = Join-Path $ToneMeshRoot 'dist\Release\ToneMesh.aip'
$cep = Join-Path $ToneMeshRoot 'cep\ToneMesh'
if (-not (Test-Path -LiteralPath $signer)) { throw "Adobe ZXPSignCmd was not found: $signer" }
if (-not (Test-Path -LiteralPath $aip)) { throw "Build ToneMesh Release first: $aip" }
if (-not (Test-Path -LiteralPath (Join-Path $cep 'CSXS\manifest.xml'))) { throw "ToneMesh CEP source is incomplete: $cep" }
if (-not (Test-Path -LiteralPath $CertificatePath)) { throw "CEP signing certificate not found: $CertificatePath" }
if ([string]::IsNullOrWhiteSpace($CertificatePassword)) { throw 'Set MYVIBE_ZXP_CERT_PASSWORD before packaging ToneMesh.' }
if (Test-Path -LiteralPath $OutputPath) { throw "Output already exists: $OutputPath" }

$tempBase = [IO.Path]::GetTempPath()
$tempRoot = Join-Path $tempBase ('myvibe-tonemesh-' + [Guid]::NewGuid().ToString('N'))
$zxp = Join-Path $tempRoot 'ToneMesh.zxp'
$signedCep = Join-Path $tempRoot 'signed-cep'
$staging = Join-Path $tempRoot 'staging'
try {
    New-Item -ItemType Directory -Force -Path $tempRoot,$signedCep,$staging,(Split-Path -Parent $OutputPath) | Out-Null
    $signArgs = @('-sign', $cep, $zxp, $CertificatePath, $CertificatePassword)
    if (-not [string]::IsNullOrWhiteSpace($TimestampUrl)) { $signArgs += @('-tsa', $TimestampUrl) }
    & $signer @signArgs
    if ($LASTEXITCODE -ne 0) { throw 'Adobe ZXPSignCmd could not sign ToneMesh.' }
    & $signer -verify $zxp -skipOnlineRevocationChecks
    if ($LASTEXITCODE -ne 0) { throw 'The signed ToneMesh CEP package failed verification.' }

    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [IO.Compression.ZipFile]::ExtractToDirectory($zxp, $signedCep)
    Copy-Item -LiteralPath $aip -Destination (Join-Path $staging 'ToneMesh.aip')
    Copy-Item -LiteralPath $signedCep -Destination (Join-Path $staging 'ToneMesh') -Recurse
    [IO.Compression.ZipFile]::CreateFromDirectory($staging, $OutputPath, [IO.Compression.CompressionLevel]::Optimal, $false)
    Get-FileHash -Algorithm SHA256 -LiteralPath $OutputPath | Select-Object Path,Hash
} finally {
    $resolvedTemp = [IO.Path]::GetFullPath($tempRoot)
    if ($resolvedTemp.StartsWith([IO.Path]::GetFullPath($tempBase), [StringComparison]::OrdinalIgnoreCase) -and (Test-Path -LiteralPath $resolvedTemp)) {
        Remove-Item -LiteralPath $resolvedTemp -Recurse -Force
    }
}
