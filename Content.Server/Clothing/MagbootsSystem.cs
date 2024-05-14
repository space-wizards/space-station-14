using Content.Server.Atmos.Components;
using Content.Shared.Clothing;

namespace Content.Server.Clothing;

public sealed class MagbootsSystem : SharedMagbootsSystem
{
    protected override void UpdateMagbootEffects(EntityUid user, bool on)
    {
        base.UpdateMagbootEffects(user, on);

        if (TryComp<MovedByPressureComponent>(user, out var moved))
            moved.Enabled = !on;
    }
}
