using System;
using System.Collections.Generic;
using System.Text;
using Content.Shared.Sound.Components;
using Content.Shared.Interaction.Events;
using Robust.Shared.GameStates;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;

namespace Content.Server.Sound;

public sealed class PlaySoundOnUseSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<PlaySoundOnUseComponent, UseInHandEvent>(OnUseInHand);
    }
    private void OnUseInHand(Entity<PlaySoundOnUseComponent> ent, ref UseInHandEvent args)
    {
        var audioParams = AudioParams.Default.WithVolume(ent.Comp.Volume);
        _audio.PlayPvs(ent.Comp.Sound, ent.Owner);
    }

}
