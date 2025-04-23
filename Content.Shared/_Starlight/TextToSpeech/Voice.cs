using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;

namespace Content.Shared.Starlight.TextToSpeech;
/// <summary>
/// Prototype represent TTS voices
/// </summary>
[Prototype("voice")]
public sealed class VoicePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("voice")]
    public int Voice { get; }

    [DataField("name")]
    public string Name { get; } = string.Empty;

    [DataField("sex", required: true)]
    public Sex Sex { get; } = default!;

    [DataField("silicon")]
    public bool Silicon { get; } = false;
}
