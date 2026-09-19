using Robust.Shared.Serialization;

namespace Content.Shared.Administration.ParrotMemories;

/// <summary>
/// Player message record that includes additional player information for administration purposes
/// </summary>
[Serializable, NetSerializable]
public sealed record ExtendedParrotMemory(
    int Id,
    string Text,
    int SourceRound,
    string SourcePlayerUserName,
    Guid SourcePlayerGuid,
    DateTime CreatedAt,
    bool Blocked
);
