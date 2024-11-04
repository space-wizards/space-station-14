#nullable enable
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._Starlight.MHelp;

[Serializable, NetSerializable]
public sealed class MHelpTypingUpdated() : EntityEventArgs
{
    public required Guid Ticket { get; init; }
    public required string PlayerName { get; init; }
    public required bool Typing { get; init; }
}
