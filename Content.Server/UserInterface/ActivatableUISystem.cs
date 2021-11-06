using System.Linq;
using Content.Shared;
using Content.Shared.CCVar;
using Content.Shared.ActionBlocker;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.IoC;

namespace Content.Server.UserInterface
{
    [UsedImplicitly]
    internal sealed class ActivatableUISystem : EntitySystem
    {
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ActivatableUIComponent, ActivateInWorldEvent>(OnActivate);
            SubscribeLocalEvent<ActivatableUIComponent, UseInHandEvent>(OnUseInHand);
        }

        private void OnActivate(EntityUid uid, ActivatableUIComponent component, ActivateInWorldEvent args)
        {
            if (component.InHandsOnly)
                return;

            InteractInstrument(args.User, component);
            args.Handled = true;
        }


        private void OnUseInHand(EntityUid uid, ActivatableUIComponent component, UseInHandEvent args)
        {
            InteractInstrument(args.User, component);
            args.Handled = true;
        }

        private void InteractInstrument(IEntity user, ActivatableUIComponent aui)
        {
            if (!user.TryGetComponent(out ActorComponent? actor)) return;
            if (!_actionBlockerSystem.CanInteract(user)) return;

            if (aui.SingleUser && (aui.CurrentSingleUser != null) && (actor.PlayerSession != aui.CurrentSingleUser))
            {
                // If we get here, supposedly, the object is in use.
                // Check with BUI that it's ACTUALLY in use just in case.
                // Since this could brick the object if it goes wrong.
                if (aui.UserInterface.SubscribedSessions.Count != 0) return;
            }

            aui.SetCurrentSingleUser(actor.PlayerSession);
            aui.UserInterface?.Toggle(actor.PlayerSession);

            return;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var component in EntityManager.EntityQuery<ActivatableUIComponent>(true))
            {
                var ui = component.UserInterface;
                // Done to skip an allocation on anything that's not in use.
                if (ui.SubscribedSessions.Count == 0)
                    continue;
                // Must ToList in order to close things safely.
                foreach (var session in ui.SubscribedSessions.ToList())
                {
                    if (session.AttachedEntityUid == null || !_actionBlockerSystem.CanInteract(session.AttachedEntityUid.Value))
                    {
                        ui.Close(session);
                    }
                }
            }
        }
    }

    public class ActivatableUIPlayerChangedEvent : EntityEventArgs
    {
    }
}
