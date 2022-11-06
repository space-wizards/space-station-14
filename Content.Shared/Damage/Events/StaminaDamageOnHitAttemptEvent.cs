using Robust.Shared.Audio;

namespace Content.Shared.Damage.Events;

/// <summary>
/// Attempting to apply stamina damage on a melee hit to an entity.
/// </summary>
[ByRefEvent]
public struct StaminaDamageOnHitAttemptEvent
{
    public bool Cancelled;
    public SoundSpecifier? HitSoundOverride;

    public StaminaDamageOnHitAttemptEvent(bool cancelled, SoundSpecifier? hitSoundOverride)
    {
        Cancelled = cancelled;
        HitSoundOverride = hitSoundOverride;
    }
}
