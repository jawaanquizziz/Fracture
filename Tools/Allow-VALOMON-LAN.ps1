param([ValidateRange(1024,65535)][int]$Port = 7788)
$valomonExecutable = Join-Path $PSScriptRoot 'VALOMON.exe'
if (-not (Test-Path -LiteralPath $valomonExecutable)) { throw 'Place this script beside VALOMON.exe in the game folder.' }
$valomonRuleName = "VALOMON UDP LAN $Port"
$valomonExistingRule = Get-NetFirewallRule -DisplayName $valomonRuleName -ErrorAction SilentlyContinue
if ($valomonExistingRule) { Write-Host 'The VALOMON LAN rule already exists.'; exit 0 }
New-NetFirewallRule -DisplayName $valomonRuleName -Direction Inbound -Action Allow -Protocol UDP -LocalPort $Port -Program $valomonExecutable -Profile Private -RemoteAddress LocalSubnet | Out-Null
Write-Host "Allowed VALOMON UDP port $Port from the local subnet on Private networks."
