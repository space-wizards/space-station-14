using Content.Client.Decals.UI;
using Content.Shared.Decals;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Prototypes;

namespace Content.Client.UserInterface.Systems.DecalPlacer;

public sealed class DecalPlacerUIController : UIController
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private DecalPlacerWindow? _window;

    public void ToggleWindow()
    {
        if (_window == null)
        {
            _window = new DecalPlacerWindow();
            _window.OpenToLeft();
        }
        else if (_window.IsOpen)
        {
            CloseWindow();
            return;
        }

        _window.OnClose += WindowClosed;

        var prototypes = _prototypes.EnumeratePrototypes<DecalPrototype>();
        _window.Populate(prototypes);
    }

    public void CloseWindow()
    {
        _window?.Close();
    }

    private void WindowClosed()
    {
        if (_window == null)
            return;

        _window.OnClose -= WindowClosed;
        _window = null;
    }
}
