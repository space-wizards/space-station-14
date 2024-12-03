using System;
using System.Collections.Generic;
using System.Text;
using Content.Shared.Interaction.Events;
using Robust.Shared.Audio;
using Content.Shared.Sound.Components;

namespace Content.Server.Sound;

[RegisterComponent]
public sealed partial class PlaySoundOnUseComponent : Component
{
    [DataField(required: true)]
    public SoundSpecifier? Sound { get; set; }
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float Volume { get; set; } = -2f;
}
