using System.Linq;
using Content.Server.Preferences.Managers;
using Content.Shared.Administration;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Commands;

/// <summary>
/// Updates the selected character's job preference for a connected player.
/// </summary>
[AdminCommand(AdminFlags.Round)]
public sealed partial class SetJobPriorityCommand : LocalizedCommands
{
    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private IServerPreferencesManager _preferences = default!;

    public override string Command => "setjobpriority";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 3)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number-need-specific",
                ("properAmount", 3),
                ("currentAmount", args.Length)));
            shell.WriteLine(Help);
            return;
        }

        if (!_players.TryGetSessionByUsername(args[0], out var player))
        {
            shell.WriteError(Loc.GetString("cmd-setjobpriority-player-not-found", ("player", args[0])));
            return;
        }

        var job = new ProtoId<JobPrototype>(args[1]);
        if (!_prototypes.HasIndex(job))
        {
            shell.WriteError(Loc.GetString("cmd-setjobpriority-job-not-found", ("job", job.Id)));
            return;
        }

        if (!Enum.TryParse<JobPriority>(args[2], true, out var priority) ||
            !Enum.IsDefined(priority))
        {
            shell.WriteError(Loc.GetString("cmd-setjobpriority-invalid-priority", ("priority", args[2])));
            return;
        }

        if (!_preferences.HavePreferencesLoaded(player))
        {
            shell.WriteError(Loc.GetString("cmd-setjobpriority-preferences-not-loaded", ("player", player.Name)));
            return;
        }

        var preferences = _preferences.GetPreferences(player.UserId);
        var slot = preferences.SelectedCharacterIndex;
        var profile = preferences.SelectedCharacter.WithJobPriority(job, priority);
        await _preferences.SetProfile(player.UserId, slot, profile);

        shell.WriteLine(Loc.GetString("cmd-setjobpriority-success",
            ("player", player.Name),
            ("job", job.Id),
            ("priority", priority)));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(_players.Sessions.Select(player => player.Name),
                Loc.GetString("cmd-setjobpriority-hint-player")),
            2 => CompletionResult.FromHintOptions(_prototypes.EnumeratePrototypes<JobPrototype>().Select(job => job.ID),
                Loc.GetString("cmd-setjobpriority-hint-job")),
            3 => CompletionResult.FromHintOptions(Enum.GetNames<JobPriority>(),
                Loc.GetString("cmd-setjobpriority-hint-priority")),
            _ => CompletionResult.Empty,
        };
    }
}
