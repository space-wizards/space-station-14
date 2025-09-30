using Robust.Shared.Audio;

namespace Content.Shared._Starlight.Sound;

public readonly struct SoundEvent(SoundSpecifier soundSpecifier, EntityUid source, EntityUid target, AudioParams audioParams)
{
    public readonly SoundSpecifier SoundSpecifier = soundSpecifier;
    public readonly EntityUid Source = source;
    public readonly EntityUid User = target;
    public readonly AudioParams AudioParams = audioParams;
}
