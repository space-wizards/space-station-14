using System.Net;
using System.Net.Http.Json;
using Content.Server.Administration;
using Content.Server.Github.Requests;
using Content.Server.Github.Responses;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Server.Github.Commands;

/// <summary>
/// Simple command for testing if the GitHub api is set up correctly!
/// </summary>
[AdminCommand(AdminFlags.Server)]
public sealed class TestGithubApiCommand : LocalizedEntityCommands
{
    [Dependency] private readonly GithubApiManager _git = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override string Command => Loc.GetString("github-command-test-name");

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var enabled = _cfg.GetCVar(CCVars.GithubEnabled);
        var auth = _cfg.GetCVar(CCVars.GithubAuthToken);
        var repoName = _cfg.GetCVar(CCVars.GithubRepositoryName);
        var owner = _cfg.GetCVar(CCVars.GithubRepositoryOwner);

        if (!enabled)
        {
            shell.WriteError(Loc.GetString("github-command-not-enabled"));
            return;
        }

        if (string.IsNullOrWhiteSpace(auth))
        {
            shell.WriteError(Loc.GetString("github-command-no-auth"));
            return;
        }

        if (string.IsNullOrWhiteSpace(repoName))
        {
            shell.WriteError(Loc.GetString("github-command-no-repo-name"));
            return;
        }

        if (string.IsNullOrWhiteSpace(owner))
        {
            shell.WriteError(Loc.GetString("github-command-no-owner"));
            return;
        }

        // Rate limit request
        var rateLimitRequest = new GetRateLimit();
        var rateLimitResponse = await _git.TryMakeRequest(rateLimitRequest);

        if (rateLimitResponse == null)
        {
            shell.WriteError(Loc.GetString("github-command-could-not-request"));
            return;
        }

        var rateLimitRespJson = await rateLimitResponse.Content.ReadFromJsonAsync<RateLimitResponse>();

        if (rateLimitRespJson == null || !rateLimitRequest.GetExpectedResponseCodes().Contains(rateLimitResponse.StatusCode))
        {
            shell.WriteError(Loc.GetString("github-command-rate-limit-resp-fail", ("error", rateLimitResponse.StatusCode)));
            return;
        }

        var remainingRequests = rateLimitRespJson.Resources.Core.Remaining;

        if (remainingRequests == 0)
        {
            shell.WriteError("github-command-rate-limit-limit-reached");
            return;
        }

        shell.WriteLine(Loc.GetString("github-command-rate-limit-success", ("rateLimit", remainingRequests.ToString())));

        // Make zen request
        var zenRequest = new GetZen();
        var zenResponse = await _git.TryMakeRequest(zenRequest);

        if (zenResponse == null)
        {
            shell.WriteError(Loc.GetString("github-command-could-not-request"));
            return;
        }

        if (!rateLimitRequest.GetExpectedResponseCodes().Contains(zenResponse.StatusCode))
        {
            shell.WriteError(Loc.GetString("github-command-zen-failure", ("error", zenResponse.StatusCode)));
            return;
        }

        var zenText = await zenResponse.Content.ReadAsStringAsync();
        shell.WriteLine(Loc.GetString("github-command-zen-success", ("zen", zenText)));

        // Create two issues and send them to the api.
        var request1 = new CreateIssue
        {
            Title = Loc.GetString("github-command-issue-title-one"),
            Body = Loc.GetString("github-command-issue-description-one"),
        };

        var request2 = new CreateIssue
        {
            Title = Loc.GetString("github-command-issue-title-two"),
            Body = Loc.GetString("github-command-issue-description-two"),
        };

        var task1 = _git.TryMakeRequestSafe(request1);
        var task2 = _git.TryMakeRequestSafe(request2);

        var response1 = await task1;
        var response2 = await task2;

        if (response1 == null || response2 == null)
        {
            shell.WriteError(Loc.GetString("github-command-could-not-request"));
            return;
        }

        if (!request1.GetExpectedResponseCodes().Contains(response1.StatusCode))
            shell.WriteError(Loc.GetString("github-command-issue-failure", ("error", response1.StatusCode)));
        else
            shell.WriteLine(Loc.GetString("github-command-issue-success"));

        if (!request2.GetExpectedResponseCodes().Contains(response2.StatusCode))
            shell.WriteError(Loc.GetString("github-command-issue-failure", ("error", response2.StatusCode)));
        else
            shell.WriteLine(Loc.GetString("github-command-issue-success"));


        shell.WriteLine(Loc.GetString("github-command-finish"));
    }
}
