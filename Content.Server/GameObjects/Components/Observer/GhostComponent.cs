using Content.Server.GameObjects.EntitySystems;
using Content.Server.Players;
using Content.Shared.GameObjects.Components.Observer;
using Robust.Server.GameObjects;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.ViewVariables;
using Timer = Robust.Shared.Timers.Timer;


namespace Content.Server.GameObjects.Components.Observer
{
    [RegisterComponent]
    public class GhostComponent : SharedGhostComponent, IActionBlocker
    {
        private bool _canReturnToBody = true;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanReturnToBody
        {
            get => _canReturnToBody;
            set
            {
                _canReturnToBody = value;
                Dirty();
            }
        }

        public override ComponentState GetComponentState() => new GhostComponentState(CanReturnToBody);

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null,
            IComponent component = null)
        {
            base.HandleMessage(message, netChannel, component);

            switch (message)
            {
                case ReturnToBodyComponentMessage reenter:
                    if (!Owner.TryGetComponent(out IActorComponent actor) || !CanReturnToBody) break;
                    if (netChannel == null || netChannel == actor.playerSession.ConnectedClient)
                    {
                        actor.playerSession.ContentData().Mind.UnVisit();
                    }
                    break;
                case PlayerAttachedMsg _:
                    Dirty();
                    break;
                case PlayerDetachedMsg _:
                    Timer.Spawn(100, Owner.Delete);
                    break;
                default:
                    break;
            }
        }

        public bool CanInteract() => false;
        public bool CanUse() => false;
        public bool CanThrow() => false;
        public bool CanDrop() => false;
        public bool CanPickup() => false;
        public bool CanEmote() => false;
        public bool CanAttack() => false;
    }
}
