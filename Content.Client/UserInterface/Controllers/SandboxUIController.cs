using Content.Client.Administration.Managers;
using Content.Client.Gameplay;
using Content.Client.Markers;
using Content.Client.Sandbox;
using Content.Client.SubFloor;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.UIWindows;
using Content.Shared.Input;
using Robust.Client.Debugging;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Input.Binding;
using static Robust.Client.UserInterface.Controls.BaseButton;
using MenuBar = Content.Client.UserInterface.Widgets.MenuBar;

namespace Content.Client.UserInterface.Controllers;

// TODO hud refactor should part of this be in engine?
public sealed class SandboxUIController : UIController, IOnStateChanged<GameplayState>
{
    [Dependency] private readonly IClientAdminManager _admin = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly ILightManager _light = default!;
    [Dependency] private readonly IUIControllerManager _controllers = default!;

    [UISystemDependency] private readonly DebugPhysicsSystem _debugPhysics = default!;
    [UISystemDependency] private readonly MarkerSystem _marker = default!;
    [UISystemDependency] private readonly SandboxSystem _sandbox = default!;
    [UISystemDependency] private readonly SubFloorHideSystem _subfloorHide = default!;

    private SandboxWindow? _window;

    // TODO hud refactor BEFORE MERGE cache
    private EntitySpawningUIController EntitySpawningController => _controllers.GetController<EntitySpawningUIController>();
    private TileSpawningUIController TileSpawningController => _controllers.GetController<TileSpawningUIController>();
    private DecalPlacerUIController DecalPlacerController => _controllers.GetController<DecalPlacerUIController>();

    private MenuButton SandboxButton => UIManager.GetActiveUIWidget<MenuBar>().SandboxButton;

    public void OnStateChanged(GameplayState state)
    {
        SandboxButton.OnPressed += SandboxButtonPressed;
        _admin.AdminStatusUpdated += CheckStatus;

        _input.SetInputCommand(ContentKeyFunctions.OpenEntitySpawnWindow,
            InputCmdHandler.FromDelegate(_ => EntitySpawningController.ToggleWindow()));
        _input.SetInputCommand(ContentKeyFunctions.OpenSandboxWindow,
            InputCmdHandler.FromDelegate(_ => ToggleWindow()));
        _input.SetInputCommand(ContentKeyFunctions.OpenTileSpawnWindow,
            InputCmdHandler.FromDelegate(_ => TileSpawningController.ToggleWindow()));
        _input.SetInputCommand(ContentKeyFunctions.OpenDecalSpawnWindow,
            InputCmdHandler.FromDelegate(_ => DecalPlacerController.ToggleWindow()));
    }

    public override void OnSystemLoaded(IEntitySystem system)
    {
        switch (system)
        {
            case SandboxSystem sandbox:
                SandboxSystemEnabled(sandbox);
                break;
        }
    }

    public override void OnSystemUnloaded(IEntitySystem system)
    {
        switch (system)
        {
            case SandboxSystem sandbox:
                SandboxSystemEnabled(sandbox);
                break;
        }
    }

    private void SandboxSystemEnabled(SandboxSystem system)
    {
        system.SandboxEnabled += CheckStatus;
        system.SandboxDisabled += CheckStatus;
    }

    private void SandboxButtonPressed(ButtonEventArgs args)
    {
        ToggleWindow();
    }

