param(
    [Parameter(Mandatory = $true)]
    [string[]]$Targets,
    [Parameter(Mandatory = $true)]
    [string]$CertificatePath,
    [string]$CertificatePassword = $env:MYVIBE_WINDOWS_CERT_PASSWORD,
    [string]$TimestampUrl = 'http://timestamp.digicert.com'
)

$ErrorActionPreference = 'Stop'

$signTool = Get-ChildItem 'C:\Program Files (x86)\Windows Kits\10\bin' -Recurse -Filter signtool.exe -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -match '\\x64\\signtool\.exe$' } |
    Sort-Object FullName -Descending |
    Select-Object -First 1 -ExpandProperty FullName
if (-not $signTool) { throw 'Windows SignTool was not found.' }
if (-not (Test-Path -LiteralPath $CertificatePath)) { throw "Certificate not found: $CertificatePath" }
if ([string]::IsNullOrWhiteSpace($CertificatePassword)) {
    $secure = Read-Host 'Windows certificate password' -AsSecureString
    $CertificatePassword = (New-Object System.Management.Automation.PSCredential('unused', $secure)).GetNetworkCredential().Password
}

foreach ($target in $Targets) {
    if (-not (Test-Path -LiteralPath $target)) { throw "Signing target not found: $target" }
    & $signTool sign /fd SHA256 /f $CertificatePath /p $CertificatePassword /tr $TimestampUrl /td SHA256 $target
    if ($LASTEXITCODE -ne 0) { throw "Windows signing failed: $target" }
    & $signTool verify /pa /v $target
    if ($LASTEXITCODE -ne 0) { throw "Windows signature verification failed: $target" }
}
