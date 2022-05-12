using Content.Shared.SurveillanceCamera;
using Robust.Client.GameObjects;

namespace Content.Client.SurveillanceCamera.UI;

public sealed class SurveillanceCameraMonitorBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    private SurveillanceCameraMonitorWindow? _window;
    private EntityUid? _currentCamera;
    private string _currentSubnet = default!;
    private readonly HashSet<string> _knownAddresses = new();

    public SurveillanceCameraMonitorBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
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
        _window.OnClose += Close;
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

        if (_currentSubnet != cast.ActiveSubnet)
        {
            _knownAddresses.Clear();
            _currentSubnet = cast.ActiveSubnet;
        }

        if (cast.ActiveCamera == null)
        {
            _window.UpdateState(null, cast.Subnets, _currentSubnet);
        }
        else
        {
            if (_entityManager.TryGetComponent(cast.ActiveCamera, out EyeComponent eye))
            {
                _window.UpdateState(eye.Eye, cast.Subnets, _currentSubnet);
            }
        }

    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        switch (message)
        {
            case SurveillanceCameraMonitorInfoMessage infoMessage:
                if (infoMessage.Info.Subnet != _currentSubnet)
                {
                    return;
                }

                if (_knownAddresses.Contains(infoMessage.Info.Address))
                {
                    return;
                }

                _knownAddresses.Add(infoMessage.Info.Address);

                _window!.AddCameraToList(infoMessage.Info.Name, infoMessage.Info.Address);
                break;
        }
    }

    /*
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _window?.Dispose();
        }
    }
    */
}
