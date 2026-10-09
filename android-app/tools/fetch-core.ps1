$ErrorActionPreference = 'Stop'
$coreDestination = Join-Path $PSScriptRoot '../app/libs/libv2ray.aar'
$coreExpectedHash = 'cf71680b776b9ca583747ba652f816b047a655eab875d8951e6141636d88bbd6'
if (-not (Test-Path -LiteralPath $coreDestination)) {
    Invoke-WebRequest 'https://github.com/2dust/AndroidLibXrayLite/releases/download/v26.9.30/libv2ray.aar' -OutFile $coreDestination
}
if ((Get-FileHash -LiteralPath $coreDestination -Algorithm SHA256).Hash.ToLowerInvariant() -ne $coreExpectedHash) {
    throw 'Native core checksum mismatch.'
}
Write-Output 'Verified Android native core v26.9.30.'
