using System.Linq;
using Content.Server.Administration.Managers;
using Content.Server.Ghost.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;
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

            SubscribeLocalEvent<ActivatableUIComponent, GetVerbsEvent<ActivationVerb>>(AddOpenUiVerb);
        }

        private void AddOpenUiVerb(EntityUid uid, ActivatableUIComponent component, GetVerbsEvent<ActivationVerb> args)
        {
            if (!args.CanAccess)
                return;

            if (!args.CanInteract && !HasComp<GhostComponent>(args.User))
                return;

            ActivationVerb verb = new();
            verb.Act = () => InteractUI(args.User, component);
            verb.Text = Loc.GetString("ui-verb-toggle-open");
            // TODO VERBS add "open UI" icon?
            args.Verbs.Add(verb);
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
