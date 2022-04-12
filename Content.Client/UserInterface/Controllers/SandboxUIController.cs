using Content.Client.Administration.Managers;
using Content.Client.Gameplay;
using Content.Client.Markers;
using Content.Client.Sandbox;
using Content.Client.SubFloor;
using Content.Client.UserInterface.UIWindows;
using Content.Shared.Input;
using Robust.Client.Debugging;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Input.Binding;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.UserInterface.Controllers;

// TODO hud refactor should part of this be in engine?
public sealed class SandboxUIController : UIController
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

    private SandboxWindow? _sandboxWindow;

    // TODO hud refactor BEFORE MERGE cache
    private EntitySpawningUIController EntitySpawningController => _controllers.GetController<EntitySpawningUIController>();
    private TileSpawningUIController TileSpawningController => _controllers.GetController<TileSpawningUIController>();
    private DecalPlacerUIController DecalPlacerController => _controllers.GetController<DecalPlacerUIController>();

    public override void OnStateChanged(StateChangedEventArgs args)
    {
        if (args.NewState is not GameplayState)
            return;

        _admin.AdminStatusUpdated += CheckStatus;

        _input.SetInputCommand(ContentKeyFunctions.OpenEntitySpawnWindow,
            InputCmdHandler.FromDelegate(_ => EntitySpawningController.ToggleWindow()));
        _input.SetInputCommand(ContentKeyFunctions.OpenSandboxWindow,
            InputCmdHandler.FromDelegate(_ => ToggleSandboxWindow()));
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

    private void OpenSandboxWindow()
    {
        if (_sandboxWindow != null)
        {
            if (!_sandboxWindow.IsOpen)
                _sandboxWindow.Open();

            return;
        }

        _sandboxWindow = new SandboxWindow();

        _sandboxWindow.ToggleLightButton.Pressed = !_light.Enabled;
        _sandboxWindow.ToggleFovButton.Pressed = !_eye.CurrentEye.DrawFov;
        _sandboxWindow.ToggleShadowsButton.Pressed = !_light.DrawShadows;
        _sandboxWindow.ToggleSubfloorButton.Pressed = _subfloorHide.ShowAll;
        _sandboxWindow.ShowMarkersButton.Pressed = _marker.MarkersVisible;
        _sandboxWindow.ShowBbButton.Pressed = (_debugPhysics.Flags & PhysicsDebugFlags.Shapes) != 0x0;

        _sandboxWindow.OnClose += SandboxWindowOnClose;

        _sandboxWindow.RespawnButton.OnPressed += OnRespawnPressed;
        _sandboxWindow.SpawnTilesButton.OnPressed += OnSpawnTilesClicked;
        _sandboxWindow.SpawnEntitiesButton.OnPressed += OnSpawnEntitiesClicked;
        _sandboxWindow.SpawnDecalsButton.OnPressed += OnSpawnDecalsClicked;
        _sandboxWindow.GiveFullAccessButton.OnPressed += OnGiveAdminAccessClicked;
        _sandboxWindow.GiveAghostButton.OnPressed += OnGiveAghostClicked;
        _sandboxWindow.ToggleLightButton.OnToggled += OnToggleLightClicked;
        _sandboxWindow.ToggleFovButton.OnToggled += OnToggleFovClicked;
        _sandboxWindow.ToggleShadowsButton.OnToggled += OnToggleShadowsClicked;
        _sandboxWindow.SuicideButton.OnPressed += OnSuicideClicked;
        _sandboxWindow.ToggleSubfloorButton.OnPressed += OnToggleSubfloorClicked;
        _sandboxWindow.ShowMarkersButton.OnPressed += OnShowMarkersClicked;
        _sandboxWindow.ShowBbButton.OnPressed += OnShowBbClicked;
        _sandboxWindow.MachineLinkingButton.OnPressed += OnMachineLinkingClicked;

        _sandboxWindow.OpenCentered();
    }

    private void CheckStatus()
    {
        if (!CanSandbox())
            CloseAll();
    }

    private void CloseAll()
    {
        _sandboxWindow?.Close();
        _sandboxWindow = null;
        EntitySpawningController.CloseWindow();
        TileSpawningController.CloseWindow();
        DecalPlacerController.CloseWindow();
    }

    private bool CanSandbox()
    {
        return _sandbox.SandboxAllowed || _admin.IsActive();
    }

    private void SandboxWindowOnClose()
    {
        _sandboxWindow = null;
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
    private void ToggleSandboxWindow()
    {
        UpdateSandboxWindowVisibility();
    }

    private void UpdateSandboxWindowVisibility()
    {
        if (CanSandbox() && _sandboxWindow?.IsOpen != true)
            OpenSandboxWindow();
        else
            _sandboxWindow?.Close();
    }
}
