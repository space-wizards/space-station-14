using Content.Client.Starlight.Medical.Surgery;
using Content.Client.Administration.UI.CustomControls;
using Content.Client.Hands.Systems;
using Content.Shared.Starlight.Antags.Abductor;
using Content.Shared.Starlight.Medical.Surgery;
using Content.Shared.Body.Part;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using static Content.Shared.Pinpointer.SharedNavMapSystem;
using static Robust.Client.UserInterface.Control;

namespace Content.Client.Starlight.Antags.Abductor;

[UsedImplicitly]
public sealed class AbductorCameraConsoleBui : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entities = default!;

    [ViewVariables]
    private AbductorCameraConsoleWindow? _window;
    private int? _station;
    public AbductorCameraConsoleBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }
    protected override void Open() => UpdateState(State);
    protected override void UpdateState(BoundUserInterfaceState? state)
    {
        if (state is AbductorCameraConsoleBuiState s)
            Update(s);
    }

    private void Update(AbductorCameraConsoleBuiState state)
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
        _window = new AbductorCameraConsoleWindow();
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
                SendMessage(new AbductorBeaconChosenBuiMsg()
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
        if (_window == null || State is not AbductorCameraConsoleBuiState state)
            return;

        _window!.Stations.DisposeAllChildren();
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

        _window.Title = State is not AbductorCameraConsoleBuiState state
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
