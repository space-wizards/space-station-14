using Content.Server.Construction;
using Content.Server.Power.Components;

namespace Content.Server.Power.EntitySystems;

/// <summary>
/// This handles using upgraded machine parts
/// to modify the power load of a machine.
/// </summary>
public sealed class UpgradePowerDrawSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<UpgradePowerDrawComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<UpgradePowerDrawComponent, RefreshPartsEvent>(OnRefreshParts);
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
            case PowerDrawScalingType.Linear:
                load += component.PowerDrawMultiplier * (rating - 1);
                break;
            case PowerDrawScalingType.Exponential:
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
}
