using Content.Shared.Weapons.Ranged.Components;

namespace Content.Shared.Weapons.Ranged.Systems;

public partial class SharedGunSystem
{
    public void SetEnabled(EntityUid uid, AutoShootGunComponent component, bool status)
    {
        component.Enabled = status;
    }
}
