namespace Content.Shared.DeviceLinking;

/// <summary>
/// Contains a logic state of a <see cref="SignalPayload"/>.
/// </summary>
public partial record struct LogicStatePayload : ISignalNetworkPayload
{
    [DataField]
    public SignalState State;
}
