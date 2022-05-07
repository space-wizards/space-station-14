using Content.Shared.SurveillanceCamera;
using Robust.Client.GameObjects;

namespace Content.Client.SurveillanceCamera.UI;

public sealed class SurveillanceCameraMonitorBoundUi : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    private SurveillanceCameraMonitorWindow? _window;
    private EntityUid? _currentCamera;

    public SurveillanceCameraMonitorBoundUi(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        _window = new SurveillanceCameraMonitorWindow();

        _window.CameraSelected += OnCameraSelected;
        _window.SubnetOpened += OnSubnetRequest;
    }

    private void OnCameraSelected(string address)
    {
        SendMessage(new SurveillanceCameraMonitorSwitchMessage(address));
    }

    private void OnSubnetRequest(string subnet)
    {
        SendMessage(new SurveillanceCameraMonitorSubnetRequestMessage(subnet));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (_window == null || state is not SurveillanceCameraMonitorUiState cast)
        {
            return;
        }

        if (cast.ActiveCamera == _currentCamera)
        {
            return;
        }

        _currentCamera = cast.ActiveCamera;

        if (cast.ActiveCamera == null)
        {
            _window.UpdateState(null, cast.Subnets);
        }
        else
        {
            if (_entityManager.TryGetComponent(cast.ActiveCamera, out EyeComponent eye))
            {
                _window.UpdateState(eye.Eye, cast.Subnets);
            }
        }

    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _window?.Dispose();
        }
    }
}
