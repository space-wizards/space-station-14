#!/usr/bin/env pwsh

Get-ChildItem release/*.zip | Get-FileHash -Algorithm S | ForEach-Object {
    $_.Hash > "$($_.Path).sha256";
}
