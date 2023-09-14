using Content.Shared.StatusEffect;

namespace Content.Shared.Speech.EntitySystems;

public abstract class SharedRatvarianLanguageSystem : EntitySystem
{
    public virtual void DoRatvarian(EntityUid uid, TimeSpan time, bool refresh, StatusEffectsComponent? status = null)
    {
    }
}
