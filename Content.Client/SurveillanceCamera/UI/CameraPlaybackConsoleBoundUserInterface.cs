using Content.Shared.SurveillanceCamera;

namespace Content.Client.SurveillanceCamera.UI;

public sealed class CameraPlaybackConsoleBoundUserInterface : BoundUserInterface
{
    private CameraPlaybackWindow? _window;

    public CameraPlaybackConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new CameraPlaybackWindow(Owner);
        _window.OnTargetChanged += target => SendMessage(new CameraPlaybackTargetRequestMessage(target));
        _window.OnClose += Close;
        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not CameraPlaybackConsoleState cast)
            return;

        _window?.UpdateState(cast);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _window?.Dispose();
    }
}
