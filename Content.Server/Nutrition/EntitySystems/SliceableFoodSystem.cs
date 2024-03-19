using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Nutrition.Components;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Server.Nutrition.EntitySystems
{
    public sealed class SliceableFoodSystem : EntitySystem
    {
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
        [Dependency] private readonly TransformSystem _xformSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SliceableFoodComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<SliceableFoodComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<SliceableFoodComponent, ComponentStartup>(OnComponentStartup);
        }

        private void OnInteractUsing(Entity<SliceableFoodComponent> entity, ref InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            if (TrySliceFood(entity, args.User, args.Used, entity.Comp))
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

            if (!_solutionContainerSystem.TryGetSolution(uid, food.Solution, out var soln, out var solution))
            {
                return false;
            }

            if (!TryComp<UtensilComponent>(usedItem, out var utensil) || (utensil.Types & UtensilType.Knife) == 0)
            {
                return false;
            }

            var sliceUid = Slice(uid, user, component, transform);

            var lostSolution = _solutionContainerSystem.SplitSolution(soln.Value, solution.Volume / FixedPoint2.New(component.Count));

            // Fill new slice
            FillSlice(sliceUid, lostSolution);

            _audio.PlayPvs(component.Sound, transform.Coordinates, AudioParams.Default.WithVolume(-2));
            var ev = new SliceFoodEvent();
            RaiseLocalEvent(uid, ref ev);

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
                DeleteFood(uid, user, food);
                return true;
            }

            // Split last slice
            if (component.Count > 1)
                return true;

            sliceUid = Slice(uid, user, component, transform);

            // Fill last slice with the rest of the solution
            FillSlice(sliceUid, solution);

            DeleteFood(uid, user, food);
            return true;
        }

        /// <summary>
        /// Create a new slice in the world and returns its entity.
        /// The solutions must be set afterwards.
        /// </summary>
        public EntityUid Slice(EntityUid uid, EntityUid user, SliceableFoodComponent? comp = null, TransformComponent? transform = null)
        {
            if (!Resolve(uid, ref comp, ref transform))
                return EntityUid.Invalid;

            var sliceUid = Spawn(comp.Slice, _xformSystem.GetMapCoordinates(uid));

            // try putting the slice into the container if the food being sliced is in a container!
            // this lets you do things like slice a pizza up inside of a hot food cart without making a food-everywhere mess
            if (_containerSystem.TryGetContainingContainer(uid, out var container) && _containerSystem.CanInsert(sliceUid, container))
            {
                _containerSystem.Insert(sliceUid, container);
            }
            else // puts it down "right-side up"
            {
                _xformSystem.AttachToGridOrMap(sliceUid);
                _xformSystem.SetLocalRotation(sliceUid, 0);
            }

            return sliceUid;
        }

        private void DeleteFood(EntityUid uid, EntityUid user, FoodComponent foodComp)
        {
            var ev = new BeforeFullySlicedEvent
            {
                User = user
            };
            RaiseLocalEvent(uid, ev);
            if (ev.Cancelled)
                return;

            if (string.IsNullOrEmpty(foodComp.Trash))
            {
                QueueDel(uid);
                return;
            }

            // Locate the sliced food and spawn its trash
            var trashUid = Spawn(foodComp.Trash, _xformSystem.GetMapCoordinates(uid));

            // try putting the trash in the food's container too, to be consistent with slice spawning?
            if (_containerSystem.TryGetContainingContainer(uid, out var container) && _containerSystem.CanInsert(trashUid, container))
            {
                _containerSystem.Insert(trashUid, container);
            }
            else // puts it down "right-side up"
            {
                _xformSystem.AttachToGridOrMap(trashUid);
                _xformSystem.SetLocalRotation(trashUid, 0);
            }

            QueueDel(uid);
        }

        private void FillSlice(EntityUid sliceUid, Solution solution)
        {
            // Replace all reagents on prototype not just copying poisons (example: slices of eaten pizza should have less nutrition)
            if (TryComp<FoodComponent>(sliceUid, out var sliceFoodComp) &&
                _solutionContainerSystem.TryGetSolution(sliceUid, sliceFoodComp.Solution, out var itsSoln, out var itsSolution))
            {
                _solutionContainerSystem.RemoveAllSolution(itsSoln.Value);

                var lostSolutionPart = solution.SplitSolution(itsSolution.AvailableVolume);
                _solutionContainerSystem.TryAddSolution(itsSoln.Value, lostSolutionPart);
            }
        }

        private void OnComponentStartup(Entity<SliceableFoodComponent> entity, ref ComponentStartup args)
        {
            entity.Comp.Count = entity.Comp.TotalCount;

            var foodComp = EnsureComp<FoodComponent>(entity);
            _solutionContainerSystem.EnsureSolution(entity.Owner, foodComp.Solution);
        }

        private void OnExamined(Entity<SliceableFoodComponent> entity, ref ExaminedEvent args)
        {
            args.PushMarkup(Loc.GetString("sliceable-food-component-on-examine-remaining-slices-text", ("remainingCount", entity.Comp.Count)));
        }
    }
}
