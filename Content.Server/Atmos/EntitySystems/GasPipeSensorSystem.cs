using Content.Server.Power.Components;
using Content.Shared.Atmos.Components;
using Content.Shared.Power;

public sealed class GasPipeSensorSystem : SharedGasPipeSensorSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasPipeSensorComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<GasPipeSensorComponent, PowerChangedEvent>(OnPowerChangedEvent);
    }

    private void OnComponentInit(EntityUid uid, GasPipeSensorComponent component, ComponentInit ev)
    {
        var isActive = false;

        if (TryComp<ApcPowerReceiverComponent>(uid, out var apcPowerReceiver))
            isActive = apcPowerReceiver.Powered;

        UpdateVisuals(uid, component, isActive);
    }

    private void OnPowerChangedEvent(EntityUid uid, GasPipeSensorComponent component, PowerChangedEvent ev)
    {
        var isActive = ev.Powered;

        UpdateVisuals(uid, component, isActive);
    }
}
