using Content.Shared.Speech.EntitySystems;

namespace Content.Server.Speech.EntitySystems;

public sealed class RatvarianLanguageSystem : SharedRatvarianLanguageSystem
{
    public override void DoRatvarian(EntityUid uid, TimeSpan time, bool refresh)
    {
        if (refresh)
            Status.TryUpdateStatusEffectDuration(uid, Ratvarian, time);
        else
            Status.TryAddStatusEffectDuration(uid, Ratvarian, time);
    }
}
