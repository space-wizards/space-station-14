using Content.Server.Body.Components;
using Content.Server.Medical.Components;
using Content.Server.Medical.Stethoscope.Components;
using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.Clothing;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Medical;
using Content.Shared.Medical.Stethoscope;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Server.Medical.Stethoscope
{
    public sealed class StethoscopeSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly MobStateSystem _mobStateSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<StethoscopeComponent, ClothingGotEquippedEvent>(OnEquipped);
            SubscribeLocalEvent<StethoscopeComponent, ClothingGotUnequippedEvent>(OnUnequipped);
            SubscribeLocalEvent<WearingStethoscopeComponent, GetVerbsEvent<InnateVerb>>(AddStethoscopeVerb);
            SubscribeLocalEvent<StethoscopeComponent, GetItemActionsEvent>(OnGetActions);
            SubscribeLocalEvent<StethoscopeComponent, StethoscopeActionEvent>(OnStethoscopeAction);
            SubscribeLocalEvent<StethoscopeComponent, StethoscopeDoAfterEvent>(OnDoAfter);
        }

        /// <summary>
        /// Add the component the verb event subs to if the equippee is wearing the stethoscope.
        /// </summary>
        private void OnEquipped(EntityUid uid, StethoscopeComponent component, ref ClothingGotEquippedEvent args)
        {
            component.IsActive = true;

            var wearingComp = EnsureComp<WearingStethoscopeComponent>(args.Wearer);
            wearingComp.Stethoscope = uid;
        }

        private void OnUnequipped(EntityUid uid, StethoscopeComponent component, ref ClothingGotUnequippedEvent args)
        {
            if (!component.IsActive)
                return;

            RemComp<WearingStethoscopeComponent>(args.Wearer);
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
                    StartListening(component.Stethoscope, uid, args.Target, stetho); // start doafter
                },
                Text = Loc.GetString("stethoscope-verb"),
                Icon = new SpriteSpecifier.Rsi(new ("Clothing/Neck/Misc/stethoscope.rsi"), "icon"),
                Priority = 2
            };
            args.Verbs.Add(verb);
        }


        private void OnStethoscopeAction(EntityUid uid, StethoscopeComponent component, StethoscopeActionEvent args)
        {
            StartListening(uid, args.Performer, args.Target, component);
        }

        private void OnGetActions(EntityUid uid, StethoscopeComponent component, GetItemActionsEvent args)
        {
            args.AddAction(ref component.ActionEntity, component.Action);
        }

        // construct the doafter and start it
        private void StartListening(EntityUid scope, EntityUid user, EntityUid target, StethoscopeComponent comp)
        {
            _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, user, comp.Delay, new StethoscopeDoAfterEvent(), scope, target: target, used: scope)
            {
                NeedHand = true,
                BreakOnMove = true,
            });
        }

        private void OnDoAfter(EntityUid uid, StethoscopeComponent component, DoAfterEvent args)
        {
            if (args.Handled || args.Cancelled || args.Args.Target == null)
                return;

            ExamineWithStethoscope(args.Args.User, args.Args.Target.Value);
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
    }
}
