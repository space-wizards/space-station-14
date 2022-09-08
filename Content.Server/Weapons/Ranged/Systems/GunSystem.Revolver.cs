using Content.Shared.Weapons.Ranged.Components;

namespace Content.Server.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    protected override void SpinRevolver(RevolverAmmoProviderComponent component, EntityUid? user = null)
    {
        base.SpinRevolver(component, user);
        var index = Random.Next(component.Capacity);

        if (component.CurrentIndex == index) return;

        component.CurrentIndex = index;
        Dirty(component);
    }
}
