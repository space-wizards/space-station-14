using Content.Shared.Weapons.Ranged;

namespace Content.Server.Weapon.Ranged;

public sealed partial class NewGunSystem
{
    protected override void SpinRevolver(SharedRevolverAmmoProviderComponent component, EntityUid? user = null)
    {
        PlaySound(component.Owner, component.SoundSpin?.GetSound(), user);
        component.CurrentIndex = Random.Next(component.Capacity);
        Dirty(component);
    }
}
