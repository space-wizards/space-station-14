using Content.Shared.Decals;
using Robust.Client.GameObjects;
using Robust.Client.Input;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;

namespace Content.Client.Decals;

// This is shit and basically a half-rewrite of PlacementManager
// TODO refactor placementmanager so this isnt shit anymore
public sealed class DecalPlacementSystem : EntitySystem
{
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly InputSystem _inputSystem = default!;

    private string? _decalId;
    private Color _decalColor = Color.White;
    private Angle _decalAngle = Angle.Zero;
    private bool _snap;
    private int _zIndex;
    private bool _cleanable;

    private bool _active;
    private bool _placing;
    private bool _erasing;

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder.Bind(EngineKeyFunctions.EditorPlaceObject, new PointerStateInputCmdHandler(
            (session, coords, uid) =>
            {
                if (!_active || _placing || _decalId == null)
                    return false;

                _placing = true;

                if (_snap)
                {
                    var newPos = new Vector2(
                        (float) (MathF.Round(coords.X - 0.5f, MidpointRounding.AwayFromZero) + 0.5),
                        (float) (MathF.Round(coords.Y - 0.5f, MidpointRounding.AwayFromZero) + 0.5)
                    );
                    coords = coords.WithPosition(newPos);
                }

                coords = coords.Offset(new Vector2(-0.5f, -0.5f));

                if (!coords.IsValid(EntityManager))
                    return false;

                var decal = new Decal(coords.Position, _decalId, _decalColor, _decalAngle, _zIndex, _cleanable);
                RaiseNetworkEvent(new RequestDecalPlacementEvent(decal, coords));

                return true;
            },
            (session, coords, uid) =>
            {
                if (!_active)
                    return false;

                _placing = false;
                return true;
            }, true))
            .Bind(EngineKeyFunctions.EditorCancelPlace, new PointerStateInputCmdHandler(
            (session, coords, uid) =>
            {
                if (!_active || _erasing)
                    return false;

                _erasing = true;

                RaiseNetworkEvent(new RequestDecalRemovalEvent(coords));

                return true;
            }, (session, coords, uid) =>
            {
                if (!_active)
                    return false;
                _erasing = false;

                return true;
            }, true)).Register<DecalPlacementSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        CommandBinds.Unregister<DecalPlacementSystem>();
    }

    public void UpdateDecalInfo(string id, Color color, float rotation, bool snap, int zIndex, bool cleanable)
    {
        _decalId = id;
        _decalColor = color;
        _decalAngle = Angle.FromDegrees(rotation);
        _snap = snap;
        _zIndex = zIndex;
        _cleanable = cleanable;
    }

    public void SetActive(bool active)
    {
        _active = active;
        if (_active)
            _inputManager.Contexts.SetActiveContext("editor");
        else
            _inputSystem.SetEntityContextActive();
    }
}
