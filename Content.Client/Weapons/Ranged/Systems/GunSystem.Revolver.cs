using Content.Shared.Weapons.Ranged.Components;

namespace Content.Client.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    protected override void InitializeRevolver()
    {
        base.InitializeRevolver();
        SubscribeLocalEvent<RevolverAmmoProviderComponent, AmmoCounterControlEvent>(OnRevolverCounter);
        SubscribeLocalEvent<RevolverAmmoProviderComponent, UpdateAmmoCounterEvent>(OnRevolverAmmoUpdate);
    }

    private void OnRevolverAmmoUpdate(EntityUid uid, RevolverAmmoProviderComponent component, UpdateAmmoCounterEvent args)
    {
        if (args.Control is not RevolverStatusControl control) return;
        control.Update(component.CurrentIndex, component.Chambers);
    }

    private void OnRevolverCounter(EntityUid uid, RevolverAmmoProviderComponent component, AmmoCounterControlEvent args)
    {
        args.Control = new RevolverStatusControl();
    }

    protected override void SpinRevolver(RevolverAmmoProviderComponent component, EntityUid? user = null)
    {
        PlaySound(component.Owner, component.SoundSpin?.GetSound(), user);
    }
}
