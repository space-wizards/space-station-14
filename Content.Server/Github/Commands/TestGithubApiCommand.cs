using Content.Server.Administration;
using Content.Server.Github.Requests;
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

        _git.TryMakeRequest(request1);
        _git.TryMakeRequest(request2);

        shell.WriteLine(Loc.GetString("github-command-finish"));
    }
}
