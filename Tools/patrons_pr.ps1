#!/usr/bin/env pwsh

param([string]$csvPath)

# Dumps Patreon's CSV download into the list for the progress report

# Have to trim patron names because apparently Patreon doesn't which is quite ridiculous.
Get-content $csvPath | ConvertFrom-Csv -Delimiter "," | select @{l="Name";e={$_.Name.Trim()}} | Sort-Object Name | Join-String Name -Separator ", "
