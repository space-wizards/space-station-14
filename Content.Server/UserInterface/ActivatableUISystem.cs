using System.Linq;
using Content.Shared;
using Content.Shared.CCVar;
using Content.Shared.ActionBlocker;
using Content.Shared.Hands;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Server.Administration.Managers;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Localization;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.IoC;

namespace Content.Server.UserInterface
{
    [UsedImplicitly]
    internal sealed class ActivatableUISystem : EntitySystem
    {
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly IAdminManager _adminManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ActivatableUIComponent, ActivateInWorldEvent>(OnActivate);
            SubscribeLocalEvent<ActivatableUIComponent, UseInHandEvent>(OnUseInHand);
            SubscribeLocalEvent<ActivatableUIComponent, DroppedEvent>(OnDropped);
            SubscribeLocalEvent<ActivatableUIComponent, ThrownEvent>(OnThrown);
            SubscribeLocalEvent<ActivatableUIComponent, HandDeselectedEvent>(OnHandDeselected);
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

        private void OnDropped(EntityUid uid, ActivatableUIComponent component, DroppedEvent args)
        {
            component.UserInterface.CloseAll();
        }

        private void OnThrown(EntityUid uid, ActivatableUIComponent component, ThrownEvent args)
        {
            component.UserInterface.CloseAll();
        }

        private void OnHandDeselected(EntityUid uid, ActivatableUIComponent component, HandDeselectedEvent args)
        {
            component.UserInterface.CloseAll();
        }

        private void InteractInstrument(IEntity user, ActivatableUIComponent aui)
        {
            if (!user.TryGetComponent(out ActorComponent? actor)) return;

            if (!_actionBlockerSystem.CanInteract(user))
            {
                user.PopupMessageCursor(Loc.GetString("base-computer-ui-component-cannot-interact"));
                return;
            }

            if (aui.AdminOnly && !_adminManager.IsAdmin(actor.PlayerSession)) return;

            if (aui.SingleUser && (aui.CurrentSingleUser != null) && (actor.PlayerSession != aui.CurrentSingleUser))
            {
                // If we get here, supposedly, the object is in use.
                // Check with BUI that it's ACTUALLY in use just in case.
                // Since this could brick the object if it goes wrong.
                if (aui.UserInterface.SubscribedSessions.Count != 0) return;
            }

            // If we've gotten this far, fire a cancellable event that indicates someone is about to activate this.
            // This is so that stuff can require further conditions (like power).
            var oae = new ActivatableUIOpenAttemptEvent(user);
            RaiseLocalEvent(aui.OwnerUid, oae, false);
            if (oae.Cancelled) return;

            SetCurrentSingleUser(aui.OwnerUid, actor.PlayerSession, aui);
            aui.UserInterface.Toggle(actor.PlayerSession);
        }

        public void SetCurrentSingleUser(EntityUid uid, IPlayerSession? v, ActivatableUIComponent? aui = null)
        {
            if (!Resolve(uid, ref aui))
                return;
            if (!aui.SingleUser)
                return;
            if (aui.CurrentSingleUser != null)
                aui.CurrentSingleUser.PlayerStatusChanged -= aui.OnPlayerStatusChanged;

            aui.CurrentSingleUser = v;

            if (v != null)
                v.PlayerStatusChanged += aui.OnPlayerStatusChanged;
            RaiseLocalEvent(uid, new ActivatableUIPlayerChangedEvent(), false);
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

    public class ActivatableUIOpenAttemptEvent : CancellableEntityEventArgs
    {
        public IEntity User { get; }
        public ActivatableUIOpenAttemptEvent(IEntity who)
        {
            User = who;
        }
    }

    public class ActivatableUIPlayerChangedEvent : EntityEventArgs
    {
    }
}
