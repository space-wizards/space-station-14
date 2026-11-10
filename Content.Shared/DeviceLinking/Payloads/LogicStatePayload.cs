namespace Content.Shared.DeviceLinking.Payloads;

/// <summary>
/// Contains a logic state of a <see cref="SignalPayload"/>.
/// </summary>
public partial record struct LogicStatePayload : ISignalNetworkPayload
{
    /// <summary>
    /// The logic signal state carried by this payload.
    /// </summary>
    [DataField]
    public SignalState State;
}
