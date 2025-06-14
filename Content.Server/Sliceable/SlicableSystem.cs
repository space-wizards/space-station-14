using Content.Server.Nutrition.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Sliceable;
using Content.Shared.Storage;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Server.Sliceable;

public sealed class SliceableSystem : SharedSliceableSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SliceableComponent, SliceEvent>(OnSliceEvent);
    }

    private void OnSliceEvent(EntityUid uid, SliceableComponent comp, ref SliceEvent args)
    {
        TrySlice(uid);
    }

    private bool TrySlice(EntityUid uid,
        SliceableComponent? comp = null,
        FoodComponent? food = null,
        TransformComponent? transform = null)
    {
        if (!Resolve(uid, ref comp, ref transform))
            return false;

        var slices = EntitySpawnCollection.GetSpawns(comp.Slices);

        foreach (var sliceProto in slices)
        {
            var sliceUid = Spawn(sliceProto);

            _transform.DropNextTo(sliceUid, (uid, transform));
            _transform.SetLocalRotation(sliceUid, 0);

            if (!_container.IsEntityOrParentInContainer(sliceUid))
            {
                var randVect = _random.NextVector2(2.0f, 2.5f);
                _physics.SetLinearVelocity(sliceUid, randVect);
            }

            // Fills new slice if comp allows.
            if (Resolve(uid, ref food) && comp.TransferSolution)
            {
                if (!_solutionContainer.TryGetSolution(uid, food.Solution, out var soln, out var solution))
                    return false;

                var sliceVolume = solution.Volume / FixedPoint2.New(slices.Count);

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
}
