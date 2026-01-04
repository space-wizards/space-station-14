using Content.Server.DoAfter;
using Content.Server.Botany.Components;
using Content.Server.Nutrition.Components;
using Content.Server.Stack;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Destructible;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Stacks;
using Content.Shared.Kitchen.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;


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
    [Dependency] private readonly StackSystem _stack = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SliceableFoodComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<SliceableFoodComponent, SliceFoodDoAfterEvent>(OnSlicedoAfter);
    }

    private void OnInteractUsing(Entity<SliceableFoodComponent> entity, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<UtensilComponent>(args.Used, out var utensil) || (utensil.Types & UtensilType.Knife) == 0)
        {
            if (entity.Comp.AnySharp == false || entity.Comp.AnySharp == true && !HasComp<SharpComponent>(args.Used))
                return;
        }

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

    private bool TrySliceFood(Entity<TransformComponent?, SliceableFoodComponent?, SolutionContainerManagerComponent?> entity,
        EntityUid user,
        EntityUid? usedItem)
    {
        if (!Resolve(entity, ref entity.Comp1, ref entity.Comp2, ref entity.Comp3) || string.IsNullOrEmpty(entity.Comp2.Slice))
            return false;

        var count = entity.Comp2.TotalCount; //avoid changing TotalCount directly because theoretically deleting the food can be cancelled
        if (entity.Comp2.PotencyEffectsCount == true)
        {
            if (TryComp<ProduceComponent>(entity, out var prod))
            {
                if (prod.Seed != null) //Is seed data defined? Wouldn't be for produce not coming from a plant.
                    count = (ushort)Math.Ceiling(entity.Comp2.TotalCount * Math.Min(prod.Seed.Potency, 100f) / 100);
            }
        }

        var slices = Slice(entity, count);
        if (!HasComp<StackComponent>(slices[0]) && !slices.Contains(EntityUid.Invalid)) //stackable entities don't handle inconsistent reagent makeups well
        {
            TryComp<EdibleComponent>(entity, out var edible);
            foreach (var container in entity.Comp3.Containers) //for each solution container in the entity
            {
                if (!_solutionContainer.TryGetSolution(entity.Owner, container, out var soln, out var solution)) //check if there's a solution to get
                    continue;

                var sliceVolume = solution.Volume / FixedPoint2.New(count);

                foreach (var sliceUid in slices) //for each slice
                {
                    TryComp<EdibleComponent>(sliceUid, out var edibleSlice);
                    var lostSolution =
                        _solutionContainer.SplitSolution(soln.Value, sliceVolume);

                    //if both sliced and slice entities have ediblecomponent, and current container is the sliced's edible solution container, make sure it ends up in the slice's edible solution container
                    if (edible != null && edibleSlice != null & edible.Solution == container)
                        FillSlice(sliceUid, lostSolution, edibleSlice!.Solution); //fill specifically the EdibleComponent-linked solution container
                    else
                        FillSlice(sliceUid, lostSolution, lostSolution.Name!); //fill a solution container of the same name (if it exists)
                }
            }
        }

        _audio.PlayPvs(entity.Comp2.Sound, entity.Comp1.Coordinates, AudioParams.Default.WithVolume(-2));
        var ev = new SliceFoodEvent();
        RaiseLocalEvent(entity, ref ev);

        DeleteFood(entity, user);
        return true;
    }

    /// <summary>
    /// Create a new slices in the world, returns all slices as a list.
    /// The solutions must be set afterwards.
    /// </summary>
    public List<EntityUid> Slice(Entity<TransformComponent?, SliceableFoodComponent?> entity, int count)
    {
        var fail = new List<EntityUid>() { EntityUid.Invalid };

        if (!Resolve(entity, ref entity.Comp1, ref entity.Comp2))
            return fail;

        var slices = _stack.SpawnMultipleAtPosition(entity.Comp2.Slice!.Value, count, entity.Comp1.Coordinates);
        foreach (var sliceUid in slices)
        {
            if (!Place(entity, sliceUid))
                return fail;
        }

        return slices;
    }

    /// <summary>
    /// Adjusts the position of a spawned slice, or tries to fit it properly within a container
    /// Returns true if all slice is positioned successfully
    /// </summary>
    public bool Place(Entity<TransformComponent?, SliceableFoodComponent?> entity, EntityUid sliceUid)
    {
        if (!Resolve(entity, ref entity.Comp1, ref entity.Comp2))
            return false;

        // try putting the slice into the container if the food being sliced is in a container!
        // this lets you do things like slice a pizza up inside of a hot food cart without making a food-everywhere mess
        _transform.DropNextTo(sliceUid, entity);
        _transform.SetLocalRotation(sliceUid, 0);

        if (!_container.IsEntityOrParentInContainer(sliceUid))
        {
            var randVect = _random.NextVector2(entity.Comp2.SpawnOffset, entity.Comp2.SpawnOffset);
            if (TryComp<PhysicsComponent>(sliceUid, out var physics))
                _physics.SetLinearVelocity(sliceUid, randVect, body: physics);
        }

        return true;
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

    /// <summary>
    ///
    /// </summary>
    /// <param name="slice"> The slice entity </param>
    /// <param name="solution"> portion of a solution from the sliced entity </param>
    /// <param name="targetContainer"> target solution container </param>
    private void FillSlice(Entity<SolutionContainerManagerComponent?> slice, Solution solution, string targetContainer)
    {
        if (!Resolve(slice, ref slice.Comp, false))
            return;

        // Replace all reagents on prototype not just copying poisons (example: slices of eaten pizza should have less nutrition)
        if (!_solutionContainer.TryGetSolution(slice.Owner, targetContainer, out var itsSoln, out var itsSolution))
            return;

        _solutionContainer.RemoveAllSolution(itsSoln.Value);

        var lostSolutionPart = solution.SplitSolution(itsSolution.AvailableVolume);
        _solutionContainer.TryAddSolution(itsSoln.Value, lostSolutionPart);
    }
}

