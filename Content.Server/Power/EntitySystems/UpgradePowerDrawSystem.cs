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
        if (!TryComp<ApcPowerReceiverComponent>(uid, out var powa))
            return;
        component.BaseLoad = powa.Load;
    }

    private void OnRefreshParts(EntityUid uid, UpgradePowerDrawComponent component, RefreshPartsEvent args)
    {
        if (!TryComp<ApcPowerReceiverComponent>(uid, out var powa))
            return;
        var rating = args.PartRatings[component.MachinePartPowerDraw];
        switch (component.Scaling)
        {
            case PowerDrawScalingType.Linear:
                powa.Load = component.BaseLoad + component.Modifier * (rating - 1);
                break;
            case PowerDrawScalingType.Exponential:
                powa.Load = component.BaseLoad * MathF.Pow(component.Modifier, rating - 1);
                break;
            default:
                Logger.Error($"invalid power scaling type for {ToPrettyString(uid)}.");
                break;
        }
    }
}
