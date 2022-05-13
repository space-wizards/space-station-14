using Content.Client.Clickable;
using Content.Shared.Weapons.Ranged;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.Weapons.Ranged;

public sealed class TetherGunSystem : SharedTetherGunSystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly InputSystem _inputSystem = default!;

    public bool Enabled { get; set; }

    /// <summary>
    /// The entity being dragged around.
    /// </summary>
    private EntityUid? _dragging;

    private MapCoordinates? _lastMousePosition;

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
            var bodyQuery = GetEntityQuery<PhysicsComponent>();
            var lowest = new List<(int DrawDepth, EntityUid Entity)>();

            foreach (var ent in _lookup.GetEntitiesIntersecting(mousePos))
            {
                if (!bodyQuery.HasComponent(ent) ||
                    !TryComp<ClickableComponent>(ent, out var clickable) ||
                    !clickable.CheckClick(mousePos.Position, out var drawDepth, out _)) continue;

                lowest.Add((drawDepth, ent));
            }

            lowest.Sort((x, y) => y.DrawDepth.CompareTo(x.DrawDepth));

            foreach (var ent in lowest)
            {
                StartDragging(ent.Entity, mousePos);
                break;
            }

            if (_dragging == null) return;
        }

        if (!TryComp<TransformComponent>(_dragging!.Value, out var xform) ||
            _lastMousePosition!.Value.MapId != xform.MapID)
        {
            StopDragging();
            return;
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
