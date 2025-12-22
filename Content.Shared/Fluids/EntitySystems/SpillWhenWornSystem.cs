using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Clothing;
using Content.Shared.Fluids.Components;

namespace Content.Shared.Fluids.EntitySystems;

/// <inheritdoc cref="SpillWhenWornComponent"/>
public sealed class SpillWhenWornSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedPuddleSystem _puddle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpillWhenWornComponent, ClothingGotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<SpillWhenWornComponent, ClothingGotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<SpillWhenWornComponent, SolutionAccessAttemptEvent>(OnSolutionAccessAttempt);
    }

    private void OnGotEquipped(Entity<SpillWhenWornComponent> ent, ref ClothingGotEquippedEvent args)
    {
        _puddle.TrySplashSpillAt(ent.Owner, Transform(args.Wearer).Coordinates, out _, out _);

        // Flag as worn after draining, otherwise we'll block ourself from accessing!
        ent.Comp.IsWorn = true;
        Dirty(ent);
    }

    private void OnGotUnequipped(Entity<SpillWhenWornComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        ent.Comp.IsWorn = false;
        Dirty(ent);
    }

    private void OnSolutionAccessAttempt(Entity<SpillWhenWornComponent> ent, ref SolutionAccessAttemptEvent args)
    {
        // If we're not being worn right now, we don't care
        if (!ent.Comp.IsWorn)
            return;

        // Make sure it's the right solution
        if (ent.Comp.Solution != args.SolutionName)
            return;

        args.Cancelled = true;
    }
}
