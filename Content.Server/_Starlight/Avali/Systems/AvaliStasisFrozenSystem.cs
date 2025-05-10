using Content.Shared.Starlight.Avali.Components;
using Content.Shared.Starlight.Avali.Systems;

namespace Content.Server.Starlight.Avali.Systems;

public sealed class AvaliStasisFrozenSystem : SharedAvaliStasisFrozenSystem
{
    /// <summary>
    /// Freezes and mutes the given entity.
    /// </summary>
    public void FreezeAndMute(EntityUid uid)
    {
        var comp = EnsureComp<AvaliStasisFrozenComponent>(uid);
        comp.Muted = false;
        Dirty(uid, comp);
    }
} 