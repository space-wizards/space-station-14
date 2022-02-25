using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Sound;
using Robust.Shared.Audio;
using Robust.Shared.Utility;


namespace Content.Server.Speech.Components;

/// <summary>
///     Component required for entities to be able to scream.
/// </summary>
[RegisterComponent]
public sealed class VocalComponent : Component
{
    [DataField("maleScream")]
    public SoundSpecifier MaleScream = new SoundCollectionSpecifier("MaleScreams");

    [DataField("femaleScream")]
    public SoundSpecifier FemaleScream = new SoundCollectionSpecifier("FemaleScreams");

    [DataField("wilhelm")]
    public SoundSpecifier Wilhelm = new SoundPathSpecifier("/Audio/Voice/Human/wilhelm_scream.ogg");

    [DataField("audioParams")]
    public AudioParams AudioParams = AudioParams.Default.WithVolume(4f);

    public const float Variation = 0.125f;

    // Not using the in-build sound support for actions, given that the sound is modified non-prototype specific factors like gender.
    [DataField("action", required: true)]
    public InstantAction Action = new()
    {
        UseDelay = TimeSpan.FromSeconds(10),
        Icon = new SpriteSpecifier.Texture(new ResourcePath("Interface/Actions/scream.png")),
        Name = "action-name-scream",
        Description = "AAAAAAAAAAAAAAAAAAAAAAAAA",
        Event = new ScreamActionEvent(),
    };
}

public sealed class ScreamActionEvent : PerformActionEvent { };
