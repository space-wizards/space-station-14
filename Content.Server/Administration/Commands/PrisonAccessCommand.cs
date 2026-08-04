using Content.Server.Database;
using Content.Server.DeadSpace.Prison;
using Content.Shared.Administration;
using Content.Shared.Database;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Ban)]
public sealed class PrisonAccessCommand : LocalizedCommands
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IEntityManager _entities = default!;

    public override string Command => "prison_access";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteLine(Help);
            return;
        }

        if (!int.TryParse(args[0], out var banId))
        {
            shell.WriteError(Loc.GetString("cmd-prison_access-invalid-id", ("id", args[0])));
            return;
        }

        bool sendToPrison;
        switch (args[1].ToLowerInvariant())
        {
            case "on":
                sendToPrison = true;
                break;
            case "off":
                sendToPrison = false;
                break;
            default:
                shell.WriteError(Loc.GetString("cmd-prison_access-invalid-mode", ("mode", args[1])));
                return;
        }

        var ban = await _db.GetBanAsync(banId);
        if (ban == null || ban.Type != BanType.Server)
        {
            shell.WriteError(Loc.GetString("cmd-prison_access-not-server-ban", ("id", banId)));
            return;
        }

        if (ban.Unban != null || ban.ExpirationTime is { } expiration && expiration <= DateTimeOffset.UtcNow)
        {
            shell.WriteError(Loc.GetString("cmd-prison_access-inactive", ("id", banId)));
            return;
        }

        await _db.SetBanPrisonAccess(banId, sendToPrison);
        _entities.System<PrisonSystem>().RefreshPrisonBanState();

        shell.WriteLine(Loc.GetString(
            "cmd-prison_access-success",
            ("id", banId),
            ("mode", sendToPrison ? "on" : "off")));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length == 2
            ? CompletionResult.FromHintOptions(["on", "off"], Loc.GetString("cmd-prison_access-arg-mode"))
            : CompletionResult.Empty;
    }
}
