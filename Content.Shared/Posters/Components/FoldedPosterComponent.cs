using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using System.Threading;

namespace Content.Shared.Posters.Components;

[RegisterComponent]
public sealed partial class FoldedPosterComponent : Component
{
    public CancellationTokenSource? CancelToken;

    [DataField("posterPrototype")]
    public string? PosterPrototype;

    [DataField("PlacingEffect")]
    public EntProtoId PlacingEffect { get; private set; } = "EffectPosterPlacing";

    [DataField("placingSound")]
    public SoundSpecifier? PlacingSound = new SoundPathSpecifier("/Audio/Effects/poster_placing.ogg");

    [DataField("placingTime")]
    public float PlacingTime = 5.0f;
}

