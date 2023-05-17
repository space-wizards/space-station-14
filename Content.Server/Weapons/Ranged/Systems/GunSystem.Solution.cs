using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Vapor;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Map;

namespace Content.Server.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;

    protected override void InitializeSolution()
    {
        base.InitializeSolution();

        SubscribeLocalEvent<SolutionAmmoProviderComponent, MapInitEvent>(OnSolutionMapInit);
        SubscribeLocalEvent<SolutionAmmoProviderComponent, SolutionChangedEvent>(OnSolutionChanged);
    }

    private void OnSolutionMapInit(EntityUid uid, SolutionAmmoProviderComponent component, MapInitEvent args)
    {
        UpdateSolutionShots(uid, component);
    }

    private void OnSolutionChanged(EntityUid uid, SolutionAmmoProviderComponent component, SolutionChangedEvent args)
    {
        if (args.Solution.Name == component.SolutionId)
            UpdateSolutionShots(uid, component, args.Solution);
    }

    protected override void UpdateSolutionShots(EntityUid uid, SolutionAmmoProviderComponent component, Solution? solution = null)
    {
        var shots = 0;
        var maxShots = 0;
        if (solution == null && !_solutionContainer.TryGetSolution(uid, component.SolutionId, out solution))
        {
            component.Shots = shots;
            component.MaxShots = maxShots;
            Dirty(component);
            return;
        }

        shots = (int) (solution.Volume / component.FireCost);
        maxShots = (int) (solution.MaxVolume / component.FireCost);

        component.Shots = shots;
        component.MaxShots = maxShots;
        Dirty(component);

        UpdateSolutionAppearance(uid, component);
    }

    protected override (EntityUid Entity, IShootable) GetSolutionShot(EntityUid uid, SolutionAmmoProviderComponent component, EntityCoordinates position)
    {
        var (ent, shootable) = base.GetSolutionShot(uid, component, position);

        if (!_solutionContainer.TryGetSolution(uid, component.SolutionId, out var solution))
            return (ent, shootable);

        var newSolution = _solutionContainer.SplitSolution(uid, solution, component.FireCost);

        if (newSolution.Volume <= FixedPoint2.Zero)
            return (ent, shootable);

        if (TryComp<AppearanceComponent>(ent, out var appearance))
        {
            Appearance.SetData(ent, VaporVisuals.Color, newSolution.GetColor(ProtoManager).WithAlpha(1f), appearance);
            Appearance.SetData(ent, VaporVisuals.State, true, appearance);
        }

        // Add the solution to the vapor and actually send the thing
        if (_solutionContainer.TryGetSolution(ent, VaporComponent.SolutionName, out var vaporSolution))
        {
            _solutionContainer.TryAddSolution(ent, vaporSolution, newSolution);
        }
        return (ent, shootable);
    }
}
