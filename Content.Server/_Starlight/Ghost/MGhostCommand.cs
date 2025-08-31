using Content.Server.GameTicking;
using Content.Shared.Administration;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared._NullLink;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Console;

namespace Content.Server._Starlight.Ghost;

[UsedImplicitly, AnyCommand]
public sealed class MGhostCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly ISharedNullLinkPlayerRolesReqManager _playerRoles = default!;

    public override string Command => "mghost";
    public override string Help => "mghost";

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args) => CompletionResult.Empty;

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length > 0)
        {
            shell.WriteError(LocalizationManager.GetString("shell-wrong-arguments-number"));
            return;
        }

        var player = shell.Player;
        var self = player != null;
        if (player == null)
        {
            shell.WriteError(LocalizationManager.GetString("shell-only-players-can-run-this-command"));
            return;
        }

        if (!_playerRoles.IsMentor(player)) // if you are a admin you should be using aghost
        {
            shell.WriteError(LocalizationManager.GetString("mghost-mentors-only"));
            return;
        }

        var mindSystem = _entities.System<SharedMindSystem>();
        var metaDataSystem = _entities.System<MetaDataSystem>();
        var ghostSystem = _entities.System<SharedGhostSystem>();
        var transformSystem = _entities.System<TransformSystem>();
        var gameTicker = _entities.System<GameTicker>();

        if (!mindSystem.TryGetMind(player, out var mindId, out var mind))
        {
            shell.WriteError(LocalizationManager.GetString("aghost-no-mind-self"));
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
        var ghost = _entities.SpawnEntity(GameTicker.ObserverPrototypeName, coordinates); //dont spawn a aghost. spawn a normal ghost you can return from.
        transformSystem.AttachToGridOrMap(ghost, _entities.GetComponent<TransformComponent>(ghost));

        if (canReturn)
        {
            // TODO: Remove duplication between all this and "GamePreset.OnGhostAttempt()"...
            if (!string.IsNullOrWhiteSpace(mind.CharacterName))
                metaDataSystem.SetEntityName(ghost, mind.CharacterName);
            else if (!string.IsNullOrWhiteSpace(player.Name))
                metaDataSystem.SetEntityName(ghost, player.Name);

            mindSystem.Visit(mindId, ghost, mind);
        }
        else
        {
            metaDataSystem.SetEntityName(ghost, player.Name);
            mindSystem.TransferTo(mindId, ghost, mind: mind);
        }

        var comp = _entities.GetComponent<GhostComponent>(ghost);
        ghostSystem.SetCanReturnToBody((ghost, comp), canReturn);
    }
}
