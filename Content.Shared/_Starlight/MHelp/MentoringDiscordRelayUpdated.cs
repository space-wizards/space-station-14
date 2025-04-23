#nullable enable
using Robust.Shared.Serialization;

namespace Content.Shared.Starlight.MHelp;

/// <summary>
///     Sent by the server to notify all clients when the webhook url is sent.
///     The webhook url itself is not and should not be sent.
/// </summary>
[Serializable, NetSerializable]
public sealed class MentoringDiscordRelayUpdated(bool enabled) : EntityEventArgs
{
    public bool DiscordRelayEnabled { get; } = enabled;
}
