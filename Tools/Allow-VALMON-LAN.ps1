param([ValidateRange(1024,65535)][int]$Port = 7788)
$valmonExecutable = Join-Path $PSScriptRoot 'VALMON.exe'
if (-not (Test-Path -LiteralPath $valmonExecutable)) { throw 'Place this script beside VALMON.exe in the game folder.' }
$valmonRuleName = "VALMON UDP LAN $Port"
$valmonExistingRule = Get-NetFirewallRule -DisplayName $valmonRuleName -ErrorAction SilentlyContinue
if ($valmonExistingRule) { Write-Host 'The VALMON LAN rule already exists.'; exit 0 }
New-NetFirewallRule -DisplayName $valmonRuleName -Direction Inbound -Action Allow -Protocol UDP -LocalPort $Port -Program $valmonExecutable -Profile Private -RemoteAddress LocalSubnet | Out-Null
Write-Host "Allowed VALMON UDP port $Port from the local subnet on Private networks."

