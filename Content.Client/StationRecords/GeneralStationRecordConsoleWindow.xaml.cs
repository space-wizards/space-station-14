using Content.Shared.StationRecords;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client.StationRecords;

public sealed class GeneralStationRecordConsoleWindow : DefaultWindow
{
    public Action<StationRecordKey?>? OnKeySelected;

    public void UpdateState(GeneralStationRecordConsoleState state)
    {}
}
