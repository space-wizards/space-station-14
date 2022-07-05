#!/usr/bin/env pwsh

    [cmdletbinding()]

param(
    [Parameter(Mandatory=$true)]
    [DateTime]$since,

    [Nullable[DateTime]]$until);

$scriptDir = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent
. $(join-path $scriptDir contribs_shared.ps1)

$engine = & "$PSScriptRoot\dump_commits_since.ps1" -repo space-wizards/RobustToolbox -since $since -until $until
$content = & "$PSScriptRoot\dump_commits_since.ps1" -repo space-wizards/space-station-14 -since $since -until $until

$contribs = ($content + $engine) `
    | Select-Object -ExpandProperty author `
    | Select-Object -ExpandProperty login -Unique `
    | Where-Object { -not $ignore[$_] }`
    | ForEach-Object { if($replacements[$_] -eq $null){ $_ } else { $replacements[$_] }} `
    | Sort-Object `

$contribs = $contribs -join ", "
Write-Host $contribs
Write-Host "Total commit count is $($engine.Length + $content.Length)"
