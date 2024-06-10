#!/usr/bin/env pwsh

$scriptDir = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent
. $(join-path $scriptDir contribs_shared.ps1)

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

($engineJson).login + ($contentJson).login + ($add) `
    | select -unique `
    | Where-Object { -not $ignore[$_] }`
    | ForEach-Object { if($replacements[$_] -eq $null){ $_ } else { $replacements[$_] }} `
    | Sort-object `
    | Join-String -Separator ", "
