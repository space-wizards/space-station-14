using Content.Shared.Chat.Prototypes;
using Content.Shared.Humanoid;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Server.Speech.Components;

/// <summary>
///     Component required for entities to be able to do vocal emotions.
/// </summary>
[RegisterComponent]
public sealed class VocalComponent : Component
{
    [DataField("screamId", customTypeSerializer: typeof(PrototypeIdSerializer<EmotePrototype>))]
    public string ScreamId = "Scream";

    [DataField("wilhelm")]
    public SoundSpecifier Wilhelm = new SoundPathSpecifier("/Audio/Voice/Human/wilhelm_scream.ogg");

    [DataField("wilhelmProbability")]
    public float WilhelmProbability = 0.01f;

    [DataField("sounds", customTypeSerializer: typeof(PrototypeIdValueDictionarySerializer<Sex, EmoteSoundsPrototype>))]
    public Dictionary<Sex, string>? Sounds;

    [ViewVariables]
    public EmoteSoundsPrototype? EmoteSounds = null;
}
