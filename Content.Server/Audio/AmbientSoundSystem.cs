using Content.Server.CrystallPunk.Temperature;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Audio;

namespace Content.Server.Audio;

public sealed class AmbientSoundSystem : SharedAmbientSoundSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AmbientOnPoweredComponent, PowerChangedEvent>(HandlePowerChange);
        SubscribeLocalEvent<AmbientOnPoweredComponent, PowerNetBatterySupplyEvent>(HandlePowerSupply);
        SubscribeLocalEvent<CPFlammableAmbientSoundComponent, OnFireChangedEvent>(OnFireChanged); //CrystallPunk bonfire moment
    }

    private void HandlePowerSupply(EntityUid uid, AmbientOnPoweredComponent component, ref PowerNetBatterySupplyEvent args)
    {
        SetAmbience(uid, args.Supply);
    }

    private void HandlePowerChange(EntityUid uid, AmbientOnPoweredComponent component, ref PowerChangedEvent args)
    {
        SetAmbience(uid, args.Powered);
    }

    //CrystallPunk bonfire moment
    private void OnFireChanged(Entity<CPFlammableAmbientSoundComponent> ent, ref OnFireChangedEvent args)
    {
        SetAmbience(ent, args.OnFire);
    }
    //CrystallPunk bonfire moment end
}
