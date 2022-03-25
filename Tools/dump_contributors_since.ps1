#!/usr/bin/env pwsh

    [cmdletbinding()]

param(
    [Parameter(Mandatory=$true)]
    [DateTime]$since,

    [Nullable[DateTime]]$until);

$replacements = @{
    "moonheart08" = "moony",
    "Elijahrane" = "Rane",
    "ZeroDayDaemon" = "Daemon"
}

$ignore = @{
    "PJBot" = $true
}

$engine = & "$PSScriptRoot\dump_commits_since.ps1" -repo space-wizards/RobustToolbox -since $since -until $until
$content = & "$PSScriptRoot\dump_commits_since.ps1" -repo space-wizards/space-station-14 -since $since -until $until

$contribs = ($content + $engine) `
    | Select-Object -ExpandProperty author `
    | Select-Object -ExpandProperty login -Unique `
    | Where-Object { -not $ignore[$_] }
    | ForEach-Object { $replacements[$_] ?? $_ } 
    | Sort-Object `
    | Join-String -Separator ", "

Write-Host $contribs
Write-Host "Total commit count is $($engine.Length + $content.Length)"
