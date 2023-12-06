using Content.Server.Nutrition.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Server.Nutrition.EntitySystems
{
    public sealed class SliceableFoodSystem : EntitySystem
    {
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SliceableFoodComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<SliceableFoodComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<SliceableFoodComponent, ComponentStartup>(OnComponentStartup);
        }

        private void OnInteractUsing(EntityUid uid, SliceableFoodComponent component, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            if (TrySliceFood(uid, args.User, args.Used, component))
                args.Handled = true;
        }

        private bool TrySliceFood(EntityUid uid, EntityUid user, EntityUid usedItem,
            SliceableFoodComponent? component = null, FoodComponent? food = null, TransformComponent? transform = null)
        {
            if (!Resolve(uid, ref component, ref food, ref transform) ||
                string.IsNullOrEmpty(component.Slice))
            {
                return false;
            }

            if (!_solutionContainerSystem.TryGetSolution(uid, food.Solution, out var solution))
            {
                return false;
            }

            if (!TryComp<UtensilComponent>(usedItem, out var utensil) || (utensil.Types & UtensilType.Knife) == 0)
            {
                return false;
            }

            var sliceUid = Spawn(component.Slice, transform.Coordinates);

            var lostSolution = _solutionContainerSystem.SplitSolution(uid, solution,
                solution.Volume / FixedPoint2.New(component.Count));

            // Fill new slice
            FillSlice(sliceUid, lostSolution);

            var inCont = _containerSystem.IsEntityInContainer(component.Owner);
            if (inCont)
            {
                _handsSystem.PickupOrDrop(user, sliceUid);
            }
            else
            {
                var xform = Transform(sliceUid);
                _containerSystem.AttachParentToContainerOrGrid((sliceUid, xform));
                xform.LocalRotation = 0;
            }

            _audio.PlayPvs(component.Sound, transform.Coordinates, AudioParams.Default.WithVolume(-2));

            // Decrease size of item based on count - Could implement in the future
            // Bug with this currently is the size in a container is not updated
            // if (TryComp(uid, out ItemComponent? itemComp) && TryComp(sliceUid, out ItemComponent? sliceComp))
            // {
            //     itemComp.Size -= sliceComp.Size;
            // }

            component.Count--;

            // If someone makes food proto with 1 slice...
            if (component.Count < 1)
            {
                DeleteFood(uid, user);
                return true;
            }

            // Split last slice
            if (component.Count > 1)
                return true;

            sliceUid = Spawn(component.Slice, transform.Coordinates);

            // Fill last slice with the rest of the solution
            FillSlice(sliceUid, solution);

            if (inCont)
            {
                _handsSystem.PickupOrDrop(user, sliceUid);
            }
            else
            {
                var xform = Transform(sliceUid);
                _containerSystem.AttachParentToContainerOrGrid((sliceUid, xform));
                xform.LocalRotation = 0;
            }

            DeleteFood(uid, user);
            return true;
        }

        private void DeleteFood(EntityUid uid, EntityUid user)
        {
            var ev = new BeforeFullySlicedEvent
            {
                User = user
            };
            RaiseLocalEvent(uid, ev);

            if (!ev.Cancelled)
                Del(uid);
        }

        private void FillSlice(EntityUid sliceUid, Solution solution)
        {
            // Replace all reagents on prototype not just copying poisons (example: slices of eaten pizza should have less nutrition)
            if (TryComp<FoodComponent>(sliceUid, out var sliceFoodComp) &&
                _solutionContainerSystem.TryGetSolution(sliceUid, sliceFoodComp.Solution, out var itsSolution))
            {
                _solutionContainerSystem.RemoveAllSolution(sliceUid, itsSolution);

                var lostSolutionPart = solution.SplitSolution(itsSolution.AvailableVolume);
                _solutionContainerSystem.TryAddSolution(sliceUid, itsSolution, lostSolutionPart);
            }
        }

        private void OnComponentStartup(EntityUid uid, SliceableFoodComponent component, ComponentStartup args)
        {
            component.Count = component.TotalCount;
            var foodComp = EnsureComp<FoodComponent>(uid);

            EnsureComp<SolutionContainerManagerComponent>(uid);
            _solutionContainerSystem.EnsureSolution(uid, foodComp.Solution);
        }

        private void OnExamined(EntityUid uid, SliceableFoodComponent component, ExaminedEvent args)
        {
            args.PushMarkup(Loc.GetString("sliceable-food-component-on-examine-remaining-slices-text", ("remainingCount", component.Count)));
        }
    }
}
