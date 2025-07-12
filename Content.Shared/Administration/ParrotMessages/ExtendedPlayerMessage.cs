using Robust.Shared.Serialization;

namespace Content.Shared.Administration.ParrotMessages;

/// <summary>
/// Player message record that includes additional player information for administration purposes
/// </summary>
[Serializable, NetSerializable]
public sealed record ExtendedPlayerMessage(
    int MessageId,
    string MessageText,
    int SourceRound,
    string SourcePlayerUserName,
    Guid SourcePlayerGuid,
    bool Blocked
);
