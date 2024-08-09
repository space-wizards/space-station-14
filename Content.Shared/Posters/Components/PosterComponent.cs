using Robust.Shared.Audio;
using System.Threading;

namespace Content.Shared.Posters.Components;

[RegisterComponent]
public sealed partial class PosterComponent : Component
{
    public CancellationTokenSource? CancelToken;

    [DataField("foldedPrototype")]
    public string? FoldedPrototype;

    [DataField("removingTime")]
    public float RemovingTime = 2.0f;

    [DataField("removingSound")]
    public SoundSpecifier? RemovingSound = new SoundPathSpecifier("/Audio/Effects/poster_removing.ogg");
}
