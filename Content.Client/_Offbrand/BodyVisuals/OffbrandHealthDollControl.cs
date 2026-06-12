using System.Linq;
using System.Numerics;
using Content.Client.Actions;
using Content.Client.Clickable;
using Content.Client.UserInterface.Systems.Actions;
using Content.Shared.Body;
using Content.Shared.Input;
using Robust.Client.GameObjects;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Direction = Robust.Shared.Maths.Direction;

namespace Content.Client._Offbrand.BodyVisuals;

public sealed class OffbrandHealthDollControl : SpriteView
{
    private static readonly EntProtoId DollPrototype = "OffbrandHealthDoll";

    private readonly BodyAppearanceRelaySystem _relay;
    private readonly InputSystem _inputSystem;

    private readonly IClickMapManager _clickMap;
    private readonly IInputManager _inputManager;
    private readonly IPlayerManager _player;
    private readonly IGameTiming _timing;

    private EntityUid? _body;
    private SpriteComponent.Layer? _hoveredLayer;
    private EntityUid? _hoveredOrgan;
    private Color _hoveredColor = Color.White;

    public OffbrandHealthDollControl()
    {
        IoCManager.Resolve(ref _clickMap, ref _inputManager, ref _player, ref _timing);
        _relay = EntMan.System<BodyAppearanceRelaySystem>();
        _inputSystem = EntMan.System<InputSystem>();

        OverrideDirection = Direction.South;
        Scale = new Vector2(2, 2);
        SetSize = new Vector2(64, 64);

        MouseFilter = MouseFilterMode.Pass;
    }

    public void SetBody(EntityUid? body)
    {
        if (_body is { } oldBody && Entity is { } oldDoll)
            _relay.RemoveTarget(oldBody, oldDoll);

        _body = body;

        if (_body is { } newBody && Entity is { } doll)
            _relay.AddTarget(newBody, doll);
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();

        if (Entity is not null)
            return;

        var doll = EntMan.SpawnEntity(DollPrototype, MapCoordinates.Nullspace);
        SetEntity(doll);

        if (_body is { } body)
            _relay.AddTarget(body, doll);
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();

        if (Entity is { } doll)
            EntMan.DeleteEntity(doll);

        SetEntity(null);
    }

    protected override void MouseExited()
    {
        base.MouseExited();

        if (_hoveredLayer is { } oldHovered)
        {
            oldHovered.Color = _hoveredColor;
        }

        _hoveredLayer = null;
        _hoveredOrgan = null;
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);

        if (SpriteSystem is null)
            return;

        if (Entity is not { } doll || _body is not { } body)
            return;

        if (!EntMan.TryGetComponent<BodyComponent>(body, out var bodyComp))
            return;

        var imagePosition = (Vector2i)(args.RelativePosition / Scale);
        SpriteComponent.Layer? found = null;
        var foundOrgan = EntityUid.Invalid;

        foreach (var iLayer in doll.Comp1.AllLayers.Reverse())
        {
            if (iLayer is not SpriteComponent.Layer layer || !SpriteSystem.IsVisible(layer))
                continue;

            if (layer.ActualRsi is not { } rsi || !rsi.TryGetState(layer.State, out _))
                continue;

            if (!_clickMap.IsOccluding(rsi, layer.State, RsiDirection.South, layer.AnimationFrame, imagePosition, 0))
                continue;

            foreach (var organ in bodyComp.Organs?.ContainedEntities ?? [])
            {
                if (!EntMan.TryGetComponent<VisualOrganComponent>(organ, out var visualOrgan))
                    continue;

                if (!SpriteSystem.TryGetLayer((doll, doll), visualOrgan.Layer, out var existingLayer, false))
                    continue;

                if (existingLayer != layer)
                    continue;

                foundOrgan = organ;
                break;
            }

            if (foundOrgan == EntityUid.Invalid)
                continue;

            found = layer;
            break;
        }

        if (_hoveredLayer is { } oldHovered)
        {
            oldHovered.Color = _hoveredColor;
        }

        _hoveredLayer = found;
        if (_hoveredLayer is { } newHovered)
        {
            _hoveredColor = newHovered.Color;
            _hoveredOrgan = foundOrgan;
            newHovered.Color = Color.FromHex("#00d485");
        }
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        if (EntMan.Deleted(_hoveredOrgan) || _player.LocalSession is null)
            return;

        var func = args.Function;
        var funcId = _inputManager.NetworkBindMap.KeyFunctionID(func);
        var message = new ClientFullInputCmdMessage(
            _timing.CurTick,
            _timing.TickFraction,
            funcId)
        {
            State = BoundKeyState.Down,
            Coordinates = EntMan.GetComponent<TransformComponent>(_hoveredOrgan.Value).Coordinates,
            ScreenCoordinates = args.PointerLocation,
            Uid = _hoveredOrgan.Value,
        };

        var actions = UserInterfaceManager.GetUIController<ActionUIController>();
        var cmd = new PointerInputCmdHandler.PointerInputCmdArgs(
            _player.LocalSession,
            EntMan.GetComponent<TransformComponent>(_hoveredOrgan.Value).Coordinates,
            args.PointerLocation,
            _hoveredOrgan.Value,
            BoundKeyState.Down,
            message
        );

        if (args.Function == EngineKeyFunctions.Use && actions.TargetingOnUse(in cmd))
        {
            args.Handle();
            return;
        }

        if (args.Function == EngineKeyFunctions.Use ||
            args.Function == ContentKeyFunctions.ActivateItemInWorld ||
            args.Function == ContentKeyFunctions.AltActivateItemInWorld ||
            args.Function == ContentKeyFunctions.Point ||
            args.Function == ContentKeyFunctions.TryPullObject)
        {
            _inputSystem.HandleInputCommand(_player.LocalSession, func, message);
            args.Handle();
        }
    }
}
