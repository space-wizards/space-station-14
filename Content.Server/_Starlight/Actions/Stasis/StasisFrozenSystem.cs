using Content.Shared._Starlight.Actions.Stasis;

namespace Content.Server._Starlight.Actions.Stasis;

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
