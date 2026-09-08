using Content.Server.Power.EntitySystems;
using Content.Server.Xenoarchaeology.Artifact.XAE.Components;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;

namespace Content.Server.Xenoarchaeology.Artifact.XAE;

/// <summary>
/// System for xeno artifact activation effect that is fully charging batteries in certain range.
/// </summary>
public sealed partial class XAEChargeBatterySystem : BaseXAESystem<XAEChargeBatteryComponent>
{
    [Dependency] private BatterySystem _battery = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;

    /// <summary> Pre-allocated and re-used collection.</summary>
    private readonly HashSet<Entity<BatteryComponent>> _batteryEntities = new();

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAEChargeBatteryComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        XAEChargeBatteryComponent component = ent;
        var radius = component.DefaultRadius;
        if (args.Modifications.TryGetValue(XenoArtifactEffectModifier.Range, out var rangeModifier))
        {
            radius = Math.Clamp(rangeModifier.Modify(radius), component.RadiusRestrictions.X, component.RadiusRestrictions.Y);
        }

        var addCharge = component.AddChargeAmount;
        if (args.Modifications.TryGetValue(XenoArtifactEffectModifier.Power, out var amountModifier))
        {
            addCharge = Math.Clamp(amountModifier.Modify(addCharge), component.ChargeAmountRestrictions.X, component.ChargeAmountRestrictions.Y);
        }

        _batteryEntities.Clear();

        _lookup.GetEntitiesInRange(args.Coordinates, radius, _batteryEntities);
        foreach (var battery in _batteryEntities)
        {
            var charge = battery.Comp.CurrentCharge + addCharge;
            _battery.SetCharge(battery.AsNullable(), addCharge, battery);
        }
    }
}
