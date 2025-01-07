using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Timing;
using Content.Shared.Toggleable;
using Content.Shared.Mobs;
using Content.Shared.Chemistry.Components;
using Content.Shared.Mobs.Components;

namespace Content.Shared.Animals;

/// <summary>
///     Gives ability to produce fiber reagents;
///     produces endlessly if the owner has no HungerComponent.
/// </summary>
public sealed class WoolySystem : EntitySystem
{
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WoolyComponent, BeforeFullyEatenEvent>(OnBeforeFullyEaten);
        SubscribeLocalEvent<WoolyComponent, SolutionContainerChangedEvent>(OnSolutionChange);
        SubscribeLocalEvent<WoolyComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<WoolyComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, WoolyComponent component, MapInitEvent args)
    {
        component.NextGrowth = _timing.CurTime + component.GrowthDelay;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<WoolyComponent>();
        while (query.MoveNext(out var uid, out var wooly))
        {
            if (_timing.CurTime < wooly.NextGrowth)
                continue;

            wooly.NextGrowth += wooly.GrowthDelay;

            if (_mobState.IsDead(uid))
                continue;

            if (!_solutionContainer.ResolveSolution(uid, wooly.SolutionName, ref wooly.Solution, out var solution))
                continue;

            if (solution.AvailableVolume == 0)
                continue;

            // Actually there is food digestion so no problem with instant reagent generation "OnFeed"
            if (EntityManager.TryGetComponent(uid, out HungerComponent? hunger))
            {
                // Is there enough nutrition to produce reagent?
                if (_hunger.GetHungerThreshold(hunger) < HungerThreshold.Okay)
                    continue;

                _hunger.ModifyHunger(uid, -wooly.HungerUsage, hunger);
            }

            _solutionContainer.TryAddReagent(wooly.Solution.Value, wooly.ReagentId, wooly.Quantity, out _);
        }
    }

    private void OnBeforeFullyEaten(Entity<WoolyComponent> ent, ref BeforeFullyEatenEvent args)
    {
        // don't want moths to delete goats after eating them
        args.Cancel();
    }


    /// <summary>
    ///     Used for managing the wooly layer as the wool solution levels change..
    ///     e.g. in Sheep, it will remove the wool layer when the remaining reagent drops to 0.
    ///     the layer is re-added when the reagent is above 0.
    ///     Check the sheep's Sprite and GenericVisualizer components for an example of how to add a wooly layer to your animal.
    /// </summary>
    private void OnSolutionChange(Entity<WoolyComponent> ent, ref SolutionContainerChangedEvent args)
    {
        // Only interested in wool solution, ignore the rest.
        if (args.SolutionId != ent.Comp.SolutionName)
            return;

        UpdateWoolLayer(ent, args.Solution);
    }

    /// <summary>
    ///     This function checks the entity's wool solution and either disables or enables the wool layer (if one exists).
    /// </summary>
    /// <param name="ent">the entity containing a wooly component that will be checked.</param>
    /// <param name="sol">a resolved solution object the prescence of which will be checked.
    private void UpdateWoolLayer(Entity<WoolyComponent> ent, Solution? sol = null)
    {
        // If the sol parameter hasn't been provided, we'll try to grab the solution from inside the animal instead.
        Solution? solution;
        if (sol == null)
        {
            _solutionContainer.ResolveSolution(
                ent.Owner,
                ent.Comp.SolutionName,
                ref ent.Comp.Solution,
                out solution
            );

            // Somehow, this entity has no wool solution.
            if (solution == null)
            {
                return;
            }
        }
        else
        {
            solution = sol;
        }

        // appearance is used to disable and enable the wool layer.
        TryComp<AppearanceComponent>(ent.Owner, out var appearance);
        // mState is used to check if the animal is dead/critical.
        TryComp<MobStateComponent>(ent.Owner, out var mState);

        // If we couldn't resolve the mobState for some reason then just assume it's alive.
        mState ??= new MobStateComponent();

        // If there's no solution at all, or the entity is dead or critical, remove the wool layer.
        // Otherwise, enable it.
        if (solution.Volume.Value <= 0 || mState.CurrentState == MobState.Dead || mState.CurrentState == MobState.Critical)
        {
            // Remove wool layer
            _appearance.SetData(ent.Owner, ToggleVisuals.Toggled, false, appearance);
        }
        else
        {
            // Add wool layer
            _appearance.SetData(ent.Owner, ToggleVisuals.Toggled, true, appearance);
        }
    }

    /// <summary>
    ///     This is used for checking if the wooly animal is dead or critical.
    ///     If it is, then the wooly layer is removed.
    private void OnMobStateChanged(Entity<WoolyComponent> ent, ref MobStateChangedEvent args)
    {
        UpdateWoolLayer(ent);
    }

}
