using Content.Shared.Weapons.Ranged.Components;

namespace Content.Shared.Weapons.Ranged.Systems;

public partial class SharedGunSystem
{
    [SubscribeLocalEvent]
    private void OnBurstStopped(Entity<GunAltFireComponent> ent, ref GunBurstStoppedEvent args)
    {
        if (args.User != null && ent.Comp.ForceWielding)
        {
            Wieldable.TryUnwield((ent, null), args.User.Value);
        }
    }

    [SubscribeLocalEvent]
    private void OnShootingStopped(Entity<GunAltFireComponent> ent, ref GunShootingStoppedEvent args)
    {
        // GunAltFire treats force wielding as requiring both hands while shooting; therefore it auto-unwields upon stopping.
        if (args.User != null && ent.Comp.ForceWielding)
        {
            Wieldable.TryUnwield((ent, null), args.User.Value);
        }
    }
}
