using System.Linq;
using Content.Server.GameTicking;
using Content.Server.Ghost;
using Content.Server.Mind;
using Content.Shared.Administration;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class AGhost : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override string Command => "aghost";
    public override string Description => LocalizationManager.GetString("aghost-description");
    public override string Help => "aghost";

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var names = _playerManager.Sessions.OrderBy(c => c.Name).Select(c => c.Name).ToArray();
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
        var self = player != null;
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

        var mindSystem = _entities.System<SharedMindSystem>();
        var metaDataSystem = _entities.System<MetaDataSystem>();
        var ghostSystem = _entities.System<SharedGhostSystem>();
        var transformSystem = _entities.System<TransformSystem>();
        var gameTicker = _entities.System<GameTicker>();

        if (!mindSystem.TryGetMind(player, out var mindId, out var mind))
        {
            shell.WriteError(self
                ? LocalizationManager.GetString("aghost-no-mind-self")
                : LocalizationManager.GetString("aghost-no-mind-other"));
            return;
        }

        if (mind.VisitingEntity != default && _entities.TryGetComponent<GhostComponent>(mind.VisitingEntity, out var oldGhostComponent))
        {
            mindSystem.UnVisit(mindId, mind);
            // If already an admin ghost, then return to body.
            if (oldGhostComponent.CanGhostInteract)
                return;
        }

        var canReturn = mind.CurrentEntity != null
                        && !_entities.HasComponent<GhostComponent>(mind.CurrentEntity);
        var coordinates = player!.AttachedEntity != null
            ? _entities.GetComponent<TransformComponent>(player.AttachedEntity.Value).Coordinates
            : gameTicker.GetObserverSpawnPoint();
        var ghost = _entities.SpawnEntity(GameTicker.AdminObserverPrototypeName, coordinates);
        transformSystem.AttachToGridOrMap(ghost, _entities.GetComponent<TransformComponent>(ghost));

        if (canReturn)
        {
            // TODO: Remove duplication between all this and "GamePreset.OnGhostAttempt()"...
            if (!string.IsNullOrWhiteSpace(mind.CharacterName))
                metaDataSystem.SetEntityName(ghost, mind.CharacterName);
            else if (!string.IsNullOrWhiteSpace(mind.Session?.Name))
                metaDataSystem.SetEntityName(ghost, mind.Session.Name);

            mindSystem.Visit(mindId, ghost, mind);
        }
        else
        {
            metaDataSystem.SetEntityName(ghost, player.Name);
            mindSystem.TransferTo(mindId, ghost, mind: mind);
        }

        var comp = _entities.GetComponent<GhostComponent>(ghost);
        ghostSystem.SetCanReturnToBody(comp, canReturn);
    }
}
