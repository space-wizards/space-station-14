using Content.Client._Starlight.Antags.Abductor;
using Content.Shared._Starlight.Computers.RemoteEye;
using Content.Shared.Starlight.Antags.Abductor;
using JetBrains.Annotations;
using static Content.Shared.Pinpointer.SharedNavMapSystem;

namespace Content.Client._Starlight.Computers.RemoteEye;

[UsedImplicitly]
public sealed class RemoteEyeConsoleBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private RemoteEyeConsoleWindow? _window;
    private int? _station;

    protected override void Open()
    {
        base.Open();
        UpdateState(State);
    }

    protected override void UpdateState(BoundUserInterfaceState? state)
    {
        if (state is RemoteEyeConsoleBuiState s)
            Update(s);
    }

    private void Update(RemoteEyeConsoleBuiState _)
    {
        TryInitWindow();

        View(ViewType.Stations);

        RefreshUI();

        if (!_window!.IsOpen)
            _window.OpenCentered();
    }

    private void TryInitWindow()
    {
        if (_window != null) return;
        _window = new RemoteEyeConsoleWindow();
        _window.OnClose += Close;
        _window.Title = "Intercepted cameras.";

        _window.StationsButton.OnPressed += _ =>
        {
            _station = null;
            View(ViewType.Stations);
        };
    }

    private void OnStationPressed(int station, List<NavMapBeacon> beacons)
    {
        if (_window == null)
            return;

        _station = station;

        foreach (var beacon in beacons)
        {
            var beaconButton = new ChoiceControl();

            beaconButton.Set(beacon.Text, null);
            beaconButton.Button.Modulate = beacon.Color;
            beaconButton.Button.OnPressed += _ =>
            {
                SendMessage(new BeaconChosenBuiMsg()
                {
                    Beacon = beacon,
                });
                Close();
            };
            _window.Beacons.AddChild(beaconButton);
        }
        View(ViewType.Beacons);
    }

    private void RefreshUI()
    {
        if (_window == null || State is not RemoteEyeConsoleBuiState state)
            return; 

        _window.Separator.Color = state.Color;
        _window.Stations.DisposeAllChildren();
        _window.Beacons.DisposeAllChildren();

        foreach (var station in state.Stations)
        {
            var stationButton = new ChoiceControl();

            stationButton.Set(station.Value.Name, null);
            stationButton.Button.OnPressed += _ => OnStationPressed(station.Key, station.Value.Beacons);

            _window.Stations.AddChild(stationButton);

            if (station.Key == _station) OnStationPressed(station.Key, station.Value.Beacons);
        }
    }

    private void View(ViewType type)
    {
        if (_window == null)
            return;

        _window.StationsButton.Parent!.Margin = new Thickness(0, 0, 0, 10);

        _window.Stations.Visible = type == ViewType.Stations;
        _window.StationsButton.Visible = true;

        _window.Beacons.Visible = type == ViewType.Beacons;
        _window.BeaconsButton.Disabled = type != ViewType.Beacons;

        _window.Title = State is not RemoteEyeConsoleBuiState state
            || _station == null
            || !state.Stations.TryGetValue(_station.Value, out var station)
            ? "Stations"
            : $"Station - {station.Name}";
    }

    private enum ViewType
    {
        Stations,
        Beacons,
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _window?.Dispose();
    }
}
