using Content.Client.Clickable;
using Content.Client.Gameplay;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.State;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Client.Weapons.Ranged.Systems;

public sealed class TetherGunSystem : SharedTetherGunSystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly InputSystem _inputSystem = default!;

    public bool Enabled { get; set; }

    /// <summary>
    /// The entity being dragged around.
    /// </summary>
    private EntityUid? _dragging;
    private EntityUid? _tether;

    private MapCoordinates? _lastMousePosition;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<PredictTetherEvent>(OnPredictTether);
        SubscribeNetworkEvent<TetherGunToggleMessage>(OnTetherGun);
    }

    private void OnTetherGun(TetherGunToggleMessage ev)
    {
        Enabled = ev.Enabled;
    }

    private void OnPredictTether(PredictTetherEvent ev)
    {
        if (_dragging != ev.Entity) return;

        _tether = ev.Entity;
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);
        if (!TryComp<PhysicsComponent>(_dragging, out var body)) return;

        body.Predict = true;

        if (TryComp<PhysicsComponent>(_tether, out var tetherBody))
        {
            tetherBody.Predict = true;
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!Enabled || !_gameTiming.IsFirstTimePredicted) return;

        var state = _inputSystem.CmdStates.GetState(EngineKeyFunctions.Use);

        if (state != BoundKeyState.Down)
        {
            StopDragging();
            return;
        }

        var mouseScreenPos = _inputManager.MouseScreenPosition;
        var mousePos = _eyeManager.ScreenToMap(mouseScreenPos);

        if (_dragging == null)
        {
            var gameState = IoCManager.Resolve<IStateManager>().CurrentState;

            if (gameState is GameplayState game)
            {
                var uid = game.GetClickedEntity(mousePos);

                if (uid != null)
                    StartDragging(uid.Value, mousePos);
            }

            if (_dragging == null)
                return;
        }

        if (!TryComp<TransformComponent>(_dragging!.Value, out var xform) ||
            _lastMousePosition!.Value.MapId != xform.MapID ||
            !TryComp<PhysicsComponent>(_dragging, out var body))
        {
            StopDragging();
            return;
        }

        body.Predict = true;

        if (TryComp<PhysicsComponent>(_tether, out var tetherBody))
        {
            tetherBody.Predict = true;
        }

        if (_lastMousePosition.Value.Position.EqualsApprox(mousePos.Position)) return;

        _lastMousePosition = mousePos;

        RaiseNetworkEvent(new TetherMoveEvent()
        {
            Coordinates = _lastMousePosition!.Value,
        });
    }

    private void StopDragging()
    {
        if (_dragging == null) return;

        RaiseNetworkEvent(new StopTetherEvent());
        _dragging = null;
        _lastMousePosition = null;
        _tether = null;
    }

    private void StartDragging(EntityUid uid, MapCoordinates coordinates)
    {
        _dragging = uid;
        _lastMousePosition = coordinates;
        RaiseNetworkEvent(new StartTetherEvent()
        {
            Entity = _dragging!.Value,
            Coordinates = coordinates,
        });
    }
}
