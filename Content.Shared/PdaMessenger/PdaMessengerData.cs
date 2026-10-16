using Robust.Shared.Serialization;

namespace Content.Shared.PdaMessenger;

/// <summary>
///     A messenger user: the messenger ID of their ID card, and the name and job on it.
/// </summary>
[DataRecord, Serializable, NetSerializable]
public partial record struct PdaMessengerContact(string UserId, string Name, string JobTitle);

/// <summary>
///     A single message. <see cref="SentAt"/> is the round time it was sent at.
/// </summary>
[DataRecord, Serializable, NetSerializable]
public partial record struct PdaMessengerMessage(int Id, string SenderId, string Text, TimeSpan SentAt);

/// <summary>
///     A direct conversation between two users.
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public sealed partial class PdaMessengerConversation
{
    [DataField]
    public string Id = string.Empty;

    [DataField]
    public List<PdaMessengerContact> Participants = new();

    [DataField]
    public List<PdaMessengerMessage> Messages = new();

    /// <summary>
    ///     Drops the oldest messages beyond the history cap.
    /// </summary>
    public void TrimTo(int cap)
    {
        if (Messages.Count > cap)
            Messages.RemoveRange(0, Messages.Count - cap);
    }

    /// <summary>
    ///     Gets the ID of the conversation between two users, the same whichever of them sends first.
    /// </summary>
    public static string GetId(string userA, string userB)
    {
        return string.CompareOrdinal(userA, userB) < 0 ? $"{userA}:{userB}" : $"{userB}:{userA}";
    }
}
