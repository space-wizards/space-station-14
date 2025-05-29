using Content.Shared.DeviceNetwork.Components;

namespace Content.Shared.DeviceNetwork.Events;

/// <summary>
/// Sent to the sending entity before broadcasting network packets to recipients
/// </summary>
public sealed class BeforeBroadcastAttemptEvent : CancellableEntityEventArgs
{
    public readonly IReadOnlySet<DeviceNetworkComponent> Recipients;
    public HashSet<DeviceNetworkComponent>? ModifiedRecipients;

    public BeforeBroadcastAttemptEvent(IReadOnlySet<DeviceNetworkComponent> recipients)
    {
        Recipients = recipients;
    }
}
