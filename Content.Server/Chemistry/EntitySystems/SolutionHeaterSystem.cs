using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Construction;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Timing;

namespace Content.Server.Chemistry.EntitySystems;

public sealed class SolutionHeaterSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SolutionContainerSystem _solution = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<SolutionHeaterComponent, RefreshPartsEvent>(OnRefreshParts);
    }

    private void OnRefreshParts(EntityUid uid, SolutionHeaterComponent component, RefreshPartsEvent args)
    {
        var heatRating = args.PartRatings[component.MachinePartHeatPerSecond] - 1;

        component.HeatMultiplier = MathF.Pow(component.PartRatingHeatMultiplier, heatRating);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var heater in EntityQuery<SolutionHeaterComponent>())
        {
            if (heater.NextHeat > _timing.CurTime)
                continue;
            heater.NextHeat = _timing.CurTime + TimeSpan.FromSeconds(1);

            if (_itemSlots.GetItemOrNull(heater.Owner, heater.BeakerSlotId) is not { } item)
                continue;

            if (!TryComp<SolutionContainerManagerComponent>(item, out var solution))
                continue;

            foreach (var s in solution.Solutions.Values)
            {
                _solution.AddThermalEnergy(solution.Owner, s, heater.HeatPerSecond * heater.HeatMultiplier);
            }
        }
    }
}
