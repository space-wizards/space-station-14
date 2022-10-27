using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

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

    [DataField("unsexedScream")]
    public SoundSpecifier UnsexedScream = new SoundCollectionSpecifier("MaleScreams");

    [DataField("wilhelm")]
    public SoundSpecifier Wilhelm = new SoundPathSpecifier("/Audio/Voice/Human/wilhelm_scream.ogg");

    [DataField("audioParams")]
    public AudioParams AudioParams = AudioParams.Default.WithVolume(4f);

    [DataField("wilhelmProbability")]
    public float WilhelmProbability = 0.01f;

    public const float Variation = 0.125f;

    [DataField("actionId", customTypeSerializer:typeof(PrototypeIdSerializer<InstantActionPrototype>))]
    public string ActionId = "Scream";

    [DataField("action")] // must be a data-field to properly save cooldown when saving game state.
    public InstantAction? ScreamAction = null;
}

public sealed class ScreamActionEvent : InstantActionEvent { };
