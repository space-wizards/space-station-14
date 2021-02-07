#!/usr/bin/env pwsh

# TODO: This is definitely gonna stop being accurate when we get above 100 contributors on one of the repos.
$engineJson = (Invoke-WebRequest "https://api.github.com/repos/space-wizards/RobustToolbox/contributors?per_page=100").Content | convertfrom-json
$contentJson = (Invoke-WebRequest "https://api.github.com/repos/space-wizards/space-station-14/contributors?per_page=100").Content | convertfrom-json

function load_contribs([string] $repo)
{
    $qParams = @{
        "per_page" = 100
    }

    $url = "https://api.github.com/repos/{0}/contributors" -f $repo

    $r = @()

    while ($null -ne $url)
    {
        $resp = Invoke-WebRequest $url -Body $qParams

        $url = $resp.RelationLink.next

        $j = ConvertFrom-Json $resp.Content
        $r += $j
    }

    return $r
}

$engineJson = load_contribs("space-wizards/RobustToolbox")
$contentJson = load_contribs("space-wizards/space-station-14")

$scriptDir = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent
$bad = get-content $(join-path $scriptDir "ignored_github_contributors.txt")

($engineJson).login + ($contentJson).login `
    | select -unique `
    | where { $bad -notcontains $_ } `
    | Sort-object `
    | Join-String -Separator ", "
