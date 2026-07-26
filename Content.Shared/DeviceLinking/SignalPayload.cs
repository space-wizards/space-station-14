using Content.Shared.DeviceLinking.Events;
using Content.Shared.DeviceNetwork;

namespace Content.Shared.DeviceLinking;

/// <summary>
/// A network payload that raises <see cref="SignalReceivedEvent"/> when successfully delivered.
/// Optionally contains a <see cref="INetworkPayload"/> to provide additional data for the event.
/// </summary>
public sealed partial class SignalPayload : NetworkPayloadBase<SignalPayload>
{
    /// <summary>
    /// A signal port that was invoked.
    /// </summary>
    [DataField]
    public string InvokedPort;

    /// <summary>
    /// Optional additional data about the signal.
    /// </summary>
    [DataField]
    public INetworkPayload? Payload;
}
