using Content.Shared.DeviceLinking.Components;
using Content.Shared.DeviceLinking.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.DeviceLinking.Systems;

public sealed partial class DeviceLinkOverloadSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audioSystem = default!;

    [SubscribeLocalEvent]
    private void OnOverloadSound(EntityUid uid, SoundOnOverloadComponent component, ref DeviceLinkOverloadedEvent args)
    {
        var audioParams = component.OverloadSound?.Params ?? AudioParams.Default;
        audioParams = audioParams.AddVolume(component.VolumeModifier);
        _audioSystem.PlayPvs(component.OverloadSound, uid, audioParams);
    }

    [SubscribeLocalEvent]
    private void OnOverloadSpawn(EntityUid uid, SpawnOnOverloadComponent component, ref DeviceLinkOverloadedEvent args)
    {
        PredictedSpawnAtPosition(component.Prototype, Transform(uid).Coordinates);
    }
}
