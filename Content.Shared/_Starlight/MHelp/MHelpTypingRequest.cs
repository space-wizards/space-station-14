#nullable enable
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Starlight.MHelp;

/// <summary>
///     Sent by the client to notify the server when it begins or stops typing.
/// </summary>
[Serializable, NetSerializable]
public sealed class MHelpTypingRequest() : EntityEventArgs
{
    public required Guid Ticket { get; init; }
    public required bool Typing { get; init; }
}
