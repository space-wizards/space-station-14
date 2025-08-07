using Content.Server.DoAfter;
using Content.Server.Nutrition.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Content.Shared.Destructible;

namespace Content.Server.Nutrition.EntitySystems;

public sealed class SliceableFoodSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDestructibleSystem _destroy = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SliceableFoodComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<SliceableFoodComponent, SliceFoodDoAfterEvent>(OnSlicedoAfter);
        SubscribeLocalEvent<SliceableFoodComponent, ComponentStartup>(OnComponentStartup);
    }

    private void OnInteractUsing(Entity<SliceableFoodComponent> entity, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<UtensilComponent>(args.Used, out var utensil) || (utensil.Types & UtensilType.Knife) == 0)
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager,
            args.User,
            entity.Comp.SliceTime,
            new SliceFoodDoAfterEvent(),
            entity,
            entity,
            args.Used)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
        };
        args.Handled = _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnSlicedoAfter(Entity<SliceableFoodComponent> entity, ref SliceFoodDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        if (TrySliceFood(entity.Owner, args.User, args.Used))
            args.Handled = true;
    }

    private bool TrySliceFood(Entity<TransformComponent?, SliceableFoodComponent?, EdibleComponent?> entity,
        EntityUid user,
        EntityUid? usedItem)
    {
        if (!Resolve(entity, ref entity.Comp1, ref entity.Comp2, ref entity.Comp3) || string.IsNullOrEmpty(entity.Comp2.Slice))
            return false;

        if (!_solutionContainer.TryGetSolution(entity.Owner, entity.Comp3.Solution, out var soln, out var solution))
            return false;

        if (!TryComp<UtensilComponent>(usedItem, out var utensil) || (utensil.Types & UtensilType.Knife) == 0)
            return false;

        var sliceVolume = solution.Volume / FixedPoint2.New(entity.Comp2.TotalCount);
        for (int i = 0; i < entity.Comp2.TotalCount; i++)
        {
            var sliceUid = Slice(entity, user);

            var lostSolution =
                _solutionContainer.SplitSolution(soln.Value, sliceVolume);

            // Fill new slice
            FillSlice(sliceUid, lostSolution);
        }

        _audio.PlayPvs(entity.Comp2.Sound, entity.Comp1.Coordinates, AudioParams.Default.WithVolume(-2));
        var ev = new SliceFoodEvent();
        RaiseLocalEvent(entity, ref ev);

        DeleteFood(entity, user);
        return true;
    }

    /// <summary>
    /// Create a new slice in the world and returns its entity.
    /// The solutions must be set afterwards.
    /// </summary>
    public EntityUid Slice(Entity<TransformComponent?, SliceableFoodComponent?> entity, EntityUid user)
    {
        if (!Resolve(entity, ref entity.Comp1, ref entity.Comp2))
            return EntityUid.Invalid;

        var sliceUid = Spawn(entity.Comp2.Slice, _transform.GetMapCoordinates((entity, entity.Comp1)));

        // try putting the slice into the container if the food being sliced is in a container!
        // this lets you do things like slice a pizza up inside of a hot food cart without making a food-everywhere mess
        _transform.DropNextTo(sliceUid, entity);
        _transform.SetLocalRotation(sliceUid, 0);

        if (!_container.IsEntityOrParentInContainer(sliceUid))
        {
            var randVect = _random.NextVector2(2.0f, 2.5f);
            if (TryComp<PhysicsComponent>(sliceUid, out var physics))
                _physics.SetLinearVelocity(sliceUid, randVect, body: physics);
        }

        return sliceUid;
    }

    private void DeleteFood(EntityUid uid, EntityUid user)
    {
        var ev = new BeforeFullySlicedEvent
        {
            User = user
        };
        RaiseLocalEvent(uid, ev);
        if (ev.Cancelled)
            return;

        _destroy.DestroyEntity(uid);
    }

    private void FillSlice(Entity<EdibleComponent?> slice, Solution solution)
    {
        if (!Resolve(slice, ref slice.Comp, false))
            return;

        // Replace all reagents on prototype not just copying poisons (example: slices of eaten pizza should have less nutrition)
        if (!_solutionContainer.TryGetSolution(slice.Owner, slice.Comp.Solution, out var itsSoln, out var itsSolution))
            return;

        _solutionContainer.RemoveAllSolution(itsSoln.Value);

        var lostSolutionPart = solution.SplitSolution(itsSolution.AvailableVolume);
        _solutionContainer.TryAddSolution(itsSoln.Value, lostSolutionPart);
    }

    private void OnComponentStartup(Entity<SliceableFoodComponent> entity, ref ComponentStartup args)
    {
        // TODO: When Food Component is fully kill delete this awful method
        // This exists just to make tests fail I guess, awesome!
        // If you're here because your test just failed, make sure that:
        // Your food has the edible component
        // The solution listed in the edible component exists
        var foodComp = EnsureComp<EdibleComponent>(entity);
        _solutionContainer.EnsureSolution(entity.Owner, foodComp.Solution, out _);
    }
}

