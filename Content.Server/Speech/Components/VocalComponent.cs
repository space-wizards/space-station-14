using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Humanoid;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Server.Speech.Components;

/// <summary>
///     Component required for entities to be able to scream.
/// </summary>
[RegisterComponent]
public sealed class VocalComponent : Component
{
    [DataField("sounds", customTypeSerializer: typeof(PrototypeIdValueDictionarySerializer<Sex, EmoteSoundsPrototype>))]
    public Dictionary<Sex, string>? SoundsBySex;

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

    [ViewVariables]
    public EmoteSoundsPrototype? EmoteSounds = null;
}

public sealed class ScreamActionEvent : InstantActionEvent { };
