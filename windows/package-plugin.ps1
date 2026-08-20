param(
    [string]$CertificatePath = (Join-Path $PSScriptRoot 'signing\release-cep.p12'),
    [string]$CertificatePassword = $env:MYVIBE_ZXP_CERT_PASSWORD,
    [switch]$CreateBetaCertificate,
    [string]$TimestampUrl = ''
)

$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $PSScriptRoot
$signer = Join-Path $PSScriptRoot 'tools\ZXPSignCmd.exe'
$sourcePayload = Join-Path $projectRoot 'release\2.5D Transform-0.9.5-20260809.zip'
$payloadDir = Join-Path $PSScriptRoot 'payload'
$outputPayload = Join-Path $payloadDir '2.5D Transform-0.9.5-beta.zip'

if (-not (Test-Path -LiteralPath $signer)) { throw "Adobe ZXPSignCmd was not found: $signer" }
if (-not (Test-Path -LiteralPath $sourcePayload)) { throw "Plugin release was not found: $sourcePayload" }
if ([string]::IsNullOrWhiteSpace($CertificatePassword)) {
    $secure = Read-Host 'CEP certificate password' -AsSecureString
    $CertificatePassword = (New-Object System.Management.Automation.PSCredential('unused', $secure)).GetNetworkCredential().Password
}
if ([string]::IsNullOrWhiteSpace($CertificatePassword)) { throw 'A certificate password is required.' }

$certificateDir = Split-Path -Parent $CertificatePath
New-Item -ItemType Directory -Force -Path $certificateDir,$payloadDir | Out-Null
if (-not (Test-Path -LiteralPath $CertificatePath)) {
    if (-not $CreateBetaCertificate) {
        throw 'Certificate not found. Pass -CreateBetaCertificate for local testing or provide a release certificate.'
    }
    & $signer -selfSignedCert MY KualaLumpur MyVibe 'MyVibe Beta' $CertificatePassword $CertificatePath -locality KualaLumpur -validityDays 3650
    if ($LASTEXITCODE -ne 0) { throw 'Adobe ZXPSignCmd could not create the beta certificate.' }
}

$tempBase = [IO.Path]::GetTempPath()
$tempRoot = Join-Path $tempBase ('myvibe-package-' + [Guid]::NewGuid().ToString('N'))
$zxp = Join-Path $tempRoot '2.5D Transform.zxp'
$unsignedCep = Join-Path $tempRoot 'unsigned-cep'
$signedCep = Join-Path $tempRoot 'signed-cep'
$staging = Join-Path $tempRoot 'staging'

try {
    New-Item -ItemType Directory -Force -Path $tempRoot,$unsignedCep,$signedCep,$staging | Out-Null

    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $sourceArchive = [IO.Compression.ZipFile]::OpenRead($sourcePayload)
    try {
        $aipEntry = $sourceArchive.Entries | Where-Object { $_.FullName -match '(^|[\\/])2\.5D Transform\.aip$' } | Select-Object -First 1
        if (-not $aipEntry) { throw 'The source plugin release does not contain 2.5D Transform.aip.' }
        [IO.Compression.ZipFileExtensions]::ExtractToFile($aipEntry, (Join-Path $staging '2.5D Transform.aip'), $true)

        $cepRoot = [IO.Path]::GetFullPath($unsignedCep) + [IO.Path]::DirectorySeparatorChar
        foreach ($entry in $sourceArchive.Entries) {
            $normalized = $entry.FullName.Replace('\', '/')
            $marker = '/2.5D Transform/'
            $markerIndex = $normalized.IndexOf($marker, [StringComparison]::OrdinalIgnoreCase)
            if ($markerIndex -lt 0) { continue }
            $relative = $normalized.Substring($markerIndex + $marker.Length).Replace('/', [IO.Path]::DirectorySeparatorChar)
            if ([string]::IsNullOrWhiteSpace($relative)) { continue }
            $target = [IO.Path]::GetFullPath((Join-Path $unsignedCep $relative))
            if (-not $target.StartsWith($cepRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'The source plugin release contains an unsafe CEP path.' }
            if ([string]::IsNullOrEmpty($entry.Name)) {
                New-Item -ItemType Directory -Force -Path $target | Out-Null
                continue
            }
            New-Item -ItemType Directory -Force -Path (Split-Path -Parent $target) | Out-Null
            [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $target, $true)
        }
    } finally {
        $sourceArchive.Dispose()
    }
    if (-not (Test-Path -LiteralPath (Join-Path $unsignedCep 'CSXS\manifest.xml'))) { throw 'The clean CEP release is missing its manifest.' }

    $signArgs = @('-sign', $unsignedCep, $zxp, $CertificatePath, $CertificatePassword)
    if (-not [string]::IsNullOrWhiteSpace($TimestampUrl)) { $signArgs += @('-tsa', $TimestampUrl) }
    & $signer @signArgs
    if ($LASTEXITCODE -ne 0) { throw 'Adobe ZXPSignCmd could not sign the CEP extension.' }

    & $signer -verify $zxp -skipOnlineRevocationChecks
    if ($LASTEXITCODE -ne 0) { throw 'The signed CEP package failed verification.' }

    [IO.Compression.ZipFile]::ExtractToDirectory($zxp, $signedCep)

    Copy-Item -LiteralPath $signedCep -Destination (Join-Path $staging '2.5D Transform') -Recurse
    if (Test-Path -LiteralPath $outputPayload) { throw "Output already exists: $outputPayload" }
    [IO.Compression.ZipFile]::CreateFromDirectory($staging, $outputPayload, [IO.Compression.CompressionLevel]::Optimal, $false)

    Get-FileHash -Algorithm SHA256 -LiteralPath $outputPayload | Select-Object Path,Hash
} finally {
    $resolvedTemp = [IO.Path]::GetFullPath($tempRoot)
    if ($resolvedTemp.StartsWith([IO.Path]::GetFullPath($tempBase), [StringComparison]::OrdinalIgnoreCase) -and (Test-Path -LiteralPath $resolvedTemp)) {
        Remove-Item -LiteralPath $resolvedTemp -Recurse -Force
    }
}
