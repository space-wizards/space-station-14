using Content.Server.Chemistry.Components;
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
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    protected override void InitializeSolution()
    {
        base.InitializeSolution();

        SubscribeLocalEvent<SolutionAmmoProviderComponent, MapInitEvent>(OnSolutionMapInit);
        SubscribeLocalEvent<SolutionAmmoProviderComponent, SolutionContainerChangedEvent>(OnSolutionChanged);
    }

    private void OnSolutionMapInit(Entity<SolutionAmmoProviderComponent> entity, ref MapInitEvent args)
    {
        UpdateSolutionShots(entity);
    }

    private void OnSolutionChanged(Entity<SolutionAmmoProviderComponent> entity, ref SolutionContainerChangedEvent args)
    {
        if (args.Solution.Name == entity.Comp.SolutionId)
            UpdateSolutionShots(entity, args.Solution);
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

        if (!_solutionContainer.TryGetSolution(ent.Owner, ent.Comp.SolutionId, out var solution, out _))
            return (shot, shootable);

        var newSolution = _solutionContainer.SplitSolution(solution.Value, ent.Comp.FireCost);

        if (newSolution.Volume <= FixedPoint2.Zero)
            return (shot, shootable);

        if (TryComp<AppearanceComponent>(shot, out var appearance))
        {
            Appearance.SetData(shot, VaporVisuals.Color, newSolution.GetColor(ProtoManager).WithAlpha(1f), appearance);
            Appearance.SetData(shot, VaporVisuals.State, true, appearance);
        }

        // Add the solution to the vapor and actually send the thing
        if (_solutionContainer.TryGetSolution(shot, VaporComponent.SolutionName, out var vaporSolution, out _))
        {
            _solutionContainer.TryAddSolution(vaporSolution.Value, newSolution);
        }
        return (shot, shootable);
    }
}
