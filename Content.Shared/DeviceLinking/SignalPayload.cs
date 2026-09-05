using Content.Shared.DeviceLinking.Events;
using Content.Shared.DeviceNetwork;

namespace Content.Shared.DeviceLinking;

/// <summary>
/// A network payload that can be nested inside <see cref="SignalPayload{T}"/>.
/// </summary>
public partial interface ISignalNetworkPayload : INetworkPayload;

/// <summary>
/// A network payload that raises <see cref="SignalReceivedEvent"/> when successfully delivered.
/// Optionally contains a <see cref="INetworkPayload"/> to provide additional data for the event.
/// </summary>
public partial record struct SignalPayload : INetworkPayload
{
    /// <summary>
    /// A signal port that was invoked.
    /// </summary>
    [DataField]
    public string InvokedPort;
}

/// <summary>
/// A network payload that raises <see cref="SignalReceivedEvent"/> when successfully delivered.
/// Optionally contains a <see cref="INetworkPayload"/> to provide additional data for the event.
/// </summary>
public partial record struct SignalPayload<T> : INetworkPayload where T : ISignalNetworkPayload
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
    public T Payload;
}
