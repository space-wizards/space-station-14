using System.Linq;
using Content.Server.Administration.Logs;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

/// <summary>
/// Clears the DisplayName for a given user's session, returning the user to displaying as their session Name in non-admin UI.
/// </summary>
[AdminCommand(AdminFlags.Admin)]
public sealed class ClearDisplayNameCommand : LocalizedCommands
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IAdminLogManager _adminLogManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override string Command => "cleardisplayname";
    public override string Description => Loc.GetString("cmd-displayname-clear-description");
    public override string Help => Loc.GetString("cmd-displayname-clear-help");

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var names = _playerManager.Sessions.OrderBy(c => c.Name).Select(c => c.Name);
            return CompletionResult.FromHintOptions(names, LocalizationManager.GetString("shell-argument-username-optional-hint"));
        }

        return CompletionResult.Empty;
    }

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length > 1)
        {
            shell.WriteError(LocalizationManager.GetString("shell-wrong-arguments-number"));
            return;
        }

        var player = shell.Player;
        if (player == null)
        {
            // If you are not a player, you require a player argument.
            if (args.Length == 0)
            {
                shell.WriteError(LocalizationManager.GetString("shell-need-exactly-one-argument"));
                return;
            }

            var didFind = _playerManager.TryGetSessionByUsername(args[0], out player);
            if (!didFind)
            {
                shell.WriteError(LocalizationManager.GetString("shell-target-player-does-not-exist"));
                return;
            }
        }

        // If you are a player and a username is provided, a lookup is done to find the target player.
        if (args.Length == 1)
        {
            var didFind = _playerManager.TryGetSessionByUsername(args[0], out player);
            if (!didFind)
            {
                shell.WriteError(LocalizationManager.GetString("shell-target-player-does-not-exist"));
                return;
            }
        }

        // Admin ghosts
        if (player!.AttachedEntity != null &&
            _entityManager.HasComponent<GhostComponent>(player.AttachedEntity) &&
            _entityManager.TrySystem(out MetaDataSystem? metaDataSystem) &&
            _entityManager.TryGetComponent<MetaDataComponent>(player.AttachedEntity, out var metaData) &&
            metaData.EntityName == player.DisplayName)
        {
            metaDataSystem.SetEntityName(player.AttachedEntity.Value, player.Name);
        }

        _playerManager.SetDisplayName(player!, string.Empty);
        _adminLogManager.Add(LogType.AdminCommands,
            LogImpact.Extreme,
            $"{shell.Player!.Name} ({shell.Player!.UserId}) had their display name cleared.");
    }
}
