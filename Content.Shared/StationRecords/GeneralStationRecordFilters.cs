using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.StationRecords;

[Serializable, NetSerializable]
public struct StationRecordConsoleFiltersFields {
    public string fingerPrints;
}

[Serializable, NetSerializable]
public sealed class StationRecordConsoleFiltersMsg : BoundUserInterfaceMessage
{
    public string fingerPrints { get; }

    public StationRecordConsoleFiltersMsg (string _prints) {
        fingerPrints = _prints;
    }
}
