using Content.Shared.StatusEffect;

namespace Content.Shared.Speech.EntitySystems;

public abstract class SharedSlurredSystem : EntitySystem
{
    public virtual void DoSlur(EntityUid uid, TimeSpan time, StatusEffectsComponent? status = null) { }
}
