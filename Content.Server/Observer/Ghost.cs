using Content.Server.Administration;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Observer;
using Content.Server.Interfaces.GameTicking;
using Content.Server.Players;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Mobs.State;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Observer
{
    [AnyCommand]
    public class Ghost : IClientCommand
    {
        public string Command => "ghost";
        public string Description => "Give up on life and become a ghost.";
        public string Help => "ghost";
        public bool CanReturn { get; set; } = true;

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (player == null)
            {
                shell.SendText((IPlayerSession) null, "Nah");
                return;
            }

            var mind = player.ContentData()?.Mind;
            if (mind == null)
            {
                shell.SendText(player, "You can't ghost here!");
                return;
            }

            var playerEntity = player.AttachedEntity;
            var canReturn = playerEntity != null && CanReturn;
            var name = playerEntity?.Name ?? player.Name;

            if (playerEntity != null && playerEntity.HasComponent<GhostComponent>())
                return;

            if (mind.VisitingEntity != null)
            {
                mind.UnVisit();
                mind.VisitingEntity.Delete();
            }

            var position = playerEntity?.Transform.Coordinates ?? IoCManager.Resolve<IGameTicker>().GetObserverSpawnPoint();

            if (canReturn && playerEntity.TryGetComponent(out SharedMobStateComponent mobState))
            {
                if (mobState.IsDead())
                {
                    canReturn = true;
                }
                else if (mobState.IsCritical())
                {
                    canReturn = true;

                    if (playerEntity.TryGetComponent(out IDamageableComponent damageable))
                    {
                        //todo: what if they dont breathe lol
                        damageable.ChangeDamage(DamageType.Asphyxiation, 100, true);
                    }
                }
                else
                {
                    canReturn = false;
                }
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();
            var ghost = entityManager.SpawnEntity("MobObserver", position);
            ghost.Name = mind.CharacterName;

            var ghostComponent = ghost.GetComponent<GhostComponent>();
            ghostComponent.CanReturnToBody = canReturn;

            if (playerEntity != null &&
                playerEntity.TryGetComponent(out ServerOverlayEffectsComponent overlayComponent))
            {
                overlayComponent.RemoveOverlay(SharedOverlayID.CircleMaskOverlay);
            }

            if (canReturn)
                mind.Visit(ghost);
            else
                mind.TransferTo(ghost);
        }
    }
}
