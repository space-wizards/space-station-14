using Content.Shared.PdaMessenger;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.PdaMessenger;

/// <summary>
///     The messenger program on a PDA.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause, Access(typeof(PdaMessengerCartridgeSystem))]
public sealed partial class PdaMessengerCartridgeComponent : Component
{
    /// <summary>
    ///     How often the PDA registers with the messenger server.
    /// </summary>
    [DataField]
    public TimeSpan RegisterInterval = TimeSpan.FromSeconds(2);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextRegister;

    /// <summary>
    ///     The inserted ID card's user, as of the last registration, or null if there's none.
    /// </summary>
    [ViewVariables]
    public PdaMessengerContact? User;

    /// <summary>
    ///     The address of the server that answered the last registration, or null if none did.
    /// </summary>
    [ViewVariables]
    public string? Server;

    [ViewVariables]
    public List<PdaMessengerContact> Contacts = new();

    /// <summary>
    ///     The local copy of each ID card's conversations, by user ID, then conversation ID.
    ///     Only the inserted card's conversations are shown.
    /// </summary>
    [ViewVariables]
    public Dictionary<string, Dictionary<string, PdaMessengerConversation>> History = new();

    /// <summary>
    ///     How many messages the user hasn't read yet, by conversation ID.
    /// </summary>
    [ViewVariables]
    public Dictionary<string, int> Unread = new();

    [ViewVariables]
    public string? OpenConversation;

    /// <summary>
    ///     Why the holder can't send, as last shown in the UI.
    /// </summary>
    [ViewVariables]
    public LocId? SendBlocked;
}
