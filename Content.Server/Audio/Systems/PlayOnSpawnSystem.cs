using Content.Server.Audio.Components;
using Robust.Shared.Player;

namespace Content.Server.Audio.Systems;

public sealed class PlayOnSpawnSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayOnSpawnComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, PlayOnSpawnComponent component, ComponentInit args)
    {
        if (component.Sound != null)
        {
            _audioSystem.Play(component.Sound, Filter.Pvs(uid), uid);
        }
    }
}
