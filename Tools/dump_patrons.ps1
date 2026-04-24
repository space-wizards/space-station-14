#!/usr/bin/env pwsh

param([string]$csvPath)

# Dumps Patreon's CSV download into a YAML file the game reads.

# Have to trim patron names because apparently Patreon doesn't which is quite ridiculous.
Get-content $csvPath | ConvertFrom-Csv -Delimiter "," | select @{l="Name";e={$_.Name.Trim()}},Tier | where-object Tier -ne "" | where-object Tier -ne "Free" | ConvertTo-Yaml
