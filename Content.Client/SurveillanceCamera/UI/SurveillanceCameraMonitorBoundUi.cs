using Content.Shared.SurveillanceCamera;
using Robust.Client.GameObjects;

namespace Content.Client.SurveillanceCamera.UI;

public sealed class SurveillanceCameraMonitorBoundUi : BoundUserInterface
{
    private SurveillanceCameraMonitorWindow? _window;
    public SurveillanceCameraMonitorBoundUi(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new SurveillanceCameraMonitorWindow();

        _window.CameraSelected += OnCameraSelected;
    }

    private void OnCameraSelected(string address)
    {
        SendMessage(new SurveillanceCameraMonitorSwitchMessage(address));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
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
