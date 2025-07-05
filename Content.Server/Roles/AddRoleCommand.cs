using Content.Server.Administration;
using Content.Server.Roles.Jobs;
using Content.Shared.Administration;
using Content.Shared.Players;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.Roles;

[AdminCommand(AdminFlags.Admin)]
public sealed class AddRoleCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly JobSystem _jobSystem = default!;

    public override string Command => "addrole";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!_playerManager.TryGetSessionByUsername(args[0], out var data))
        {
            shell.WriteLine(Loc.GetString("shell-target-player-does-not-exist"));
            return;
        }

        if (data.ContentData()?.Mind is not { } mind)
        {
            shell.WriteLine(Loc.GetString("shell-target-player-lacks-mind"));
            return;
        }

        if (!_prototypeManager.TryIndex<JobPrototype>(args[1], out var jobPrototype))
        {
            shell.WriteLine(Loc.GetString("shell-argument-must-be-prototype",
                ("index", args[1]),
                ("prototypeName", nameof(JobPrototype))));
            return;
        }

        if (_jobSystem.MindHasJobWithId(mind, jobPrototype.Name))
        {
            shell.WriteLine(Loc.GetString("cmd-addrole-mind-already-has-role"));
            return;
        }

        _jobSystem.MindAddJob(mind, args[1]);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        switch (args.Length)
        {
            case 1:
                return CompletionResult.FromOptions(CompletionHelper.SessionNames());
            case 2:
            {
                var result = new List<CompletionOption>();

                foreach (var job in _prototypeManager.EnumeratePrototypes<JobPrototype>())
                {
                    var completionOption = new CompletionOption(job.ID, job.Name);

                    result.Add(completionOption);
                }

                return CompletionResult.FromOptions(result);
            }
            default:
                return CompletionResult.Empty;
        }
    }
}
