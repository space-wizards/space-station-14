using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Server.Power.Components;

namespace Content.Server.Power.EntitySystems;

/// <summary>
/// This handles using upgraded machine parts
/// to modify the power supply/generation of a machine.
/// </summary>
public sealed class UpgradePowerSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<UpgradePowerDrawComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<UpgradePowerDrawComponent, RefreshPartsEvent>(OnRefreshParts);

        SubscribeLocalEvent<UpgradePowerSupplierComponent, MapInitEvent>(OnSupplierMapInit);
        SubscribeLocalEvent<UpgradePowerSupplierComponent, RefreshPartsEvent>(OnSupplierRefreshParts);
    }

    private void OnMapInit(EntityUid uid, UpgradePowerDrawComponent component, MapInitEvent args)
    {
        if (TryComp<PowerConsumerComponent>(uid, out var powa))
            component.BaseLoad = powa.DrawRate;
        else if (TryComp<ApcPowerReceiverComponent>(uid, out var powa2))
            component.BaseLoad = powa2.Load;
    }

    private void OnRefreshParts(EntityUid uid, UpgradePowerDrawComponent component, RefreshPartsEvent args)
    {
        var load = component.BaseLoad;
        var rating = args.PartRatings[component.MachinePartPowerDraw];
        switch (component.Scaling)
        {
            case MachineUpgradeScalingType.Linear:
                load += component.PowerDrawMultiplier * (rating - 1);
                break;
            case MachineUpgradeScalingType.Exponential:
                load *= MathF.Pow(component.PowerDrawMultiplier, rating - 1);
                break;
            default:
                Logger.Error($"invalid power scaling type for {ToPrettyString(uid)}.");
                load = 0;
                break;
        }
        if (TryComp<ApcPowerReceiverComponent>(uid, out var powa))
            powa.Load = load;
        if (TryComp<PowerConsumerComponent>(uid, out var powa2))
            powa2.DrawRate = load;
    }

    private void OnSupplierMapInit(EntityUid uid, UpgradePowerSupplierComponent component, MapInitEvent args)
    {
        if (TryComp<PowerSupplierComponent>(uid, out var supplier))
            component.BaseSupplyRate = supplier.MaxSupply;
    }

    private void OnSupplierRefreshParts(EntityUid uid, UpgradePowerSupplierComponent component, RefreshPartsEvent args)
    {
        if (!TryComp<PowerSupplierComponent>(uid, out var powa))
            return;

        var rating = args.PartRatings[component.MachinePartPowerSupply];
        switch (component.Scaling)
        {
            case MachineUpgradeScalingType.Linear:
                powa.MaxSupply += component.BaseSupplyRate * (rating - 1);
                break;
            case MachineUpgradeScalingType.Exponential:
                powa.MaxSupply *= MathF.Pow(component.PowerSupplyMultiplier, rating - 1);
                break;
            default:
                Logger.Error($"invalid power scaling type for {ToPrettyString(uid)}.");
                powa.MaxSupply = component.BaseSupplyRate;
                break;
        }
    }
}
