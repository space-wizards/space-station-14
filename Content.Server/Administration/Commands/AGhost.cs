using Content.Server.GameTicking;
using Content.Server.Ghost.Components;
using Content.Server.Players;
using Content.Shared.Administration;
using Content.Shared.Ghost;
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

            var mind = player.ContentData()?.Mind;

            if (mind == null)
            {
                shell.WriteLine("You can't ghost here!");
                return;
            }

            if (mind.VisitingEntity != default && _entities.HasComponent<GhostComponent>(mind.VisitingEntity))
            {
                player.ContentData()!.Mind?.UnVisit();
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
                if(!string.IsNullOrWhiteSpace(mind.CharacterName))
                    _entities.GetComponent<MetaDataComponent>(ghost).EntityName = mind.CharacterName;
                else if (!string.IsNullOrWhiteSpace(mind.Session?.Name))
                    _entities.GetComponent<MetaDataComponent>(ghost).EntityName = mind.Session.Name;

                mind.Visit(ghost);
            }
            else
            {
                _entities.GetComponent<MetaDataComponent>(ghost).EntityName = player.Name;
                mind.TransferTo(ghost);
            }

            var comp = _entities.GetComponent<GhostComponent>(ghost);
            EntitySystem.Get<SharedGhostSystem>().SetCanReturnToBody(comp, canReturn);
        }
    }
}
