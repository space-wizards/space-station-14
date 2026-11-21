using Content.Shared.PdaMessenger;

namespace Content.Server.PdaMessenger;

/// <summary>
///     PDA messenger server. Relays messages between PDAs and keeps the message history.
/// </summary>
[RegisterComponent, Access(typeof(PdaMessengerServerSystem))]
public sealed partial class PdaMessengerServerComponent : Component
{
    /// <summary>
    ///     User IDs with registered PDAs.
    /// </summary>
    [ViewVariables]
    public Dictionary<string, PdaMessengerDirectoryEntry> Directory = new();

    /// <summary>
    ///     Stored conversations, by conversation ID.
    /// </summary>
    [ViewVariables]
    public Dictionary<string, PdaMessengerConversation> Conversations = new();

    /// <summary>
    ///     The IDs of each user's conversations, by user ID.
    /// </summary>
    [ViewVariables]
    public Dictionary<string, HashSet<string>> UserConversations = new();

    /// <summary>
    ///     How long a user stays online after their PDA last registered.
    /// </summary>
    [DataField]
    public TimeSpan RegistrationTimeout = TimeSpan.FromSeconds(10);
}

/// <summary>
///     A registered user and the address of the PDA they registered from.
/// </summary>
public readonly record struct PdaMessengerDirectoryEntry(string Address, PdaMessengerContact Contact, TimeSpan LastSeen);
