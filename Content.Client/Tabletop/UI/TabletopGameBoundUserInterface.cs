using Content.Client.Tabletop.UI;
using Content.Shared.Tabletop.Components;
using Robust.Client.UserInterface;

namespace Content.Client.SurveillanceCamera.UI;

/// <summary>
/// A bound UI for tabletop games.
/// Sets up the viewer eye and handles rotation, and that's about it!
/// </summary>
public sealed class TabletopGameBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private TabletopWindow? _window;

    [ViewVariables]
    private EntityUid? _camera;

    public TabletopGameBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<TabletopWindow>();
        if (EntMan.TryGetComponent<TabletopGameComponent>(Owner, out var tabletop))
        {
            if (EntMan.TryGetComponent<EyeComponent>(tabletop.UprightCamera, out var eye))
            {
                _camera = tabletop.UprightCamera;
                _window.SetPosition(eye.Eye, tabletop.Size);
            }

            _window.SetBoard(tabletop.Board);
        }
    }
}
