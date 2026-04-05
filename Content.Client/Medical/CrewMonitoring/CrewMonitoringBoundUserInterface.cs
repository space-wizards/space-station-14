using Content.Shared.Medical.CrewMonitoring;
using Content.Shared.Silicons.StationAi;
using Robust.Client.UserInterface;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Client.Medical.CrewMonitoring;

public sealed class CrewMonitoringBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;

    [ViewVariables]
    private CrewMonitoringWindow? _menu;

    public CrewMonitoringBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        if (_menu != null)
            _menu.MapClicked -= OnMapClicked;

        EntityUid? gridUid = null;
        var stationName = string.Empty;

        if (EntMan.TryGetComponent<TransformComponent>(Owner, out var xform))
        {
            gridUid = xform.GridUid;

            if (EntMan.TryGetComponent<MetaDataComponent>(gridUid, out var metaData))
            {
                stationName = metaData.EntityName;
            }
        }

        _menu = this.CreateWindow<CrewMonitoringWindow>();
        _menu.Set(stationName, gridUid);
        _menu.MapClicked += OnMapClicked;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        switch (state)
        {
            case CrewMonitoringState st:
                EntMan.TryGetComponent<TransformComponent>(Owner, out var xform);
                _menu?.ShowSensors(st.Sensors, Owner, xform?.Coordinates);
                break;
        }
    }

    private void OnMapClicked(EntityCoordinates coordinates)
    {
        var local = _playerManager.LocalEntity;

        if (local is null || !EntMan.HasComponent<StationAiHeldComponent>(local.Value))
            return;

        var netCoordinates = EntMan.GetNetCoordinates(coordinates);
        SendMessage(new CrewMonitoringWarpRequestMessage(netCoordinates));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_menu != null)
            {
                _menu.MapClicked -= OnMapClicked;
                _menu = null;
            }
        }

        base.Dispose(disposing);
    }
}
