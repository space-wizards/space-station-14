using System.Linq;
using Content.Server.Administration.Logs;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

/// <summary>
/// Sets a DisplayName for the given user's session, anonymizing the user in non-admin UI.
/// </summary>
[AdminCommand(AdminFlags.Admin)]
public sealed class SetDisplayNameCommand : LocalizedCommands
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IAdminLogManager _adminLogManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override string Command => "setdisplayname";
    public override string Description => Loc.GetString("cmd-displayname-set-description");
    public override string Help =>  Loc.GetString("cmd-displayname-set-help");

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 2)
        {
            var names = _playerManager.Sessions.OrderBy(c => c.Name).Select(c => c.Name);
            return CompletionResult.FromHintOptions(names, Loc.GetString("shell-argument-username-optional-hint"));
        }

        return CompletionResult.Empty;
    }

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length > 2)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        var player = shell.Player;
        if (player == null)
        {
            // If you are not a player, you require a player argument.
            if (args.Length < 2)
            {
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number-need-specific", ("properAmount", 2), ("currentAmount", args.Length)));
                return;
            }

            var didFind = _playerManager.TryGetSessionByUsername(args[1], out player);
            if (!didFind)
            {
                shell.WriteError(Loc.GetString("shell-target-player-does-not-exist"));
                return;
            }
        }

        // If you are a player and a username is provided, a lookup is done to find the target player.
        if (args.Length == 2)
        {
            var didFind = _playerManager.TryGetSessionByUsername(args[1], out player);
            if (!didFind)
            {
                shell.WriteError(Loc.GetString("shell-target-player-does-not-exist"));
                return;
            }
        }

        if (_playerManager.Sessions.Select(c => c.DisplayName).Contains(args[0]))
        {
            shell.WriteError(Loc.GetString("cmd-displayname-name-exists"));
        }

        // Admin ghosts
        if (player!.AttachedEntity != null &&
            _entityManager.HasComponent<GhostComponent>(player.AttachedEntity) &&
            _entityManager.TrySystem(out MetaDataSystem? metaDataSystem) &&
            _entityManager.TryGetComponent<MetaDataComponent>(player.AttachedEntity, out var metaData) &&
            (metaData.EntityName == player.Name || metaData.EntityName == player.DisplayName))
        {
            metaDataSystem.SetEntityName(player.AttachedEntity.Value, args[0]);
        }

        _playerManager.SetDisplayName(player, args[0]);
        _adminLogManager.Add(LogType.AdminCommands,
            LogImpact.Extreme,
            $"{shell.Player!.Name} ({shell.Player!.UserId}) had their display name set to {args[0]}.");
    }
}
