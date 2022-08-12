using Content.Client.ShipGuns.Components;
using Content.Shared.Input;
using Content.Shared.ShipGuns.Components;
using Content.Shared.ShipGuns.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Client.ShipGuns.Systems;

/// <inheritdoc/>
public sealed class TargetingConsoleSystem : SharedTargetingConsoleSystem
{
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GunnerComponent, ComponentHandleState>(OnHandleState);
        var turretContext = _inputManager.Contexts.New("turret", "common");
        turretContext.AddFunction(ContentKeyFunctions.TurretSafety);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var cursorPosition = EntityCoordinates.FromMap(_mapManager, _eyeManager.ScreenToMap(_inputManager.MouseScreenPosition));

        if (_playerManager.LocalPlayer!.ControlledEntity == null)
            return;

        var character = _playerManager.LocalPlayer!.ControlledEntity.Value;
        if (!EntityManager.TryGetComponent<GunnerComponent>(character, out var component))
            return;

        var args = new GimbalGunSystem.GunnerCursorPositionEvent()
        {
            Coordinates = cursorPosition,
        };

        EntityManager.RaisePredictiveEvent(args);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _inputManager.Contexts.Remove("turret");
    }

    protected override void HandleGunnerShutdown(EntityUid uid, GunnerComponent component, ComponentShutdown args)
    {
        base.HandleGunnerShutdown(uid, component, args);
        if (_playerManager.LocalPlayer?.ControlledEntity != uid)
            return;

        _inputManager.Contexts.SetActiveContext("human");
    }

    private void OnHandleState(EntityUid uid, GunnerComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not GunnerComponentState state)
            return;

        var console = state.Console.GetValueOrDefault();
        if (!console.IsValid())
        {
            component.Console = null;
            _inputManager.Contexts.SetActiveContext("human");
            return;
        }

        if (!TryComp<TargetingConsoleComponent>(console, out var targetingConsoleComponent))
        {
            Logger.Warning($"Unable to set Gunner console to {console}");
            return;
        }

        component.Console = targetingConsoleComponent;
        _actionBlockerSystem.UpdateCanMove(uid);
        _inputManager.Contexts.SetActiveContext("turret");
    }
}
