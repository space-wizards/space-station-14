using Content.Server.GameTicking;
using Content.Server.Ghost.Components;
using Content.Server.Hands.Systems;
using Content.Server.Players;
using Content.Shared.Administration;
using Content.Shared.Ghost;
using Content.Shared.Hands.Components;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Map;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class AGhost : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entities = default!;

        public string Command => "aghost";
        public string Description => "Makes you an admin ghost. If you already are - simple ghost";
        public string Help => "aghost";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (player == null)
            {
                shell.WriteLine($"Nah: could not find player's session, skipping command {Command}");
                return;
            }

            var mind = player.ContentData()?.Mind;

            if (mind == null)
            {
                shell.WriteLine("You can't ghost here! Could not find 'mind'");
                return;
            }

            //try to find entity, where we can return player's mind
            if (mind.VisitingEntity != default && _entities.HasComponent<GhostComponent>(mind.VisitingEntity))
            {
                player.ContentData()!.Mind?.UnVisit();
                return;
            }

            var canReturn = mind.CurrentEntity != null
                            && !_entities.HasComponent<GhostComponent>(mind.CurrentEntity);
            var coordinates = player.AttachedEntity != null
                ? _entities.GetComponent<TransformComponent>(player.AttachedEntity.Value).Coordinates
                : _entities.System<GameTicker>().GetObserverSpawnPoint();
            var ghost = SpawnGhost(coordinates, player, canReturn);
            _entities.GetComponent<TransformComponent>(ghost).AttachToGridOrMap();

            if (canReturn)
            {
                // TODO: Remove duplication between all this and "GamePreset.OnGhostAttempt()"...
                if (!string.IsNullOrWhiteSpace(mind.CharacterName))
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
            _entities.System<SharedGhostSystem>().SetCanReturnToBody(comp, canReturn);
        }

        /**
         * Choose ghost prototype based on current player's state:
         * - if player can return back to body -> aghost
         * - else if player is aghost -> ghost
         * - else -> aghost
         */
        private EntityUid SpawnGhost(EntityCoordinates coordinates, IPlayerSession player, bool canReturn)
        {
            if (canReturn)
            {
                return _entities.SpawnEntity("AdminObserver", coordinates);
            }

            //check if current player is aghost
            var playerAttachedEntity = player.AttachedEntity;
            if (playerAttachedEntity is { Valid: true } playerEntity &&
                _entities.GetComponent<MetaDataComponent>(playerEntity).EntityPrototype?.ID == "AdminObserver")
            {
                EmptyHands(playerAttachedEntity);
                return _entities.SpawnEntity("MobObserver", coordinates);
            }

            return _entities.SpawnEntity("AdminObserver", coordinates);
        }

        private void EmptyHands(EntityUid? playerAttachedEntity)
        {
            if (playerAttachedEntity == null)
                return;
            var handsSystem = _entities.System<HandsSystem>();
            var handsComponent = _entities.GetComponent<HandsComponent>(playerAttachedEntity.Value);
            handsSystem.TryDrop(playerAttachedEntity.Value, checkActionBlocker: false, doDropInteraction: false,
                handsComp: handsComponent);
        }
    }
}
