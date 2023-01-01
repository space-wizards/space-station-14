using Content.Client.Eye;
using Content.Shared.SurveillanceCamera;
using Robust.Client.GameObjects;

namespace Content.Client.SurveillanceCamera.UI;

public sealed class SurveillanceCameraMonitorBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    private readonly EyeLerpingSystem _eyeLerpingSystem;
    private readonly SurveillanceCameraMonitorSystem _surveillanceCameraMonitorSystem;

    private SurveillanceCameraMonitorWindow? _window;
    private EntityUid? _currentCamera;

    public SurveillanceCameraMonitorBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
        _eyeLerpingSystem = _entityManager.EntitySysManager.GetEntitySystem<EyeLerpingSystem>();
        _surveillanceCameraMonitorSystem = _entityManager.EntitySysManager.GetEntitySystem<SurveillanceCameraMonitorSystem>();
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
        _surveillanceCameraMonitorSystem.AddTimer(Owner.Owner, _window!.OnSwitchTimerComplete);
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

        if (cast.ActiveCamera == null)
        {
            _window.UpdateState(null, cast.Subnets, cast.ActiveAddress, cast.ActiveSubnet, cast.Cameras);

            if (_currentCamera != null)
            {
                _surveillanceCameraMonitorSystem.RemoveTimer(Owner.Owner);
                _eyeLerpingSystem.RemoveEye(_currentCamera.Value);
                _currentCamera = null;
            }
        }
        else
        {
            if (_currentCamera == null)
            {
                _eyeLerpingSystem.AddEye(cast.ActiveCamera.Value);
                _currentCamera = cast.ActiveCamera;
            }
            else if (_currentCamera != cast.ActiveCamera)
            {
                _eyeLerpingSystem.RemoveEye(_currentCamera.Value);
                _eyeLerpingSystem.AddEye(cast.ActiveCamera.Value);
                _currentCamera = cast.ActiveCamera;
            }

            if (_entityManager.TryGetComponent(cast.ActiveCamera, out EyeComponent? eye))
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
