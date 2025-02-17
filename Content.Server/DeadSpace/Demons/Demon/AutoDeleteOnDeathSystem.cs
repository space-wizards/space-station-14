// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Demons.Demon.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Audio.Systems;

namespace Content.Server.DeadSpace.Demons.Demon;

public sealed class AutoDeleteOnDeathSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutoDeleteOnDeathComponent, MobStateChangedEvent>(OnDead);
    }

    private void OnDead(EntityUid uid, AutoDeleteOnDeathComponent component, MobStateChangedEvent args)
    {
        if (_mobState.IsDead(uid))
        {
            _audio.PlayPvs(component.DeadSound, uid);
            QueueDel(uid);
        }
    }
}
