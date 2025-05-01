#!/usr/bin/env pwsh

$scriptDir = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent
. $(join-path $scriptDir contribs_shared.ps1)

if ($null -eq $env:GITHUB_TOKEN)
{
    throw "A GitHub API token is required to run this script properly without being rate limited. If you're a user, generate a personal access token and use that. If you're running this in a GitHub action, make sure you expose the GITHUB_TOKEN secret as an environment variable."
}

function load_contribs([string] $repo)
{
    # https://developer.github.com/enterprise/2.8/v3/repos/#list-contributors
    # We use the ?anon=1 query param for reasons explained later.
    $qParams = @{
        "per_page" = 100
        "anon" = 1
    }

    $headers = @{
        Authorization="Bearer $env:GITHUB_TOKEN"
    }

    $url = "https://api.github.com/repos/{0}/contributors" -f $repo

    $r = @()

    while ($null -ne $url)
    {
        $resp = Invoke-WebRequest $url -Body $qParams -Headers $headers

        $url = $resp.RelationLink.next

        $j = ConvertFrom-Json $resp.Content
        $r += $j
    }

    # After collecting all the paginated data, we still aren't done.
    # GitHub's API, for some reason, has a hard cap on 500 email addresses per repo which it will collate
    # SS14 has gone past this limit for quite some time, so GitHub will stop including accounts, starting
    # with those that have lower contributions, as valid distinct users with a `login` field.
    # 
    # This is obviously a problem.
    # To remedy, we first use the ?anon=1 parameter to force GitHub to include all committers emails, even
    # those that it has, in its great and infinite wisdom, chosen to not properly attach to a GitHub account.
    #
    # Of course, this is normally an issue -- we use this API specifically because we want to only get
    # committers with valid GitHub accounts, otherwise we pollute the contributor log with random aliases
    # and names that people don't use, things like that.
    #
    # So, okay, solution:
    # 1) Go over our list, and check for ones which only have a `name` and `email` field ('anonymous' contributors)
    #    and which dont already appear.
    # 2) Check to see if the email ends with `@users.noreply.github.com`.
    #    - To my knowledge, GitHub includes an email in the form of `(numbers)+(username)@users.noreply.github.com`
    #    - when commits are made using someones GitHub account, and they aren't attaching another email to their account
    # 3) If an email of this form was found, we can assume this is one of the 'missing' contribs and extract their GitHub username.
    # 4) If an email of this form -wasn't- found, but they're still anonymous, we -unfortunately- still have to check if they're a valid GitHub user
    #    because GitHub might have just force-anonymized them anyway!
    #
    #    It's possible their `name` is a valid GitHub user, but that this is a coincidence and they aren't actually a contributor.
    #    There is kind of not really jack shit we can do about that! It's not that common though and it's probably more likely to attribute
    #    correctly than not.
    # 5) Then, we just add a `login` field to our object with their true username and let the rest of the code do its job.

    foreach ($contributor in $r) 
    {
        if ($null -ne $contributor.name `
            -And $null -ne $contributor.email `
            -And $contributor.email -match '\d+\+(.*)@users\.noreply\.github\.com$')
        {
            $username = $Matches.1
            # Use their `name` if its equivalent to the extracted username,
            # since that one will have proper casing. Otherwise just let them be a lowercasecel
            if ($contributor.name.ToLower() -eq $username)
            {
                $username = $contributor.name
            }

            if (($r).login -contains $username)
            {
                continue
            }
            
            $contributor | Add-Member -MemberType NoteProperty -Name "login" -Value $username
        }
        elseif ($null -eq $contributor.login `
                 -And $null -ne $contributor.name `
                 -And !$contributor.name.Contains(" "))
        {
            $username = $contributor.name
            # They're an anonymous user, without a GH email, and their name doesn't contain a space
            # (since a valid GH username can't have a space)
            # Might still be a valid contrib???
            if (($r).login -contains $username)
            {
                continue
            }

            $userUrl = "https://api.github.com/users/{0}" -f $username

            try
            {
                $userResp = Invoke-WebRequest $userUrl -Headers $headers
                $userJ = ConvertFrom-Json $userResp.Content
                $contributor | Add-Member -MemberType NoteProperty -Name "login" -Value $userJ.login
            }
            catch {} # if it 404s do nothing. powershell doesn't seem to really have a simpler way to do this.
        }
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