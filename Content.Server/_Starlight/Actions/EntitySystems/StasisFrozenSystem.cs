using Content.Shared._Starlight.Actions.EntitySystems;
using Content.Shared._Starlight.Actions.Components;

namespace Content.Server._Starlight.Actions.EntitySystems;

public sealed class StasisFrozenSystem : SharedStasisFrozenSystem
{
    /// <summary>
    /// Freezes and mutes the given entity.
    /// </summary>
    public void FreezeAndMute(EntityUid uid)
    {
        var comp = EnsureComp<StasisFrozenComponent>(uid);
        comp.Muted = false;
        Dirty(uid, comp);
    }
}
