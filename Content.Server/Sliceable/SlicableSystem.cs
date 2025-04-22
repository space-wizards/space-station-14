using Content.Server.Nutrition.Components;

using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Sliceable;

using Robust.Server.GameObjects;

using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Server.Sliceable;

public sealed class SliceableSystem : SharedSliceableSystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SliceableComponent, SliceFoodEvent>(OnSliceEvent);
    }

    private void OnSliceEvent(EntityUid uid, SliceableComponent comp, ref SliceFoodEvent args)
    {
        TrySlice(uid);
    }

    private bool TrySlice(EntityUid uid,
        SliceableComponent? comp = null,
        FoodComponent? food = null,
        TransformComponent? transform = null)
    {
        if (!Resolve(uid, ref comp, ref food, ref transform) ||
            string.IsNullOrEmpty(comp.Slice))
            return false;

        if (!_solutionContainer.TryGetSolution(uid, food.Solution, out var soln, out var solution))
            return false;

        var sliceVolume = solution.Volume / FixedPoint2.New(comp.Count);
        for (var i = 0; i < comp.Count; i++)
        {
            var sliceUid = Slice(uid, comp, transform);

            // Fills new slice if comp allows
            if (comp.TransferSolution)
            {
                var lostSolution = _solutionContainer.SplitSolution(soln.Value, sliceVolume);
                FillSlice(sliceUid, lostSolution);
            }
        }

        _audio.PlayPvs(comp.Sound, transform.Coordinates, AudioParams.Default.WithVolume(-2));

        return true;
    }

    private void FillSlice(EntityUid sliceUid, Solution solution)
    {
        // Replace all reagents on prototype not just copying poisons (example: slices of eaten pizza should have less nutrition)
        if (TryComp<FoodComponent>(sliceUid, out var sliceFoodComp) &&
            _solutionContainer.TryGetSolution(sliceUid, sliceFoodComp.Solution, out var itsSoln, out var itsSolution))
        {
            _solutionContainer.RemoveAllSolution(itsSoln.Value);

            var lostSolutionPart = solution.SplitSolution(itsSolution.AvailableVolume);
            _solutionContainer.TryAddSolution(itsSoln.Value, lostSolutionPart);
        }
    }

    public EntityUid Slice(EntityUid uid,
        SliceableComponent? comp = null,
        TransformComponent? transform = null)
    {
        if (!Resolve(uid, ref comp, ref transform))
            return EntityUid.Invalid;

        var sliceUid = Spawn(comp.Slice, _transform.GetMapCoordinates(uid));

        // try putting the slice into the container if the food being sliced is in a container!
        // this lets you do things like slice a pizza up inside of a hot food cart without making a food-everywhere mess
        _transform.DropNextTo(sliceUid, (uid, transform));
        _transform.SetLocalRotation(sliceUid, 0);

        if (!_container.IsEntityOrParentInContainer(sliceUid))
        {
            var randVect = _random.NextVector2(2.0f, 2.5f);
            if (TryComp<PhysicsComponent>(sliceUid, out var physics))
                _physics.SetLinearVelocity(sliceUid, randVect, body: physics);
        }

        return sliceUid;
    }
}
