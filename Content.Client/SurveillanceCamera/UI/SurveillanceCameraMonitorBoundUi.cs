using Content.Client.Eye;
using Content.Shared.SurveillanceCamera;
using Robust.Client.GameObjects;

namespace Content.Client.SurveillanceCamera.UI;

public sealed class SurveillanceCameraMonitorBoundUserInterface : BoundUserInterface
{
    private readonly EyeLerpingSystem _eyeLerpingSystem;
    private readonly SurveillanceCameraMonitorSystem _surveillanceCameraMonitorSystem;

    [ViewVariables]
    private SurveillanceCameraMonitorWindow? _window;

    [ViewVariables]
    private EntityUid? _currentCamera;

    public SurveillanceCameraMonitorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _eyeLerpingSystem = EntMan.System<EyeLerpingSystem>();
        _surveillanceCameraMonitorSystem = EntMan.System<SurveillanceCameraMonitorSystem>();
    }

    protected override void Open()
    {
        base.Open();

        _window = new SurveillanceCameraMonitorWindow();

        if (State != null)
        {
            UpdateState(State);
        }

        _window.OpenCentered();

        _window.CameraSelected += OnCameraSelected;
        _window.SubnetOpened += OnSubnetRequest;
        _window.CameraRefresh += OnCameraRefresh;
        _window.SubnetRefresh += OnSubnetRefresh;
        _window.OnClose += Close;
        _window.CameraSwitchTimer += OnCameraSwitchTimer;
        _window.CameraDisconnect += OnCameraDisconnect;
    }

    private void OnCameraSelected(string address)
    {
        SendMessage(new SurveillanceCameraMonitorSwitchMessage(address));
    }

    private void OnSubnetRequest(string subnet)
    {
        SendMessage(new SurveillanceCameraMonitorSubnetRequestMessage(subnet));
    }

    private void OnCameraSwitchTimer()
    {
        _surveillanceCameraMonitorSystem.AddTimer(Owner, _window!.OnSwitchTimerComplete);
    }

    private void OnCameraRefresh()
    {
        SendMessage(new SurveillanceCameraRefreshCamerasMessage());
    }

    private void OnSubnetRefresh()
    {
        SendMessage(new SurveillanceCameraRefreshSubnetsMessage());
    }

    private void OnCameraDisconnect()
    {
        SendMessage(new SurveillanceCameraDisconnectMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (_window == null || state is not SurveillanceCameraMonitorUiState cast)
        {
            return;
        }

        var active = EntMan.GetEntity(cast.ActiveCamera);

        if (active == null)
        {
            _window.UpdateState(null, cast.Subnets, cast.ActiveAddress, cast.ActiveSubnet, cast.Cameras);

            if (_currentCamera != null)
            {
                _surveillanceCameraMonitorSystem.RemoveTimer(Owner);
                _eyeLerpingSystem.RemoveEye(_currentCamera.Value);
                _currentCamera = null;
            }
        }
        else
        {
            if (_currentCamera == null)
            {
                _eyeLerpingSystem.AddEye(active.Value);
                _currentCamera = active;
            }
            else if (_currentCamera != active)
            {
                _eyeLerpingSystem.RemoveEye(_currentCamera.Value);
                _eyeLerpingSystem.AddEye(active.Value);
                _currentCamera = active;
            }

            if (EntMan.TryGetComponent<EyeComponent>(active, out var eye))
            {
                _window.UpdateState(eye.Eye, cast.Subnets, cast.ActiveAddress, cast.ActiveSubnet, cast.Cameras);
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (_currentCamera != null)
        {
            _eyeLerpingSystem.RemoveEye(_currentCamera.Value);
            _currentCamera = null;
        }

        if (disposing)
        {
            _window?.Dispose();
        }
    }
}
