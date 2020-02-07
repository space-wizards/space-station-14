#!/usr/bin/env pwsh

Get-ChildItem release/*.zip | Get-FileHash -Algorithm SHA256 | ForEach-Object {
    $_.Hash > "$($_.Path).sha256";
}
