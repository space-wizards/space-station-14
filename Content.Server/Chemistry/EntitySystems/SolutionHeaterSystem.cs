using System.Linq;
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Construction;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Chemistry;
using Content.Shared.Placeable;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Server.Chemistry.EntitySystems;

public sealed class SolutionHeaterSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly PlaceableSurfaceSystem _placeableSurface = default!;
    [Dependency] private readonly PowerReceiverSystem _powerReceiver = default!;
    [Dependency] private readonly SolutionContainerSystem _solution = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<SolutionHeaterComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<SolutionHeaterComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<SolutionHeaterComponent, UpgradeExamineEvent>(OnUpgradeExamine);
        SubscribeLocalEvent<SolutionHeaterComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<SolutionHeaterComponent, EndCollideEvent>(OnEndCollide);
    }

    private void TurnOn(EntityUid uid)
    {
        _appearance.SetData(uid, SolutionHeaterVisuals.IsOn, true);
        EnsureComp<ActiveSolutionHeaterComponent>(uid);
    }

    public bool TryTurnOn(EntityUid uid, SolutionHeaterComponent component)
    {
        if (component.PlacedEntities.Count <= 0 || !_powerReceiver.IsPowered(uid))
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
        if (args.Powered && component.PlacedEntities.Count > 0)
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

    private void OnStartCollide(EntityUid uid, SolutionHeaterComponent component, ref StartCollideEvent args)
    {
        if (component.Whitelist is not null && !component.Whitelist.IsValid(args.OtherEntity))
            return;

        // Disallow sleeping so we can detect when entity is removed from the heater.
        _physics.SetSleepingAllowed(args.OtherEntity, args.OtherBody, false);

        component.PlacedEntities.Add(args.OtherEntity);

        TryTurnOn(uid, component);

        if (component.PlacedEntities.Count >= component.MaxEntities)
            _placeableSurface.SetPlaceable(uid, false);
    }

    private void OnEndCollide(EntityUid uid, SolutionHeaterComponent component, ref EndCollideEvent args)
    {
        // Re-allow sleeping.
        _physics.SetSleepingAllowed(args.OtherEntity, args.OtherBody, true);

        component.PlacedEntities.Remove(args.OtherEntity);

        if (component.PlacedEntities.Count == 0) // Last entity was removed
            TurnOff(uid);

        _placeableSurface.SetPlaceable(uid, true);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveSolutionHeaterComponent, SolutionHeaterComponent>();
        while (query.MoveNext(out _, out _, out var heater))
        {
            foreach (var heatingEntity in heater.PlacedEntities.Take((int) heater.MaxEntities))
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
