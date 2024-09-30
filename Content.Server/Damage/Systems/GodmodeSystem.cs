using Content.Shared.Atmos.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;

namespace Content.Server.Damage.Systems;

public sealed class GodmodeSystem : SharedGodmodeSystem
{
    public override void EnableGodmode(EntityUid uid, GodmodeComponent? godmode = null)
    {
        godmode ??= EnsureComp<GodmodeComponent>(uid);

        base.EnableGodmode(uid, godmode);

        if (TryComp<MovedByPressureComponent>(uid, out var moved))
        {
            godmode.WasMovedByPressure = moved.Enabled;
            moved.Enabled = false;
        }
    }

    public override void DisableGodmode(EntityUid uid, GodmodeComponent? godmode = null)
    {
    	if (!Resolve(uid, ref godmode, false))
    	    return;

        base.DisableGodmode(uid, godmode);

        if (godmode.Deleted)
            return;

        if (TryComp<MovedByPressureComponent>(uid, out var moved))
        {
            moved.Enabled = godmode.WasMovedByPressure;
        }
    }
}
