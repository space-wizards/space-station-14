using System.Linq;
using Content.Server.Administration.Managers;
using Content.Shared.ActionBlocker;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

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
            SubscribeLocalEvent<ActivatableUIComponent, HandDeselectedEvent>((uid, aui, _) => CloseAll(uid, aui));
            SubscribeLocalEvent<ActivatableUIComponent, UnequippedHandEvent>((uid, aui, _) => CloseAll(uid, aui));
            // *THIS IS A BLATANT WORKAROUND!* RATIONALE: Microwaves need it
            SubscribeLocalEvent<ActivatableUIComponent, EntParentChangedMessage>(OnParentChanged);
            SubscribeLocalEvent<ActivatableUIComponent, BoundUIClosedEvent>(OnUIClose);
        }

        private void OnActivate(EntityUid uid, ActivatableUIComponent component, ActivateInWorldEvent args)
        {
            if (args.Handled) return;
            if (component.InHandsOnly) return;
            args.Handled = InteractUI(args.User, component);
        }

        private void OnUseInHand(EntityUid uid, ActivatableUIComponent component, UseInHandEvent args)
        {
            if (args.Handled) return;
            args.Handled = InteractUI(args.User, component);
        }

        private void OnParentChanged(EntityUid uid, ActivatableUIComponent aui, ref EntParentChangedMessage args)
        {
            CloseAll(uid, aui);
        }

        private void OnUIClose(EntityUid uid, ActivatableUIComponent component, BoundUIClosedEvent args)
        {
            if (args.Session != component.CurrentSingleUser) return;
            if (args.UiKey != component.Key) return;
            SetCurrentSingleUser(uid, null, component);
        }

        private bool InteractUI(EntityUid user, ActivatableUIComponent aui)
        {
            if (!EntityManager.TryGetComponent(user, out ActorComponent? actor)) return false;

            if (aui.AdminOnly && !_adminManager.IsAdmin(actor.PlayerSession)) return false;

            if (!_actionBlockerSystem.CanInteract(user))
            {
                user.PopupMessageCursor(Loc.GetString("base-computer-ui-component-cannot-interact"));
                return true;
            }

            var ui = aui.UserInterface;
            if (ui == null) return false;

            if (aui.SingleUser && (aui.CurrentSingleUser != null) && (actor.PlayerSession != aui.CurrentSingleUser))
            {
                // If we get here, supposedly, the object is in use.
                // Check with BUI that it's ACTUALLY in use just in case.
                // Since this could brick the object if it goes wrong.
                if (ui.SubscribedSessions.Count != 0) return false;
            }

            // If we've gotten this far, fire a cancellable event that indicates someone is about to activate this.
            // This is so that stuff can require further conditions (like power).
            var oae = new ActivatableUIOpenAttemptEvent(user);
            RaiseLocalEvent((aui).Owner, oae, false);
            if (oae.Cancelled) return false;

            SetCurrentSingleUser((aui).Owner, actor.PlayerSession, aui);
            ui.Toggle(actor.PlayerSession);
            return true;
        }

        public void SetCurrentSingleUser(EntityUid uid, IPlayerSession? v, ActivatableUIComponent? aui = null)
        {
            if (!Resolve(uid, ref aui))
                return;
            if (!aui.SingleUser)
                return;

            aui.CurrentSingleUser = v;

            RaiseLocalEvent(uid, new ActivatableUIPlayerChangedEvent(), false);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var component in EntityManager.EntityQuery<ActivatableUIComponent>(true))
            {
                var ui = component.UserInterface;
                if (ui == null) continue;
                // Done to skip an allocation on anything that's not in use.
                if (ui.SubscribedSessions.Count == 0) continue;
                // Must ToList in order to close things safely.
                foreach (var session in ui.SubscribedSessions.ToArray())
                {
                    if (session.AttachedEntity == null || !_actionBlockerSystem.CanInteract(session.AttachedEntity.Value))
                    {
                        ui.Close(session);
                    }
                }
            }
        }

        public void CloseAll(EntityUid uid, ActivatableUIComponent? aui = null)
        {
            if (!Resolve(uid, ref aui, false)) return;
            aui.UserInterface?.CloseAll();
        }
    }

    public class ActivatableUIOpenAttemptEvent : CancellableEntityEventArgs
    {
        public EntityUid User { get; }
        public ActivatableUIOpenAttemptEvent(EntityUid who)
        {
            User = who;
        }
    }

    public class ActivatableUIPlayerChangedEvent : EntityEventArgs
    {
    }
}
