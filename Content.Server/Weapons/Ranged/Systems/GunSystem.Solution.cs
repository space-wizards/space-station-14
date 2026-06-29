using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Vapor;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Map;

namespace Content.Server.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private VaporSystem _vapor = default!;

    protected override void InitializeSolution()
    {
        base.InitializeSolution();

        SubscribeLocalEvent<SolutionAmmoProviderComponent, MapInitEvent>(OnSolutionMapInit);
        SubscribeLocalEvent<SolutionAmmoProviderComponent, SolutionChangedEvent>(OnSolutionChanged);
    }

    private void OnSolutionMapInit(Entity<SolutionAmmoProviderComponent> entity, ref MapInitEvent args)
    {
        UpdateSolutionShots(entity);
    }

    private void OnSolutionChanged(Entity<SolutionAmmoProviderComponent> entity, ref SolutionChangedEvent args)
    {
        if (args.Solution.Comp.Id == entity.Comp.SolutionId)
            UpdateSolutionShots(entity, args.Solution.Comp.Solution);
    }

    protected override void UpdateSolutionShots(Entity<SolutionAmmoProviderComponent> ent, Solution? solution = null)
    {
        var shots = 0;
        var maxShots = 0;
        if (solution == null && !_solutionContainer.TryGetSolution(ent.Owner, ent.Comp.SolutionId, out _, out solution))
        {
            ent.Comp.Shots = shots;
            DirtyField(ent.AsNullable(), nameof(SolutionAmmoProviderComponent.Shots));
            ent.Comp.MaxShots = maxShots;
            DirtyField(ent.AsNullable(), nameof(SolutionAmmoProviderComponent.MaxShots));
            return;
        }

        shots = (int)(solution.Volume / ent.Comp.FireCost);
        maxShots = (int)(solution.MaxVolume / ent.Comp.FireCost);

        ent.Comp.Shots = shots;
        DirtyField(ent.AsNullable(), nameof(SolutionAmmoProviderComponent.Shots));

        ent.Comp.MaxShots = maxShots;
        DirtyField(ent.AsNullable(), nameof(SolutionAmmoProviderComponent.MaxShots));

        UpdateSolutionAppearance(ent);
    }

    protected override (EntityUid Entity, IShootable) GetSolutionShot(Entity<SolutionAmmoProviderComponent> ent, EntityCoordinates position)
    {
        var (shot, shootable) = base.GetSolutionShot(ent, position);

        if (!_solutionContainer.TryGetSolution(ent.Owner, ent.Comp.SolutionId, out var solution))
            return (shot, shootable);

        _vapor.TryAddSolution(shot, solution.Value, ent.Comp.FireCost);
        return (shot, shootable);
    }
}
