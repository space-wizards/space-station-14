#!/usr/bin/env pwsh

    [cmdletbinding()]

param(
    [Parameter(Mandatory=$true)]
    [DateTime]$since,

    [Parameter(Mandatory=$true)]
    [string]$repo);

$r = @()

$qParams = @{
    "since" = $since.ToString("o")
    "per_page" = 100
}

$url = "https://api.github.com/repos/{0}/commits" -f $repo

while ($null -ne $url)
{
    $resp = Invoke-WebRequest $url -Body $qParams

    $url = $resp.RelationLink.next

    $j = ConvertFrom-Json $resp.Content
    $r += $j
}

return $r
