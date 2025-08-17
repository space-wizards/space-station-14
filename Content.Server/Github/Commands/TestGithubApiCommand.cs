using Content.Server.Administration;
using Content.Server.Github.Requests;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Server.Github.Commands;

/// <summary>
/// Simple command for testing if the GitHub api is set up correctly! It ensures that all necessary ccvars are set,
/// and will also create one new issue on the targeted repository.
/// </summary>
[AdminCommand(AdminFlags.Server)]
public sealed class TestGithubApiCommand : LocalizedCommands
{
    [Dependency] private readonly GithubApiManager _git = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override string Command => Loc.GetString("github-command-test-name");

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var enabled = _cfg.GetCVar(CCVars.GithubEnabled);
        var path = _cfg.GetCVar(CCVars.GithubAppPrivateKeyPath);
        var appId = _cfg.GetCVar(CCVars.GithubAppId);
        var repoName = _cfg.GetCVar(CCVars.GithubRepositoryName);
        var owner = _cfg.GetCVar(CCVars.GithubRepositoryOwner);

        if (!enabled)
        {
            shell.WriteError(Loc.GetString("github-command-not-enabled"));
            return;
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            shell.WriteError(Loc.GetString("github-command-no-path"));
            return;
        }

        if (string.IsNullOrWhiteSpace(appId))
        {
            shell.WriteError(Loc.GetString("github-command-no-app-id"));
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
        var request = new CreateIssueRequest
        {
            Title = Loc.GetString("github-command-issue-title-one"),
            Body = Loc.GetString("github-command-issue-description-one"),
        };

        _git.TryMakeRequest(request);

        shell.WriteLine(Loc.GetString("github-command-finish"));
    }
}
