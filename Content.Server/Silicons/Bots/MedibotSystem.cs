using System.Diagnostics.CodeAnalysis;
using Content.Shared.Mobs.Systems;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Chemistry.Components;
using Content.Shared.Popups;
using Content.Shared.ActionBlocker;
using Content.Shared.Interaction;
using Content.Shared.FixedPoint;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Server.NPC.Components;
using Content.Server.Chemistry.EntitySystems;
using Robust.Shared.Physics.Components;
using Robust.Server.GameObjects;

namespace Content.Server.Silicons.Bots
{
    public sealed class MedibotSystem : EntitySystem
    {
        [Dependency] private readonly MobStateSystem _mobs = default!;
        [Dependency] private readonly SolutionContainerSystem _solution = default!;
        [Dependency] private readonly PopupSystem _popups = default!;
        [Dependency] private readonly DoAfterSystem _doAfter = default!;
        [Dependency] private readonly AudioSystem _audioSystem = default!;
        [Dependency] private readonly ActionBlockerSystem _blocker = default!;
        [Dependency] private readonly PhysicsSystem _physics = default!;
        [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MedibotComponent, InteractNoHandEvent>((uid, component, args) => StartInject(uid, args.Target, component));
            SubscribeLocalEvent<MedibotComponent, DoAfterEvent<MedibotInjectData>>(OnDoAfter);
        }
        public bool StartInject(EntityUid uid, EntityUid? target, MedibotComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return false;

            if (target == null || target == uid)
                return false;

            if (_mobs.IsDead(target.Value))
                return false;

            if (HasComp<NPCRecentlyInjectedComponent>(target))
                return false;

            if (!_solution.TryGetInjectableSolution(target.Value, out var injectableSol))
                return false;

            return TryStartInject(uid, component, target.Value, injectableSol);
        }

        private void OnDoAfter(EntityUid uid, MedibotComponent component, DoAfterEvent<MedibotInjectData> args)
        {
            component.IsInjecting = false;

            if (args.Cancelled || args.Handled || args.Args.Target == null)
                return;

            _audioSystem.PlayPvs(component.InjectFinishSound, args.Args.Target.Value);
            _solution.TryAddReagent(args.Args.Target.Value, args.AdditionalData.Solution, args.AdditionalData.Drug, args.AdditionalData.Amount, out var acceptedQuantity);
            _popups.PopupEntity(Loc.GetString("hypospray-component-feel-prick-message"), args.Args.Target.Value, args.Args.Target.Value);
            EnsureComp<NPCRecentlyInjectedComponent>(args.Args.Target.Value);
            args.Handled = true;
        }

        private bool TryStartInject(EntityUid performer, MedibotComponent component, EntityUid target, Solution injectable)
        {
            if (component.IsInjecting)
                return false;

            if (!_blocker.CanInteract(performer, target))
                return false;

            if (!_interactionSystem.InRangeUnobstructed(performer, target, 2f))
                return false;

            // Hold still, please
            if (TryComp<PhysicsComponent>(target, out var physics) && physics.LinearVelocity.Length > 0.00001f)
                return false;

            // Figure out which drug we're going to inject, if any.
            if (!ChooseDrug(target, component, out var drug, out var injectAmount))
            {
                _popups.PopupEntity(Loc.GetString("medibot-cannot-inject"), performer, performer, PopupType.SmallCaution);
                return false;
            }

            component.InjectTarget = target;

            _popups.PopupEntity(Loc.GetString("medibot-inject-receiver", ("bot", performer)), target, target, PopupType.Medium);
            _popups.PopupEntity(Loc.GetString("medibot-inject-actor", ("target", target)), performer, performer, PopupType.Medium);

            var data = new MedibotInjectData(injectable, drug, injectAmount);
            var args = new DoAfterEventArgs(performer, component.InjectDelay, target: target)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnStun = true,
                NeedHand = false
            };

            component.IsInjecting = true;
            _doAfter.DoAfter(args, data);

            return true;
        }

        /// <summary>
        /// Chooses which drug to inject based on info about the target.
        /// With a small rewrite shouldn't be hard to make different kinds of medibots.
        /// </summary>
        private bool ChooseDrug(EntityUid target, MedibotComponent component, [NotNullWhen(true)] out string? drug, out float injectAmount, DamageableComponent? damage = null)
        {
            drug = null;
            injectAmount = 0;
            if (!Resolve(target, ref damage))
                return false;

            if (damage.TotalDamage == 0)
                return false;

            if (_mobs.IsCritical(target))
            {
                drug = component.EmergencyMed;
                injectAmount = component.EmergencyMedInjectAmount;
                return true;
            }

            if (damage.TotalDamage <= 50)
            {
                drug = component.StandardMed;
                injectAmount = component.StandardMedInjectAmount;
                return true;
            }

            return false;
        }

        private record struct MedibotInjectData(Solution Solution, string Drug, FixedPoint2 Amount)
        {
            public Solution Solution = Solution;
            public string Drug = Drug;
            public FixedPoint2 Amount = Amount;
        }
    }
}
