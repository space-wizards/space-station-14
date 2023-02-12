using Content.Server.Body.Components;
using Content.Server.DoAfter;
using Content.Server.Medical.Components;
using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.Clothing.Components;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory.Events;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using System.Threading;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;

namespace Content.Server.Medical
{
    public sealed class StethoscopeSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly MobStateSystem _mobStateSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<StethoscopeComponent, GotEquippedEvent>(OnEquipped);
            SubscribeLocalEvent<StethoscopeComponent, GotUnequippedEvent>(OnUnequipped);
            SubscribeLocalEvent<WearingStethoscopeComponent, GetVerbsEvent<InnateVerb>>(AddStethoscopeVerb);
            SubscribeLocalEvent<StethoscopeComponent, GetItemActionsEvent>(OnGetActions);
            SubscribeLocalEvent<StethoscopeComponent, StethoscopeActionEvent>(OnStethoscopeAction);
            SubscribeLocalEvent<ListenSuccessfulEvent>(OnListenSuccess);
            SubscribeLocalEvent<ListenCancelledEvent>(OnListenCancelled);
        }

        /// <summary>
        /// Add the component the verb event subs to if the equippee is wearing the stethoscope.
        /// </summary>
        private void OnEquipped(EntityUid uid, StethoscopeComponent component, GotEquippedEvent args)
        {
            if (!TryComp<ClothingComponent>(uid, out var clothing))
                return;
            // Is the clothing in its actual slot?
            if (!clothing.Slots.HasFlag(args.SlotFlags))
                return;

            component.IsActive = true;

            var wearingComp = EnsureComp<WearingStethoscopeComponent>(args.Equipee);
            wearingComp.Stethoscope = uid;
        }

        private void OnUnequipped(EntityUid uid, StethoscopeComponent component, GotUnequippedEvent args)
        {
            if (!component.IsActive)
                return;

            RemComp<WearingStethoscopeComponent>(args.Equipee);
            component.IsActive = false;
        }

        /// <summary>
        /// This is raised when someone with WearingStethoscopeComponent requests verbs on an item.
        /// It returns if the target is not a mob.
        /// </summary>
        private void AddStethoscopeVerb(EntityUid uid, WearingStethoscopeComponent component, GetVerbsEvent<InnateVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess)
                return;

            if (!HasComp<MobStateComponent>(args.Target))
                return;

            if (component.CancelToken != null)
                return;

            if (!TryComp<StethoscopeComponent>(component.Stethoscope, out var stetho))
                return;

            InnateVerb verb = new()
            {
                Act = () =>
                {
                    StartListening(uid, args.Target, stetho); // start doafter
                },
                Text = Loc.GetString("stethoscope-verb"),
                IconTexture = "Clothing/Neck/Misc/stethoscope.rsi/icon.png",
                Priority = 2
            };
            args.Verbs.Add(verb);
        }


        private void OnStethoscopeAction(EntityUid uid, StethoscopeComponent component, StethoscopeActionEvent args)
        {
            StartListening(args.Performer, args.Target, component);
        }

        private void OnGetActions(EntityUid uid, StethoscopeComponent component, GetItemActionsEvent args)
        {
            args.Actions.Add(component.Action);
        }

        // doafter succeeded / failed
        private void OnListenSuccess(ListenSuccessfulEvent ev)
        {
            ev.Component.CancelToken = null;
            ExamineWithStethoscope(ev.User, ev.Target);
        }

        private void OnListenCancelled(ListenCancelledEvent ev)
        {
            if (ev.Component == null)
                return;
            ev.Component.CancelToken = null;
        }
        // construct the doafter and start it
        private void StartListening(EntityUid user, EntityUid target, StethoscopeComponent comp)
        {
            comp.CancelToken = new CancellationTokenSource();
            _doAfterSystem.DoAfter(new DoAfterEventArgs(user, comp.Delay, comp.CancelToken.Token, target: target)
            {
                BroadcastFinishedEvent = new ListenSuccessfulEvent(user, target, comp),
                BroadcastCancelledEvent = new ListenCancelledEvent(user, comp),
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnStun = true,
                NeedHand = true
            });
        }

        /// <summary>
        /// Return a value based on the total oxyloss of the target.
        /// Could be expanded in the future with reagent effects etc.
        /// The loc lines are taken from the goon wiki.
        /// </summary>
        public void ExamineWithStethoscope(EntityUid user, EntityUid target)
        {
            // The mob check seems a bit redundant but (1) they could conceivably have lost it since when the doafter started and (2) I need it for .IsDead()
            if (!HasComp<RespiratorComponent>(target) || !TryComp<MobStateComponent>(target, out var mobState) || _mobStateSystem.IsDead(target, mobState))
            {
                _popupSystem.PopupEntity(Loc.GetString("stethoscope-dead"), target, user);
                return;
            }

            if (!TryComp<DamageableComponent>(target, out var damage))
                return;
            // these should probably get loc'd at some point before a non-english fork accidentally breaks a bunch of stuff that does this
            if (!damage.Damage.DamageDict.TryGetValue("Asphyxiation", out var value))
                return;

            var message = GetDamageMessage(value);

            _popupSystem.PopupEntity(Loc.GetString(message), target, user);
        }

        private string GetDamageMessage(FixedPoint2 totalOxyloss)
        {
            var msg = (int) totalOxyloss switch
            {
                < 20 => "stethoscope-normal",
                < 60 => "stethoscope-hyper",
                < 80 => "stethoscope-irregular",
                _ => "stethoscope-fucked"
            };
            return msg;
        }

        // events for the doafter
        private sealed class ListenSuccessfulEvent : EntityEventArgs
        {
            public EntityUid User;
            public EntityUid Target;
            public StethoscopeComponent Component;

            public ListenSuccessfulEvent(EntityUid user, EntityUid target, StethoscopeComponent component)
            {
                User = user;
                Target = target;
                Component = component;
            }
        }

        private sealed class ListenCancelledEvent : EntityEventArgs
        {
            public EntityUid Uid;
            public StethoscopeComponent Component;

            public ListenCancelledEvent(EntityUid uid, StethoscopeComponent component)
            {
                Uid = uid;
                Component = component;
            }
        }

    }

    public sealed class StethoscopeActionEvent : EntityTargetActionEvent {}
}
