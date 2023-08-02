using Content.Shared.StatusEffect;

namespace Content.Shared.Speech.EntitySystems;

public abstract class SharedSqueakSpeakSystem : EntitySystem
{
    public virtual void DoSqueakSpeak(EntityUid uid, TimeSpan time, bool refresh, StatusEffectsComponent? status = null)
    {

    }
}
