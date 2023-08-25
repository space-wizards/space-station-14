using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Construction;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Chemistry;
using Content.Shared.Placeable;

namespace Content.Server.Chemistry.EntitySystems;

public sealed class SolutionHeaterSystem : EntitySystem
{
    [Dependency] private readonly PowerReceiverSystem _powerReceiver = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SolutionContainerSystem _solution = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SolutionHeaterComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<SolutionHeaterComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<SolutionHeaterComponent, UpgradeExamineEvent>(OnUpgradeExamine);
        SubscribeLocalEvent<SolutionHeaterComponent, ItemPlacedEvent>(OnItemPlaced);
        SubscribeLocalEvent<SolutionHeaterComponent, ItemRemovedEvent>(OnItemRemoved);
    }

    private void TurnOn(EntityUid uid)
    {
        _appearance.SetData(uid, SolutionHeaterVisuals.IsOn, true);
        EnsureComp<ActiveSolutionHeaterComponent>(uid);
    }

    public bool TryTurnOn(EntityUid uid, ItemPlacerComponent? placer = null)
    {
        if (!Resolve(uid, ref placer))
            return false;

        if (placer.PlacedEntities.Count <= 0 || !_powerReceiver.IsPowered(uid))
            return false;

        TurnOn(uid);
        return true;
    }

    public void TurnOff(EntityUid uid)
    {
        _appearance.SetData(uid, SolutionHeaterVisuals.IsOn, false);
        RemComp<ActiveSolutionHeaterComponent>(uid);
    }

    private void OnPowerChanged(EntityUid uid, SolutionHeaterComponent component, ref PowerChangedEvent args)
    {
        var placer = Comp<ItemPlacerComponent>(uid);
        if (args.Powered && placer.PlacedEntities.Count > 0)
        {
            TurnOn(uid);
        }
        else
        {
            TurnOff(uid);
        }
    }

    private void OnRefreshParts(EntityUid uid, SolutionHeaterComponent component, RefreshPartsEvent args)
    {
        var heatRating = args.PartRatings[component.MachinePartHeatMultiplier] - 1;

        component.HeatPerSecond = component.BaseHeatPerSecond * MathF.Pow(component.PartRatingHeatMultiplier, heatRating);
    }

    private void OnUpgradeExamine(EntityUid uid, SolutionHeaterComponent component, UpgradeExamineEvent args)
    {
        args.AddPercentageUpgrade("solution-heater-upgrade-heat", component.HeatPerSecond / component.BaseHeatPerSecond);
    }

    private void OnItemPlaced(EntityUid uid, SolutionHeaterComponent comp, ref ItemPlacedEvent args)
    {
        TryTurnOn(uid);
    }

    private void OnItemRemoved(EntityUid uid, SolutionHeaterComponent component, ref ItemRemovedEvent args)
    {
        var placer = Comp<ItemPlacerComponent>(uid);
        if (placer.PlacedEntities.Count == 0) // Last entity was removed
            TurnOff(uid);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveSolutionHeaterComponent, SolutionHeaterComponent, ItemPlacerComponent>();
        while (query.MoveNext(out _, out _, out var heater, out var placer))
        {
            foreach (var heatingEntity in placer.PlacedEntities)
            {
                if (!TryComp<SolutionContainerManagerComponent>(heatingEntity, out var solution))
                    continue;

                var energy = heater.HeatPerSecond * frameTime;
                foreach (var s in solution.Solutions.Values)
                {
                    _solution.AddThermalEnergy(heatingEntity, s, energy);
                }
            }
        }
    }
}
