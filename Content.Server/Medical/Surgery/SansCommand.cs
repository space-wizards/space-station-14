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
    public override string Help => $"Usage: {Command} | {Command} <player> | {Command} <player> <music>";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            if (shell.Player == null)
            {
                shell.WriteError("Specify a player to send to the sans dimension");
                return;
            }

            SansUndertale((IPlayerSession) shell.Player);
            shell.WriteLine("You are about to have a bad time");
            return;
        }

        var name = args[0];

        SurgeryRealmMusic? music = null;
        if (args.Length == 2)
        {
            music = args[1].ToLowerInvariant() switch
            {
                "midi" => SurgeryRealmMusic.Midi,
                "megalovania" => SurgeryRealmMusic.Megalovania,
                "undermale" => SurgeryRealmMusic.Undermale,
                _ => null
            };
        }

        if (_players.TryGetSessionByUsername(name, out var target))
        {
            SansUndertale(target, music);
            shell.WriteLine($"{target.Name} is about to have a bad time");
        }
    }

    private void SansUndertale(IPlayerSession session, SurgeryRealmMusic? music = null)
    {
        if (session.AttachedEntity == null)
            return;

        EntitySystem.Get<PopupSystem>().PopupEntity("You feel like you are going to have a bad time", session.AttachedEntity.Value, PopupType.LargeCaution);

        Timer.Spawn(4000, () =>
        {
            EntitySystem.Get<SurgeryRealmSystem>().StartOperation(session, null, music);
        });
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var options = _players.ServerSessions.OrderBy(c => c.Name).Select(c => c.Name).ToArray();

            return CompletionResult.FromHintOptions(options, "<PlayerIndex>");
        }

        if (args.Length == 2)
        {
            var options = new[] {"midi", "megalovania", "undermale"};

            return CompletionResult.FromHintOptions(options, "<Music>");
        }

        return CompletionResult.Empty;
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
