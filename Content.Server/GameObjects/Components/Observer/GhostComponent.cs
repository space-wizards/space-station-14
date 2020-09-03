using Content.Server.GameObjects.Components.Mobs;
using Content.Server.Mobs;
using Content.Server.Players;
using Content.Shared.GameObjects.Components.Observer;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Players;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Observer
{
    [RegisterComponent]
    public class GhostComponent : SharedGhostComponent
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

        public override void Initialize()
        {
            base.Initialize();

            Owner.EnsureComponent<VisibilityComponent>().Layer = (int) VisibilityFlags.Ghost;
        }

        public override ComponentState GetComponentState() => new GhostComponentState(CanReturnToBody);

        public override void HandleMessage(ComponentMessage message, IComponent component)
        {
            base.HandleMessage(message, component);

            switch (message)
            {
                case PlayerAttachedMsg msg:
                    msg.NewPlayer.VisibilityMask |= (int) VisibilityFlags.Ghost;
                    Dirty();
                    break;
                case PlayerDetachedMsg msg:
                    msg.OldPlayer.VisibilityMask &= ~(int) VisibilityFlags.Ghost;
                    break;
                default:
                    break;
            }
        }

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel,
            ICommonSession session = null)
        {
            base.HandleNetworkMessage(message, netChannel, session);

            switch (message)
            {
                case ReturnToBodyComponentMessage reenter:
                    if (!Owner.TryGetComponent(out IActorComponent actor) || !CanReturnToBody) break;
                    if (netChannel == null || netChannel == actor.playerSession.ConnectedClient)
                    {
                        actor.playerSession.ContentData().Mind.UnVisit();
                        Owner.Delete();
                    }

                    break;
                case ReturnToCloneComponentMessage reenter:

                    if (Owner.TryGetComponent(out VisitingMindComponent mind))
                    {
                        Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new GhostReturnMessage(mind.Mind));
                    }

                    break;
                default:
                    break;
            }
        }

        public class GhostReturnMessage : EntitySystemMessage
        {
            public GhostReturnMessage(Mind sender)
            {
                Sender = sender;
            }

            public Mind Sender { get; }
        }
    }
}
