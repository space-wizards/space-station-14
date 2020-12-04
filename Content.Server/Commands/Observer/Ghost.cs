using Content.Server.Administration;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Observer;
using Content.Server.Interfaces.GameTicking;
using Content.Server.Players;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Commands.Observer
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

            var mind = player.ContentData().Mind;
            if (mind == null)
            {
                shell.SendText(player, "You can't ghost here!");
                return;
            }

            var canReturn = player.AttachedEntity != null && CanReturn;
            var name = player.AttachedEntity?.Name ?? player.Name;

            if (player.AttachedEntity != null && player.AttachedEntity.HasComponent<GhostComponent>())
                return;

            if (mind.VisitingEntity != null)
            {
                mind.UnVisit();
                mind.VisitingEntity.Delete();
            }

            var position = player.AttachedEntity?.Transform.Coordinates ?? IoCManager.Resolve<IGameTicker>().GetObserverSpawnPoint();

            if (canReturn && player.AttachedEntity.TryGetComponent(out IDamageableComponent damageable))
            {
                switch (damageable.CurrentState)
                {
                    case DamageState.Dead:
                        canReturn = true;
                        break;
                    case DamageState.Critical:
                        canReturn = true;
                        damageable.ChangeDamage(DamageType.Asphyxiation, 100, true, null); //todo: what if they dont breathe lol
                        break;
                    default:
                        canReturn = false;
                        break;
                }
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();
            var ghost = entityManager.SpawnEntity("MobObserver", position);
            ghost.Name = mind.CharacterName;

            var ghostComponent = ghost.GetComponent<GhostComponent>();
            ghostComponent.CanReturnToBody = canReturn;

            if (player.AttachedEntity.TryGetComponent(out ServerOverlayEffectsComponent overlayComponent))
            {
                overlayComponent?.RemoveOverlay(SharedOverlayID.CircleMaskOverlay);
            }

            if (canReturn)
                mind.Visit(ghost);
            else
                mind.TransferTo(ghost);
        }
    }
}
