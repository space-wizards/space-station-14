using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Construction;
using Content.Server.Power.Components;
using Content.Shared.Containers.ItemSlots;

namespace Content.Server.Chemistry.EntitySystems;

public sealed class SolutionHeaterSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SolutionContainerSystem _solution = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<SolutionHeaterComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<SolutionHeaterComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<SolutionHeaterComponent, UpgradeExamineEvent>(OnUpgradeExamine);
    }

    private void OnPowerChanged(EntityUid uid, SolutionHeaterComponent component, ref PowerChangedEvent args)
    {
        if (args.Powered)
        {
            EnsureComp<ActiveSolutionHeaterComponent>(uid);
        }
        else
        {
            RemComp<ActiveSolutionHeaterComponent>(uid);
        }
    }

    private void OnRefreshParts(EntityUid uid, SolutionHeaterComponent component, RefreshPartsEvent args)
    {
        var heatRating = args.PartRatings[component.MachinePartHeatPerSecond] - 1;

        component.HeatMultiplier = MathF.Pow(component.PartRatingHeatMultiplier, heatRating);
    }

    private void OnUpgradeExamine(EntityUid uid, SolutionHeaterComponent component, UpgradeExamineEvent args)
    {
        args.AddPercentageUpgrade("solution-heater-upgrade-heat", component.HeatMultiplier);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var (_, heater) in EntityQuery<ActiveSolutionHeaterComponent, SolutionHeaterComponent>())
        {
            if (_itemSlots.GetItemOrNull(heater.Owner, heater.BeakerSlotId) is not { } item)
                continue;

            if (!TryComp<SolutionContainerManagerComponent>(item, out var solution))
                continue;

            var energy = heater.HeatPerSecond * heater.HeatMultiplier * frameTime;
            foreach (var s in solution.Solutions.Values)
            {
                _solution.AddThermalEnergy(solution.Owner, s, energy);
            }
        }
    }
}
