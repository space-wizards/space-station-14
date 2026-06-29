using Content.Shared.Chat.Prototypes;
using Content.Shared.Humanoid;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

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

    //TODO: Wilhelm scream logic needs to be more generic
    /// <summary>
    /// Emote ID for screaming (for whilhelm scream)
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public ProtoId<EmotePrototype> ScreamId = "Scream";

    /// <summary>
    /// Sound specifier for Wilhelm scream
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public SoundSpecifier Wilhelm = new SoundPathSpecifier("/Audio/Voice/Human/wilhelm_scream.ogg");

    /// <summary>
    /// Odds that screaming will be a Wilhelm scream
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public float WilhelmProbability = 0.0002f;

    /// <summary>
    /// Default Emote Action to grant
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public EntProtoId? EmoteAction = "ActionScream";

    [DataField]
    [AutoNetworkedField]
    public EntityUid? EmoteActionEntity;

    /// <summary>
    ///     Currently loaded emote sounds prototype, based on entity sex.
    ///     Null if no valid prototype for entity sex was found.
    /// </summary>
    [ViewVariables]
    [AutoNetworkedField]
    public ProtoId<EmoteSoundsPrototype>? EmoteSounds = null;
}
