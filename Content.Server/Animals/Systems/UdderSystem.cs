using System;
using Content.Server.Animals.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Nutrition.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Interaction;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.DoAfter;
using Content.Shared.Popups;
using Robust.Shared.Localization;

namespace Content.Server.Animals.Systems
{
    /// <summary>
    ///     Gives ability to living beings with acceptable hunger level to produce milkable reagents.
    /// </summary>
    internal class UdderSystem : EntitySystem
    {
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<UdderComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<UdderComponent, MilkingFinishedEvent>(OnMilkingFinished);
            SubscribeLocalEvent<UdderComponent, MilkingFailEvent>(OnMilkingFailed);
        }

        public override void Update(float frameTime)
        {
            foreach (var udder in EntityManager.EntityQuery<UdderComponent>(false))
            {
                udder.AccumulatedFrameTime += frameTime;

                if (udder.AccumulatedFrameTime < udder.UpdateRate)
                    continue;

                // Actually there is food digestion so no problem with instant reagent generation "OnFeed"
                if (udder.Owner.TryGetComponent<HungerComponent>(out var hunger))
                {
                    hunger.HungerThresholds.TryGetValue(HungerThreshold.Peckish, out var targetThreshold);

                    // Is there enough nutrition to produce reagent?
                    if (hunger.CurrentHunger < targetThreshold)
                        continue;
                }

                if (!_solutionContainerSystem.TryGetSolution(udder.OwnerUid, udder.TargetSolutionName, out var solution))
                    continue;

                //TODO: toxins from bloodstream !?
                _solutionContainerSystem.TryAddReagent(udder.OwnerUid, solution, udder.ReagentId, udder.QuantityPerUpdate, out var accepted);
                udder.AccumulatedFrameTime = 0;
            }
        }

        private void OnInteractUsing(EntityUid uid, UdderComponent component, InteractUsingEvent args)
        {
            // Milking available with empty refillable containers only
            if (!args.Used.TryGetComponent<RefillableSolutionComponent>(out var refillable) ||
                !_solutionContainerSystem.TryGetSolution(args.UsedUid, refillable.Solution, out var solution))
                return;

            if (solution.TotalVolume > 0)
                return;

            AttemtMilk(uid, args.UserUid, args.UsedUid, component);
            args.Handled = true;
        }

        private void AttemtMilk(EntityUid uid, EntityUid userUid, EntityUid containerUid, UdderComponent udder)
        {
            var user = EntityManager.GetEntity(userUid);
            if (udder.BeingMilked)
            {
                udder.Owner.PopupMessage(user, Loc.GetString("udder-system-already-milking"));
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

            var user = EntityManager.GetEntity(ev.UserUid);

            var quantity = solution.TotalVolume;
            if(quantity == 0)
            {
                udder.Owner.PopupMessage(user, Loc.GetString("udder-system-dry"));
                return;
            }

            if (quantity > targetSolution.AvailableVolume)
                quantity = targetSolution.AvailableVolume;

            var split = _solutionContainerSystem.SplitSolution(uid, solution, quantity);
            _solutionContainerSystem.TryAddSolution(ev.ContainerUid, targetSolution, split);

            var container = EntityManager.GetEntity(ev.ContainerUid);
            udder.Owner.PopupMessage(user, Loc.GetString("udder-system-success", ("amount", quantity), ("target", container)));
        }

        private void OnMilkingFailed(EntityUid uid, UdderComponent component, MilkingFailEvent ev)
        {
            //TODO: fail PopupMessage?
            component.BeingMilked = false;
        }

        private class MilkingFinishedEvent : EntityEventArgs
        {
            public EntityUid UserUid;
            public EntityUid ContainerUid;

            public MilkingFinishedEvent(EntityUid userUid, EntityUid containerUid)
            {
                UserUid = userUid;
                ContainerUid = containerUid;
            }
        }

        private class MilkingFailEvent : EntityEventArgs
        { }
    }
}
