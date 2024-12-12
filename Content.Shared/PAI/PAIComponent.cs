using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Containers;
using Robust.Shared.Audio;
using Content.Shared.Tools;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.PAI;

/// <summary>
/// pAIs, or Personal AIs, are essentially portable ghost role generators.
/// In their current implementation in SS14, they create a ghost role anyone can access,
/// and that a player can also "wipe" (reset/kick out player).
/// Theoretically speaking pAIs are supposed to use a dedicated "offer and select" system,
///  with the player holding the pAI being able to choose one of the ghosts in the round.
/// This seems too complicated for an initial implementation, though,
///  and there's not always enough players and ghost roles to justify it.
/// All logic in PAISystem.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PAIComponent : Component
{
    /// <summary>
    /// The last person who activated this PAI.
    /// Used for assigning the name.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? LastUser;

    [DataField(serverOnly: true)]
    public EntProtoId? MidiActionId = "ActionPAIPlayMidi";

    [DataField(serverOnly: true)] // server only, as it uses a server-BUI event !type
    public EntityUid? MidiAction;

    [DataField]
    public EntProtoId MapActionId = "ActionPAIOpenMap";

    [DataField, AutoNetworkedField]
    public EntityUid? MapAction;

    /// <summary>
    /// When microwaved there is this chance to brick the pai, kicking out its player and preventing it from being used again.
    /// </summary>
    [DataField]
    public float BrickChance = 0.5f;

    /// <summary>
    /// Locale id for the popup shown when the pai gets bricked.
    /// </summary>
    [DataField]
    public string BrickPopup = "pai-system-brick-popup";

    /// <summary>
    /// Locale id for the popup shown when the pai is microwaved but does not get bricked.
    /// </summary>
    [DataField]
    public string ScramblePopup = "pai-system-scramble-popup";

    /// <summary>
    /// gives it that satisfying click sound when insert and remove encryption keys
    /// side note: a lot of this is just yoinked from EncryptionKeyHolderComponents.cs, bless that person
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("keysUnlocked")]
    public bool KeysUnlocked = true;

    /// <summary>
    ///     The tool required to extract the encryption keys from the headset.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("keysExtractionMethod", customTypeSerializer: typeof(PrototypeIdSerializer<ToolQualityPrototype>))]
    public string KeysExtractionMethod = "Screwing";

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("keySlots")]
    public int KeySlots = 2;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("keyExtractionSound")]
    public SoundSpecifier KeyExtractionSound = new SoundPathSpecifier("/Audio/Items/pistol_magout.ogg");

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("keyInsertionSound")]
    public SoundSpecifier KeyInsertionSound = new SoundPathSpecifier("/Audio/Items/pistol_magin.ogg");

    [ViewVariables]
    public Container KeyContainer = default!;
    public const string KeyContainerName = "key_slots";

    /// <summary>
    ///     Combined set of radio channels provided by all contained keys.
    /// </summary>
    [ViewVariables]
    public HashSet<string> Channels = new();

    /// <summary>
    ///     This is the channel that will be used when using the default/department prefix (<see cref="SharedChatSystem.DefaultChannelKey"/>).
    /// </summary>
    [ViewVariables]
    public string? DefaultChannel;
}
