using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Audio;
using Content.Shared.Mobs;
using Content.Shared.Power;

namespace Content.Server.Audio;

public sealed partial class AmbientSoundSystem : SharedAmbientSoundSystem
{
    [Dependency] private PowerReceiverSystem _powerReceiver = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AmbientOnPoweredComponent, MapInitEvent>(HandleMapInit);
        SubscribeLocalEvent<AmbientOnPoweredComponent, PowerChangedEvent>(HandlePowerChange);
        SubscribeLocalEvent<AmbientOnPoweredComponent, PowerNetBatterySupplyEvent>(HandlePowerSupply);
    }

    private void HandleMapInit(EntityUid uid, AmbientOnPoweredComponent component, MapInitEvent args)
    {
        SetAmbience(uid, _powerReceiver.IsPowered(uid));
    }

    private void HandlePowerSupply(EntityUid uid, AmbientOnPoweredComponent component, ref PowerNetBatterySupplyEvent args)
    {
        SetAmbience(uid, args.Supply);
    }

    private void HandlePowerChange(EntityUid uid, AmbientOnPoweredComponent component, ref PowerChangedEvent args)
    {
        SetAmbience(uid, args.Powered);
    }
}
