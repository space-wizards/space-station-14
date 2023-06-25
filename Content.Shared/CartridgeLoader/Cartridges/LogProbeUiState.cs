using Content.Shared.Access.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class LogProbeUiState : BoundUserInterfaceState
{
    /// <summary>
    /// The list of probed network devices
    /// </summary>
    public List<PulledAccessLog> PulledLogs;

    public LogProbeUiState(List<PulledAccessLog> pulledLogs)
    {
        PulledLogs = pulledLogs;
    }
}

[Serializable, NetSerializable, DataRecord]
public sealed class PulledAccessLog
{
    public readonly string Time;
    public readonly string Accessor;

    public PulledAccessLog(string time, string accessor)
    {
        Time = time;
        Accessor = accessor;
    }
}
