using Content.Shared.Chat.Prototypes;
using Content.Shared.Humanoid;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.Speech.Components;

/// <summary>
///     Component required for entities to be able to do vocal emotions.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class VocalComponent : Component
{
    /// <summary>
    ///     Emote sounds prototype id for each sex (not gender).
    ///     Entities without <see cref="HumanoidComponent"/> considered to be <see cref="Sex.Unsexed"/>.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public Dictionary<Sex, ProtoId<EmoteSoundsPrototype>>? Sounds;

    [DataField("screamId", customTypeSerializer: typeof(PrototypeIdSerializer<EmotePrototype>))]
    [AutoNetworkedField]
    public string ScreamId = "Scream";

    [DataField("wilhelm")]
    [AutoNetworkedField]
    public SoundSpecifier Wilhelm = new SoundPathSpecifier("/Audio/Voice/Human/wilhelm_scream.ogg");

    [DataField("wilhelmProbability")]
    [AutoNetworkedField]
    public float WilhelmProbability = 0.0002f;

    [DataField("screamAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    [AutoNetworkedField]
    public string? ScreamAction = "ActionScream";

    [DataField("screamActionEntity")]
    [AutoNetworkedField]
    public EntityUid? ScreamActionEntity;

    /// <summary>
    ///     Currently loaded emote sounds prototype, based on entity sex.
    ///     Null if no valid prototype for entity sex was found.
    /// </summary>
    [ViewVariables]
    [AutoNetworkedField]
    public ProtoId<EmoteSoundsPrototype>? EmoteSounds = null;

    //starlight start
    [ViewVariables(VVAccess.ReadOnly)]
    [AutoNetworkedField]
    //have to use string as for some reason emote prototypes are not serializable even though im telling it not to serialize
    public Dictionary<string, TimeSpan> LastEmoteTime = new Dictionary<string, TimeSpan>();

    [AutoNetworkedField]
    [DataField]
    public TimeSpan EmoteCooldown = TimeSpan.FromSeconds(1.5f);
    //starlight end
}
