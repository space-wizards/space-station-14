using System.Numerics;
using Content.Client.Actions;
using Content.Client.Decals.Overlays;
using Content.Shared.Actions;
using Content.Shared.Decals;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Prototypes;

namespace Content.Client.Decals;

// This is shit and basically a half-rewrite of PlacementManager
// TODO refactor placementmanager so this isnt shit anymore
public sealed class DecalPlacementSystem : EntitySystem
{
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly InputSystem _inputSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private string? _decalId;
    private Color _decalColor = Color.White;
    private Angle _decalAngle = Angle.Zero;
    private bool _snap;
    private int _zIndex;
    private bool _cleanable;

    private bool _active;
    private bool _placing;
    private bool _erasing;

    public (DecalPrototype? Decal, bool Snap, Angle Angle, Color Color) GetActiveDecal()
    {
        return _active && _decalId != null ?
            (_protoMan.Index<DecalPrototype>(_decalId), _snap, _decalAngle, _decalColor) :
            (null, false, Angle.Zero, Color.Wheat);
    }

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new DecalPlacementOverlay(this, _transform, _sprite));

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
                RaiseNetworkEvent(new RequestDecalPlacementEvent(decal, GetNetCoordinates(coords)));

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

                RaiseNetworkEvent(new RequestDecalRemovalEvent(GetNetCoordinates(coords)));

                return true;
            }, (session, coords, uid) =>
            {
                if (!_active)
                    return false;
                _erasing = false;

                return true;
            }, true)).Register<DecalPlacementSystem>();

        SubscribeLocalEvent<FillActionSlotEvent>(OnFillSlot);
        SubscribeLocalEvent<PlaceDecalActionEvent>(OnPlaceDecalAction);
    }

    private void OnPlaceDecalAction(PlaceDecalActionEvent args)
    {
        if (args.Handled)
            return;

        if (_transform.GetGrid(args.Target) == null)
            return;

        args.Handled = true;

        if (args.Snap)
        {
            var newPos = new Vector2(
                (float) (MathF.Round(args.Target.X - 0.5f, MidpointRounding.AwayFromZero) + 0.5),
                (float) (MathF.Round(args.Target.Y - 0.5f, MidpointRounding.AwayFromZero) + 0.5)
            );
            args.Target = args.Target.WithPosition(newPos);
        }

        args.Target = args.Target.Offset(new Vector2(-0.5f, -0.5f));

        var decal = new Decal(args.Target.Position, args.DecalId, args.Color, Angle.FromDegrees(args.Rotation), args.ZIndex, args.Cleanable);
        RaiseNetworkEvent(new RequestDecalPlacementEvent(decal, GetNetCoordinates(args.Target)));
    }

    private void OnFillSlot(FillActionSlotEvent ev)
    {
        if (!_active || _placing)
            return;

        if (ev.Action != null)
            return;

        if (_decalId == null || !_protoMan.TryIndex<DecalPrototype>(_decalId, out var decalProto))
            return;

        var actionEvent = new PlaceDecalActionEvent()
        {
            DecalId = _decalId,
            Color = _decalColor,
            Rotation = _decalAngle.Degrees,
            Snap = _snap,
            ZIndex = _zIndex,
            Cleanable = _cleanable,
        };

        var actionId = Spawn(null);
        AddComp(actionId, new WorldTargetActionComponent
        {
            // non-unique actions may be considered duplicates when saving/loading.
            Icon = decalProto.Sprite,
            Repeat = true,
            ClientExclusive = true,
            CheckCanAccess = false,
            CheckCanInteract = false,
            Range = -1,
            Event = actionEvent,
            IconColor = _decalColor,
        });

        _metaData.SetEntityName(actionId, $"{_decalId} ({_decalColor.ToHex()}, {(int) _decalAngle.Degrees})");

        ev.Action = actionId;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlay.RemoveOverlay<DecalPlacementOverlay>();
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
