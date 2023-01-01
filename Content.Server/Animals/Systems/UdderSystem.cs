using Content.Server.Animals.Components;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.DoAfter;
using Content.Server.Nutrition.Components;
using Content.Server.Popups;
using Content.Shared.IdentityManagement;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Player;

namespace Content.Server.Animals.Systems
{
    /// <summary>
    ///     Gives ability to living beings with acceptable hunger level to produce milkable reagents.
    /// </summary>
    internal sealed class UdderSystem : EntitySystem
    {
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<UdderComponent, GetVerbsEvent<AlternativeVerb>>(AddMilkVerb);
            SubscribeLocalEvent<UdderComponent, MilkingFinishedEvent>(OnMilkingFinished);
            SubscribeLocalEvent<UdderComponent, MilkingFailEvent>(OnMilkingFailed);
        }

        public override void Update(float frameTime)
        {
            foreach (var udder in EntityManager.EntityQuery<UdderComponent>(false))
            {
                udder.AccumulatedFrameTime += frameTime;

                while (udder.AccumulatedFrameTime > udder.UpdateRate)
                {
                    udder.AccumulatedFrameTime -= udder.UpdateRate;

                    // Actually there is food digestion so no problem with instant reagent generation "OnFeed"
                    if (EntityManager.TryGetComponent<HungerComponent?>(udder.Owner, out var hunger))
                    {
                        hunger.HungerThresholds.TryGetValue(HungerThreshold.Peckish, out var targetThreshold);

                        // Is there enough nutrition to produce reagent?
                        if (hunger.CurrentHunger < targetThreshold)
                            continue;
                    }

                    if (!_solutionContainerSystem.TryGetSolution(udder.Owner, udder.TargetSolutionName,
                            out var solution))
                        continue;

                    //TODO: toxins from bloodstream !?
                    _solutionContainerSystem.TryAddReagent(udder.Owner, solution, udder.ReagentId,
                        udder.QuantityPerUpdate, out var accepted);
                }
            }
        }

        private void AttemptMilk(EntityUid uid, EntityUid userUid, EntityUid containerUid, UdderComponent? udder = null)
        {
            if (!Resolve(uid, ref udder))
                return;

            if (udder.BeingMilked)
            {
                _popupSystem.PopupEntity(Loc.GetString("udder-system-already-milking"), uid, userUid);
                return;
            }

            udder.BeingMilked = true;

            var doargs = new DoAfterEventArgs(userUid, 5, default, uid)
            {
                BreakOnUserMove = true,
                BreakOnDamage = true,
                BreakOnStun = true,
                BreakOnTargetMove = true,
                MovementThreshold = 1.0f,
                TargetFinishedEvent = new MilkingFinishedEvent(userUid, containerUid),
                TargetCancelledEvent = new MilkingFailEvent()
            };

            _doAfterSystem.DoAfter(doargs);
        }

        private void OnMilkingFinished(EntityUid uid, UdderComponent udder, MilkingFinishedEvent ev)
        {
            udder.BeingMilked = false;

            if (!_solutionContainerSystem.TryGetSolution(uid, udder.TargetSolutionName, out var solution))
                return;

            if (!_solutionContainerSystem.TryGetRefillableSolution(ev.ContainerUid, out var targetSolution))
                return;

            var quantity = solution.TotalVolume;
            if(quantity == 0)
            {
                _popupSystem.PopupEntity(Loc.GetString("udder-system-dry"), uid, ev.UserUid);
                return;
            }

            if (quantity > targetSolution.AvailableVolume)
                quantity = targetSolution.AvailableVolume;

            var split = _solutionContainerSystem.SplitSolution(uid, solution, quantity);
            _solutionContainerSystem.TryAddSolution(ev.ContainerUid, targetSolution, split);

            _popupSystem.PopupEntity(Loc.GetString("udder-system-success", ("amount", quantity), ("target", Identity.Entity(ev.ContainerUid, EntityManager))), uid,
                ev.UserUid, PopupType.Medium);
        }

        private void OnMilkingFailed(EntityUid uid, UdderComponent component, MilkingFailEvent ev)
        {
            component.BeingMilked = false;
        }

        private void AddMilkVerb(EntityUid uid, UdderComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (args.Using == null ||
                 !args.CanInteract ||
                 !EntityManager.HasComponent<RefillableSolutionComponent>(args.Using.Value))
                return;

            AlternativeVerb verb = new()
            {
                Act = () =>
                {
                    AttemptMilk(uid, args.User, args.Using.Value, component);
                },
                Text = Loc.GetString("udder-system-verb-milk"),
                Priority = 2
            };
            args.Verbs.Add(verb);
        }

        private sealed class MilkingFinishedEvent : EntityEventArgs
        {
            public EntityUid UserUid;
            public EntityUid ContainerUid;

            public MilkingFinishedEvent(EntityUid userUid, EntityUid containerUid)
            {
                UserUid = userUid;
                ContainerUid = containerUid;
            }
        }

        private sealed class MilkingFailEvent : EntityEventArgs
        { }
    }
}
