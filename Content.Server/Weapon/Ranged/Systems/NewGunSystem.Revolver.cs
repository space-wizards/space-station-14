using Content.Shared.Weapons.Ranged;

namespace Content.Server.Weapon.Ranged;

public sealed partial class GunSystem
{
    protected override void SpinRevolver(SharedRevolverAmmoProviderComponent component, EntityUid? user = null)
    {
        PlaySound(component.Owner, component.SoundSpin?.GetSound(), user);
        var index = Random.Next(component.Capacity);

        if (component.CurrentIndex == index) return;

        component.CurrentIndex = index;
        Dirty(component);
    }
}
