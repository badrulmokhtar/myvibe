param(
    [Parameter(Mandatory = $true)]
    [string]$CatalogPath,
    [string]$PrivateKeyPath = '',
    [string]$SignaturePath = ($CatalogPath + '.sig')
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($PrivateKeyPath)) {
    $PrivateKeyPath = Join-Path $PSScriptRoot 'signing\catalog-signing-private.key'
}

if (-not (Test-Path -LiteralPath $CatalogPath)) { throw "Catalog not found: $CatalogPath" }
if (-not (Test-Path -LiteralPath $PrivateKeyPath)) { throw "Catalog signing key not found: $PrivateKeyPath" }

$rsa = New-Object System.Security.Cryptography.RSACryptoServiceProvider
try {
    $rsa.ImportCspBlob([IO.File]::ReadAllBytes($PrivateKeyPath))
    $catalog = [IO.File]::ReadAllBytes($CatalogPath)
    $signature = $rsa.SignData($catalog, [Security.Cryptography.CryptoConfig]::MapNameToOID('SHA256'))
    [IO.File]::WriteAllText($SignaturePath, ([Convert]::ToBase64String($signature) + "`n"), (New-Object Text.UTF8Encoding($false)))
    Write-Output "Signed $CatalogPath"
    Get-FileHash -Algorithm SHA256 -LiteralPath $CatalogPath,$SignaturePath | Select-Object Path,Hash
} finally {
    $rsa.PersistKeyInCsp = $false
    $rsa.Dispose()
}
