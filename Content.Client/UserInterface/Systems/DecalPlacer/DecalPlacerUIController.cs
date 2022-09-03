using Content.Client.Decals.UI;
using Content.Client.Gameplay;
using Content.Client.Sandbox;
using Content.Shared.Decals;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Systems.DecalPlacer;

public sealed class DecalPlacerUIController : UIController, IOnStateChanged<GameplayState>, IOnSystemChanged<SandboxSystem>
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [UISystemDependency] private readonly SandboxSystem _sandbox = default!;

    private DecalPlacerWindow? _window;

    public void ToggleWindow()
    {
        if (_window == null)
        {
            return;
        }

        if (_window.IsOpen)
        {
            _window.Close();
        }
        else if(_sandbox.SandboxAllowed)
        {
            _window.Open();
        }
    }

    public void OnStateEntered(GameplayState state)
    {
        DebugTools.Assert(_window == null);
        _window = UIManager.CreateWindow<DecalPlacerWindow>();
        LayoutContainer.SetAnchorPreset(_window, LayoutContainer.LayoutPreset.CenterLeft);
        ReloadPrototypes();
    }

    public void OnStateExited(GameplayState state)
    {
        _window!.DisposeAllChildren();
    }

    public void OnSystemLoaded(SandboxSystem system)
    {
        _sandbox.SandboxDisabled += CloseWindow;
        _prototypes.PrototypesReloaded += OnPrototypesReloaded;
    }

    public void OnSystemUnloaded(SandboxSystem system)
    {
        _sandbox.SandboxDisabled -= CloseWindow;
        _prototypes.PrototypesReloaded -= OnPrototypesReloaded;
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs obj)
    {
        ReloadPrototypes();
    }

    private void ReloadPrototypes()
    {
        if (_window == null)
        {
            return;
        }
        var prototypes = _prototypes.EnumeratePrototypes<DecalPrototype>();
        _window.Populate(prototypes);
    }

    private void CloseWindow()
    {
        _window?.Close();
    }
}
