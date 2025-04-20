using Content.Shared.Speech.Accents;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.StatusEffect;

namespace Content.Server.Speech.EntitySystems;

public sealed class SlurredSystem : SharedSlurredSystem
{
    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;

    [ValidatePrototypeId<StatusEffectPrototype>]
    private const string SlurKey = "SlurredSpeech";

    public override void DoSlur(EntityUid uid, TimeSpan time, StatusEffectsComponent? status = null)
    {
        if (!Resolve(uid, ref status, false))
            return;

        if (!_statusEffectsSystem.HasStatusEffect(uid, SlurKey, status))
            _statusEffectsSystem.TryAddStatusEffect<SlurredAccentComponent>(uid, SlurKey, time, true, status);
        else
            _statusEffectsSystem.TryAddTime(uid, SlurKey, time, status);
    }
}
