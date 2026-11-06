using Content.Shared.DeviceNetwork;

namespace Content.Shared.PdaMessenger;

/// <summary>
///     Sent by a PDA to the messenger server every few seconds, to register its user.
/// </summary>
public partial record struct PdaMessengerRegisterPayload : INetworkPayload
{
    [DataField]
    public PdaMessengerContact User;
}

/// <summary>
///     The server's reply to <see cref="PdaMessengerRegisterPayload"/>: everyone who is online.
/// </summary>
public partial record struct PdaMessengerContactsPayload : INetworkPayload
{
    [DataField]
    public List<PdaMessengerContact> Contacts;
}

/// <summary>
///     Sent by a PDA to the server to send a message.
/// </summary>
public partial record struct PdaMessengerSendPayload : INetworkPayload
{
    [DataField]
    public string SenderId;

    [DataField]
    public string RecipientId;

    [DataField]
    public string Text;
}

/// <summary>
///     Sent by the server to both users' PDAs when a message is stored. The sender's copy confirms it was sent.
/// </summary>
public partial record struct PdaMessengerDeliverPayload : INetworkPayload
{
    [DataField]
    public string RecipientId;

    [DataField]
    public string ConversationId;

    [DataField]
    public List<PdaMessengerContact> Participants;

    [DataField]
    public PdaMessengerMessage Message;
}

/// <summary>
///     Sent by a PDA to the server when an ID card is inserted, to get that user's history.
/// </summary>
public partial record struct PdaMessengerHistoryRequestPayload : INetworkPayload
{
    [DataField]
    public string UserId;
}

/// <summary>
///     The server's reply to <see cref="PdaMessengerHistoryRequestPayload"/>.
/// </summary>
public partial record struct PdaMessengerHistoryPayload : INetworkPayload
{
    [DataField]
    public string UserId;

    [DataField]
    public List<PdaMessengerConversation> Conversations;
}
