using Content.Server.DeviceLinking.Components;
using Content.Server.DeviceLinking.Components.Overload;
using Content.Server.DeviceLinking.Events;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;

namespace Content.Server.DeviceLinking.Systems;

public sealed class DeviceLinkOverloadSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audioSystem = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<SoundOnOverloadComponent, DeviceLinkOverloadedEvent>(OnOverloadSound);
        SubscribeLocalEvent<SpawnOnOverloadComponent, DeviceLinkOverloadedEvent>(OnOverloadSpawn);
    }

    private void OnOverloadSound(EntityUid uid, SoundOnOverloadComponent component, ref DeviceLinkOverloadedEvent args)
    {

        _audioSystem.PlayPvs(component.OverloadSound, uid, AudioParams.Default.WithVolume(component.VolumeModifier));
    }


    private void OnOverloadSpawn(EntityUid uid, SpawnOnOverloadComponent component, ref DeviceLinkOverloadedEvent args)
    {
        Spawn(component.Prototype, Transform(uid).Coordinates);
    }
}
