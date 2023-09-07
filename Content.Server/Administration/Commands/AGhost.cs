using Content.Server.GameTicking;
using Content.Shared.Administration;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class AGhost : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entities = default!;

        public string Command => "aghost";
        public string Description => "Makes you an admin ghost.";
        public string Help => "aghost";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (player == null)
            {
                shell.WriteLine("Nah");
                return;
            }

            var mindSystem = _entities.System<SharedMindSystem>();
            if (!mindSystem.TryGetMind(player, out var mindId, out var mind))
            {
                shell.WriteLine("You can't ghost here!");
                return;
            }

            var metaDataSystem = _entities.System<MetaDataSystem>();

            if (mind.VisitingEntity != default && _entities.TryGetComponent<GhostComponent>(mind.VisitingEntity, out var oldGhostComponent))
            {
                mindSystem.UnVisit(mindId, mind);
                // If already an admin ghost, then return to body.
                if (oldGhostComponent.CanGhostInteract)
                    return;
            }

            var canReturn = mind.CurrentEntity != null
                            && !_entities.HasComponent<GhostComponent>(mind.CurrentEntity);
            var coordinates = player.AttachedEntity != null
                ? _entities.GetComponent<TransformComponent>(player.AttachedEntity.Value).Coordinates
                : EntitySystem.Get<GameTicker>().GetObserverSpawnPoint();
            var ghost = _entities.SpawnEntity("AdminObserver", coordinates);
            _entities.GetComponent<TransformComponent>(ghost).AttachToGridOrMap();

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
            EntitySystem.Get<SharedGhostSystem>().SetCanReturnToBody(comp, canReturn);
        }
    }
}
