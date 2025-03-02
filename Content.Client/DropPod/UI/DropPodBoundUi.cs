using Content.Shared.DropPod;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.DropPod.UI;

[UsedImplicitly]
public sealed class DropPodBoundUi : BoundUserInterface
{
    [ViewVariables]
    private DropPodConsoleWindow? _window;

    public DropPodBoundUi(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new DropPodConsoleWindow();
        _window.OpenCentered();

        _window.PointsRefreshButtonPressed += OnPointsRefreshButtonPressed;
        _window.StartLandingButtonPressed += OnStartLandingButtonPressed;
        _window.PointSelected += OnPointSelected;
        _window.OnClose += Close;
    }

    private void OnPointsRefreshButtonPressed()
    {
        SendMessage(new DropPodRefreshMessage());
    }

    private void OnStartLandingButtonPressed()
    {
        SendMessage(new DropPodStartMessage());
    }

    private void OnPointSelected(int point)
    {
        SendMessage(new DropPodPointSelectedMessage(point));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null || state is not DropPodUiState cast)
            return;

        _window.UpdateState(cast);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _window?.Dispose();
    }
}
