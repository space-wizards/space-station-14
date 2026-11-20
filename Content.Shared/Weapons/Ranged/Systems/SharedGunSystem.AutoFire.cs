using Content.Shared.Weapons.Ranged.Components;

namespace Content.Shared.Weapons.Ranged.Systems;

public partial class SharedGunSystem
{
    public void SetEnabled(Entity<AutoShootGunComponent?> ent, bool status)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (ent.Comp.Enabled == status)
            return;

        ent.Comp.Enabled = status;
        Dirty(ent);
    }
}
