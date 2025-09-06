using Content.Shared.Traits.Assorted;
using Robust.Shared.Audio;

namespace Content.Server.Traits.Assorted;

public sealed class ParacusiaSystem : SharedParacusiaSystem
{
    public void SetSounds(Entity<ParacusiaComponent?> ent, SoundSpecifier sounds)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.Sounds = sounds;
        Dirty(ent);
    }

    public void SetTime(Entity<ParacusiaComponent?> ent, TimeSpan minTime, TimeSpan maxTime)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.MinTimeBetweenIncidents = minTime;
        ent.Comp.MaxTimeBetweenIncidents = maxTime;
        Dirty(ent);
    }

    public void SetDistance(Entity<ParacusiaComponent?> ent, float maxSoundDistance)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.MaxSoundDistance = maxSoundDistance;
        Dirty(ent);
    }
}
