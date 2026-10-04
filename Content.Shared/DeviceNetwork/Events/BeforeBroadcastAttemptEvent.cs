namespace Content.Shared.DeviceNetwork.Events;

/// <summary>
/// Sent to the sending entity before broadcasting network packets to recipients
/// </summary>
[ByRefEvent]
public record struct BeforeBroadcastAttemptEvent
{
    public readonly IReadOnlySet<Device> Recipients;

    public HashSet<Device>? ModifiedRecipients;

    public bool Cancelled = false;

    public BeforeBroadcastAttemptEvent(IReadOnlySet<Device> recipients)
    {
        Recipients = recipients;
    }
}
