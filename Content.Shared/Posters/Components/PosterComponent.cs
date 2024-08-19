using Robust.Shared.Audio;
using System.Threading;

namespace Content.Shared.Posters.Components;

[RegisterComponent]
public sealed partial class PosterComponent : Component
{
    [DataField]
    public string? FoldedPrototype;

    [DataField]
    public float RemovingTime = 2.0f;

    [DataField]
    public SoundSpecifier? RemovingSound = new SoundPathSpecifier("/Audio/Effects/poster_removing.ogg");
}
