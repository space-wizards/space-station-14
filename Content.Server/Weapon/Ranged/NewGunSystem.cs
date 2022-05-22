using Content.Shared.Weapons.Ranged;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Input;
using Robust.Shared.Player;

namespace Content.Server.Weapon.Ranged;

public sealed class NewGunSystem : SharedNewGunSystem
{
    protected override void PlaySound(NewGunComponent gun, string? sound, int shots, EntityUid? user = null)
    {
        if (sound == null) return;

        SoundSystem.Play(Filter.Pvs(gun.Owner).RemoveWhereAttachedEntity(e => e == user), sound, gun.Owner);
    }
}
