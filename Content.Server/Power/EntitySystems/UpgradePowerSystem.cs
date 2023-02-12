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
        SubscribeLocalEvent<UpgradePowerDrawComponent, UpgradeExamineEvent>(OnUpgradeExamine);

        SubscribeLocalEvent<UpgradePowerSupplierComponent, MapInitEvent>(OnSupplierMapInit);
        SubscribeLocalEvent<UpgradePowerSupplierComponent, RefreshPartsEvent>(OnSupplierRefreshParts);
        SubscribeLocalEvent<UpgradePowerSupplierComponent, UpgradeExamineEvent>(OnSupplierUpgradeExamine);
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

    private void OnUpgradeExamine(EntityUid uid, UpgradePowerDrawComponent component, UpgradeExamineEvent args)
    {
        // UpgradePowerDrawComponent.PowerDrawMultiplier is not the actual multiplier, so we have to do this.
        var powerDrawMultiplier = CompOrNull<ApcPowerReceiverComponent>(uid)?.Load / component.BaseLoad
            ?? CompOrNull<PowerConsumerComponent>(uid)?.DrawRate / component.BaseLoad;

        if (powerDrawMultiplier is not null)
            args.AddPercentageUpgrade("upgrade-power-draw", powerDrawMultiplier.Value);
    }

    private void OnSupplierMapInit(EntityUid uid, UpgradePowerSupplierComponent component, MapInitEvent args)
    {
        if (TryComp<PowerSupplierComponent>(uid, out var supplier))
            component.BaseSupplyRate = supplier.MaxSupply;
    }

    private void OnSupplierRefreshParts(EntityUid uid, UpgradePowerSupplierComponent component, RefreshPartsEvent args)
    {
        var supply = component.BaseSupplyRate;
        var rating = args.PartRatings[component.MachinePartPowerSupply];
        switch (component.Scaling)
        {
            case MachineUpgradeScalingType.Linear:
                supply += component.BaseSupplyRate * (rating - 1);
                break;
            case MachineUpgradeScalingType.Exponential:
                supply *= MathF.Pow(component.PowerSupplyMultiplier, rating - 1);
                break;
            default:
                Logger.Error($"invalid power scaling type for {ToPrettyString(uid)}.");
                supply = component.BaseSupplyRate;
                break;
        }

        if (TryComp<PowerSupplierComponent>(uid, out var powa))
            powa.MaxSupply = supply;
    }

    private void OnSupplierUpgradeExamine(EntityUid uid, UpgradePowerSupplierComponent component, UpgradeExamineEvent args)
    {
        // UpgradePowerSupplierComponent.PowerSupplyMultiplier is not the actual multiplier, so we have to do this.
        if (TryComp<PowerSupplierComponent>(uid, out var powa))
            args.AddPercentageUpgrade("upgrade-power-supply", powa.MaxSupply / component.BaseSupplyRate);
    }
}
