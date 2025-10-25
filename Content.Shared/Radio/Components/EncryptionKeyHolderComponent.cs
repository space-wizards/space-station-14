using Content.Shared.Chat;
using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Radio.Components;

/// <summary>
///     This component is by entities that can contain encryption keys
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EncryptionKeyHolderComponent : Component
{
    /// <summary>
    ///     Whether or not encryption keys can be removed from the headset.
    /// </summary>
    [DataField]
    public bool KeysUnlocked = true;

    /// <summary>
    ///     The tool required to extract the encryption keys from the headset.
    /// </summary>
    [DataField]
    public ProtoId<ToolQualityPrototype> KeysExtractionMethod = "Screwing";

    [DataField]
    public int KeySlots = 2;

    [DataField]
    public SoundSpecifier KeyExtractionSound = new SoundPathSpecifier("/Audio/Items/pistol_magout.ogg");

    [DataField]
    public SoundSpecifier KeyInsertionSound = new SoundPathSpecifier("/Audio/Items/pistol_magin.ogg");

    [ViewVariables]
    public Container KeyContainer = default!;
    public const string KeyContainerName = "key_slots";

    /// <summary>
    ///     Combined set of radio channels provided by all contained keys.
    /// </summary>
    [ViewVariables]
    public HashSet<ProtoId<RadioChannelPrototype>> Channels = new();

    /// <summary>
    ///     This is the channel that will be used when using the default/department prefix (<see cref="SharedChatSystem.DefaultChannelKey"/>).
    /// </summary>
    [ViewVariables]
    public string? DefaultChannel;
}
