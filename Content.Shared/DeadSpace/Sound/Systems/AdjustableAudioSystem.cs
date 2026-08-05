// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Audio.Components;
using Content.Shared.DeadSpace.Sound.Components;

namespace Content.Shared.DeadSpace.Sound.Systems;

public sealed class AdjustableAudioSystem : EntitySystem
{
    public void Mark((EntityUid Entity, AudioComponent Component)? stream)
    {
        if (stream == null)
            return;

        var (uid, audio) = stream.Value;
        var component = EnsureComp<ItemSoundAudioComponent>(uid);
        component.BaseVolume = audio.Params.Volume;
        Dirty(uid, component);
    }
}
