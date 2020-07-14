#!/usr/bin/env pwsh

# TODO: This is definitely gonna stop being accurate when we get above 100 contributors on one of the repos.
$engineJson = (Invoke-WebRequest "https://api.github.com/repos/space-wizards/RobustToolbox/contributors?per_page=100").Content | convertfrom-json
$contentJson = (Invoke-WebRequest "https://api.github.com/repos/space-wizards/space-station-14/contributors?per_page=100").Content | convertfrom-json

if ($engineJson.Count -ge 100)
{
    Write-Warning "Engine is reporting 100 contributors. It might not be a complete list due to API pagination!"
}

if ($contentJson.Count -ge 100)
{
    Write-Warning "Content is reporting 100 contributors. It might not be a complete list due to API pagination!"
}

$scriptDir = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent
$bad = get-content $(join-path $scriptDir "ignored_github_contributors.txt")

($engineJson).login + ($contentJson).login | select -unique | where { $bad -notcontains $_ } | Sort-object | Join-String -Separator ", "
