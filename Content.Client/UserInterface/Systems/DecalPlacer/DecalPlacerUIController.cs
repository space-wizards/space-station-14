using Content.Client.Decals.UI;
using Content.Shared.Decals;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client.UserInterface.Systems.DecalPlacer;

public sealed class DecalPlacerUIController : UIController
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private DecalPlacerWindow? _window;

    private void CreateWindow()
    {
        _window = UIManager.CreateWindow<DecalPlacerWindow>();
        LayoutContainer.SetAnchorPreset(_window, LayoutContainer.LayoutPreset.CenterLeft);
    }

    public void ToggleWindow()
    {
        if (_window == null)
        {
            CreateWindow();
        }
        else if (_window.IsOpen)
        {
            CloseWindow();
            return;
        }

        _window!.Open();
        var prototypes = _prototypes.EnumeratePrototypes<DecalPrototype>();
        _window.Populate(prototypes);
    }

    public void CloseWindow()
    {
        _window?.Close();
    }
}