    private void OpenWindow()
    {
        if (_window != null)
        {
            if (!_window.IsOpen)
                _window.Open();

            return;
        }

        _window = new SandboxWindow();

        _window.ToggleLightButton.Pressed = !_light.Enabled;
        _window.ToggleFovButton.Pressed = !_eye.CurrentEye.DrawFov;
        _window.ToggleShadowsButton.Pressed = !_light.DrawShadows;
        _window.ToggleSubfloorButton.Pressed = _subfloorHide.ShowAll;
        _window.ShowMarkersButton.Pressed = _marker.MarkersVisible;
        _window.ShowBbButton.Pressed = (_debugPhysics.Flags & PhysicsDebugFlags.Shapes) != 0x0;

        _window.OnClose += WindowOnClose;

        _window.RespawnButton.OnPressed += OnRespawnPressed;
        _window.SpawnTilesButton.OnPressed += OnSpawnTilesClicked;
        _window.SpawnEntitiesButton.OnPressed += OnSpawnEntitiesClicked;
        _window.SpawnDecalsButton.OnPressed += OnSpawnDecalsClicked;
        _window.GiveFullAccessButton.OnPressed += OnGiveAdminAccessClicked;
        _window.GiveAghostButton.OnPressed += OnGiveAghostClicked;
        _window.ToggleLightButton.OnToggled += OnToggleLightClicked;
        _window.ToggleFovButton.OnToggled += OnToggleFovClicked;
        _window.ToggleShadowsButton.OnToggled += OnToggleShadowsClicked;
        _window.SuicideButton.OnPressed += OnSuicideClicked;
        _window.ToggleSubfloorButton.OnPressed += OnToggleSubfloorClicked;
        _window.ShowMarkersButton.OnPressed += OnShowMarkersClicked;
        _window.ShowBbButton.OnPressed += OnShowBbClicked;
        _window.MachineLinkingButton.OnPressed += OnMachineLinkingClicked;

        _window.OpenCentered();
        SandboxButton.Pressed = true;
    }

    private void CloseWindow()
    {
        _window?.Close();
        SandboxButton.Pressed = false;
    }

    private void CheckStatus()
    {
        if (!CanSandbox())
            CloseAll();
    }

    private void CloseAll()
    {
        CloseWindow();
        EntitySpawningController.CloseWindow();
        TileSpawningController.CloseWindow();
        DecalPlacerController.CloseWindow();
    }

    private bool CanSandbox()
    {
        return _sandbox.SandboxAllowed || _admin.IsActive();
    }

    private void WindowOnClose()
    {
        _window = null;
        SandboxButton.Pressed = false;
    }

    private void OnRespawnPressed(ButtonEventArgs args)
    {
        _sandbox.Respawn();
    }

    private void OnSpawnEntitiesClicked(ButtonEventArgs args)
    {
        EntitySpawningController.ToggleWindow();
    }

    private void OnSpawnTilesClicked(ButtonEventArgs args)
    {
        TileSpawningController.ToggleWindow();
    }

    private void OnSpawnDecalsClicked(ButtonEventArgs obj)
    {
        DecalPlacerController.ToggleWindow();
    }

    private void OnToggleLightClicked(ButtonEventArgs args)
    {
        _sandbox.ToggleLight();
    }

    private void OnToggleFovClicked(ButtonEventArgs args)
    {
        _sandbox.ToggleFov();
    }

    private void OnToggleShadowsClicked(ButtonEventArgs args)
    {
        _sandbox.ToggleShadows();
    }

    private void OnToggleSubfloorClicked(ButtonEventArgs args)
    {
        _sandbox.ToggleSubFloor();
    }

    private void OnShowMarkersClicked(ButtonEventArgs args)
    {
        _sandbox.ShowMarkers();
    }

    private void OnShowBbClicked(ButtonEventArgs args)
    {
        _sandbox.ShowBb();
    }

    private void OnMachineLinkingClicked(ButtonEventArgs args)
    {
        _sandbox.MachineLinking();
    }

    private void OnGiveAdminAccessClicked(ButtonEventArgs args)
    {
        _sandbox.GiveAdminAccess();
    }

    private void OnGiveAghostClicked(ButtonEventArgs args)
    {
        _sandbox.GiveAGhost();
    }

    private void OnSuicideClicked(ButtonEventArgs args)
    {
        _sandbox.Suicide();
    }

    // TODO: These should check for command perms + be reset if the round is over.
    private void ToggleWindow()
    {
        if (CanSandbox() && _window?.IsOpen != true)
            OpenWindow();
        else
            CloseWindow();
    }
}
