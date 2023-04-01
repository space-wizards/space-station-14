using System.Linq;
using Content.Server.Administration;
using Content.Server.Popups;
using Content.Shared.Administration;
using Content.Shared.Popups;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Timing;

namespace Content.Server.Medical.Surgery;

[AdminCommand(AdminFlags.Fun)]
public sealed class SansCommand : LocalizedCommands
{
    [Dependency] private readonly IPlayerManager _players = default!;

    public override string Command => "sans";
    public override string Description => "You feel like you are going to have a bad time.";
    public override string Help => $"Usage: {Command} | {Command} <player> | {Command} <player1> <player2> | {Command} <player> <music> | {Command} <player1> <player2> <music>";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            if (shell.Player == null)
            {
                shell.WriteError("Specify a player to send to the sans dimension");
                return;
            }

            SansUndertale(new List<IPlayerSession> {(IPlayerSession) shell.Player});
            shell.WriteLine("You are about to have a bad time");
            return;
        }

        var players = new List<IPlayerSession>();
        foreach (var arg in args.SkipLast(1))
        {
            if (_players.TryGetSessionByUsername(arg, out var target))
            {
                players.Add(target);
            }
        }

        SurgeryRealmMusic? music = args[^1].ToLowerInvariant() switch
        {
            "midi" => SurgeryRealmMusic.Midi,
            "megalovania" => SurgeryRealmMusic.Megalovania,
            "undermale" => SurgeryRealmMusic.Undermale,
            _ => null
        };

        SansUndertale(players, music);
        shell.WriteLine($"{string.Join(", ", players.Select(player => player.Name))} are about to have a bad time");
    }

    private void SansUndertale(List<IPlayerSession> sessions, SurgeryRealmMusic? music = null)
    {
        if (sessions.Any(session => session.AttachedEntity == null))
            return;

        foreach (var session in sessions)
        {
            EntitySystem.Get<PopupSystem>().PopupEntity("You feel like you are going to have a bad time", session.AttachedEntity!.Value, PopupType.LargeCaution);
        }

        Timer.Spawn(4000, () =>
        {
            EntitySystem.Get<SurgeryRealmSystem>().StartDuel(sessions, null, music);
        });
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 0)
            return CompletionResult.Empty;

        var options = _players.ServerSessions.OrderBy(c => c.Name).Select(c => c.Name).ToList();
        options.Add("midi");
        options.Add("megalovania");
        options.Add("undermale");

        return CompletionResult.FromHintOptions(options, "<PlayerIndex | Music>");
    }
}

[AdminCommand(AdminFlags.Fun)]
public sealed class UnSansCommand : LocalizedCommands
{
    [Dependency] private readonly IPlayerManager _players = default!;

    public override string Command => "unsans";
    public override string Description => "Stop someone from having a bad time..";
    public override string Help => $"Usage: {Command} | {Command} <player>";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            if (shell.Player == null)
            {
                shell.WriteError("Specify a player to retrieve from the sans dimension");
                return;
            }

            EntitySystem.Get<SurgeryRealmSystem>().StopOperation((IPlayerSession) shell.Player);
            shell.WriteLine("You stopped having a bad time");
            return;
        }

        var name = args[0];

        if (_players.TryGetSessionByUsername(name, out var target))
        {
            EntitySystem.Get<SurgeryRealmSystem>().StopOperation(target);
            shell.WriteLine($"Stopped {target.Name} from having a bad time");
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var options = _players.ServerSessions.OrderBy(c => c.Name).Select(c => c.Name).ToArray();

            return CompletionResult.FromHintOptions(options, "<PlayerIndex>");
        }

        return CompletionResult.Empty;
    }
}
