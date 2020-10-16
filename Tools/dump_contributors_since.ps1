#!/usr/bin/env pwsh

    [cmdletbinding()]

param(
    [Parameter(Mandatory=$true)]
    [DateTime]$since);

$engine = & "$PSScriptRoot\dump_commits_since.ps1" -repo space-wizards/RobustToolbox -since $since
$content = & "$PSScriptRoot\dump_commits_since.ps1" -repo space-wizards/space-station-14 -since $since

($content + $engine) `
    | Select-Object -ExpandProperty author `
    | Select-Object -ExpandProperty login -Unique `
    | Sort-Object `
    | Join-String -Separator ", "
