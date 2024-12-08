using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Posters.Components;

/// <summary>
/// Marks component that will unfold entity (poster) on interact with wall (or another object)
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedPosterSystem))]
public sealed partial class PosterComponent : Component
{
    // Time to unfold poster
    [DataField]
    public float PlacingTime = 5.0f;

    // Time to fold poster back
    [DataField]
    public float RemovingTime = 2.0f;

    // A tag that defines the ability to stick a poster on a specific object. If it's empty, you can place poster on any object.
    [DataField, AutoNetworkedField]
    public string PlacingTag = "Wall";

    [DataField]
    public SoundSpecifier? PlacingSound = new SoundPathSpecifier("/Audio/Effects/poster_placing.ogg");

    [DataField]
    public SoundSpecifier? RemovingSound = new SoundPathSpecifier("/Audio/Effects/poster_removing.ogg");

    // Effect that will appear when on poster placing
    [DataField]
    public EntProtoId PlacingEffect { get; private set; } = "EffectPosterPlacing";

    // Entity that store effect
    [DataField]
    public EntityUid? EffectEntity;

}
