using Content.Shared.Speech.Components;
using Content.Shared.StatusEffect;
using Robust.Shared.Prototypes;

namespace Content.Shared.Speech.EntitySystems;

public abstract class SharedSlurredSystem : RelayAccentSystem<SlurredAccentComponent>
{
    public static readonly EntProtoId Stutter = "StatusEffectSlurred";

    public virtual void DoSlur(EntityUid uid, TimeSpan time, StatusEffectsComponent? status = null) { }
}
