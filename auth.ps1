#!/usr/bin/env pwsh

# Old version
<#
# Constants
$loginToken = "kSiBszXoB5hOQBAMBC//f2YcxD76Ec36rlKy+f8sjhY="
$authServer = "http://localhost:5000/"
$localServer = "http://localhost:1212/"
$userName = "PJB"

$authUrl = $authServer + "api/session/getToken"
$localUrl = $localServer + "info"

$pubKey = Invoke-WebRequest $localUrl |
    select -exp Content |
    ConvertFrom-Json |
    select -exp auth |
    select -exp public_key

$postData = @{"ServerPublicKey"=$pubkey} | ConvertTo-Json

$token = Invoke-WebRequest $authUrl `
    -Method Post -Body $postData `
    -Headers @{"Authorization"="SS14Auth $loginToken"} `
    -ContentType "application/json" |
    select -exp Content

echo $token

bin/Content.Client/Content.Client --launcher --username $userName `
    --cvar "auth.token=$token" `
    --cvar "auth.serverpubkey=$pubKey"
#>

$loginToken = "kSiBszXoB5hOQBAMBC//f2YcxD76Ec36rlKy+f8sjhY="
$authServer = "http://localhost:5000/"
$userName = "PJB"
$userId = "957ebebb-1a06-4a6e-b8ae-f76d98d01adf"

