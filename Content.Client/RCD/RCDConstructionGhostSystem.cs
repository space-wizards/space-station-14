using Content.Client.Gameplay;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.RCD;
using Content.Shared.RCD.Components;
using Content.Shared.RCD.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.RCD;

public sealed class RCDConstructionGhostSystem : EntitySystem
{
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly RCDSystem _rcdSystem = default!;

    private EntityUid? _constructionGhost = null;
    private Vector2i _constructionGhostPosition = default;
    private Direction _constructionGhostDirection = default;
    private ProtoId<RCDPrototype>? _constructionPrototype = null;
    private RCDPrototype? _cachedPrototype = null;
    private bool _isKeyBindActive = false;

    private readonly BoundKeyFunction _keyfunction = EngineKeyFunctions.EditorRotateObject;
    private readonly EntProtoId _rcdConstructionGhost = "rcdconstructionghost";
    private readonly Color _validConstructionColor = new Color(48, 255, 48, 128);
    private readonly Color _invalidConstructionColor = new Color(255, 48, 48, 128);

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Determine if player is carrying an RCD in their active hand
        var player = _playerManager.LocalSession?.AttachedEntity;

        if (!TryComp<HandsComponent>(player, out var hands))
            return;

        var uid = hands.ActiveHand?.HeldEntity;
        var hasRCD = TryComp<RCDComponent>(uid, out var rcd);

        // Rotate the construction ghost if the keybind was actived this frame
        if (WasKeybindActivatedThisFrame(player.Value) && hasRCD && uid != null)
            RotateRCDConstructionGhost(uid.Value, rcd!);

        // Delete the construction ghost if its no longer needed
        if (!hasRCD ||
            !TryComp<InputComponent>(player, out var input) ||
            _inputManager.Contexts.ActiveContext.Name != input.ContextName)
        {
            DeleteConstructionGhost();
            return;
        }

        if (rcd == null || _constructionPrototype != rcd.ProtoId)
        {
            DeleteConstructionGhost();

            _constructionPrototype = rcd!.ProtoId;
            _protoManager.TryIndex(rcd!.ProtoId, out _cachedPrototype);
        }

        if (_cachedPrototype?.GhostSprite == null)
        {
            DeleteConstructionGhost();
            return;
        }

        // Try to get the tile the player is hovering over
        if (!CurrentMousePosition(player.Value, out var screenCoords))
            return;

        var location = ScreenToCursorGrid(screenCoords);

        if (!_rcdSystem.TryGetMapGridData(location, out var mapGridData))
            return;

        // Delete the construction ghost if the distance between the player and the target tile is too great
        var tileWorldPosition = _mapSystem.GridTileToWorldPos(mapGridData.Value.GridUid, mapGridData.Value.Component, mapGridData.Value.Position);
        var distance = (tileWorldPosition - _transformSystem.GetWorldPosition(player.Value)).Length();

        if (distance > SharedInteractionSystem.InteractionRange)
        {
            DeleteConstructionGhost();
            return;
        }

        // Create a new construction ghost if needed
        SpriteComponent? sprite;
        var tilePosition = _mapSystem.GridTileToLocal(mapGridData.Value.GridUid, mapGridData.Value.Component, mapGridData.Value.Position);

        if (_constructionGhost == null)
        {
            _constructionGhost = Spawn(_rcdConstructionGhost, tilePosition);

            if (TryComp(_constructionGhost.Value, out sprite))
            {
                sprite.Color = _validConstructionColor;

                sprite.AddBlankLayer(0);
                sprite.LayerSetSprite(0, _cachedPrototype.GhostSprite);
                sprite.LayerSetShader(0, "unshaded");
                sprite.LayerSetVisible(0, true);

                if (_cachedPrototype.Rotation == RcdRotation.Camera)
                    sprite.NoRotation = true;
            }
        }

        // See if the mouse is hovering over a target
        var currentState = _stateManager.CurrentState;

        if (currentState is not GameplayStateBase screen)
            return;

        var target = screen.GetClickedEntity(location.ToMap(EntityManager, _transformSystem));

        // Update color of the construction ghost
        var isValid = _rcdSystem.IsRCDOperationStillValid(uid!.Value, rcd, mapGridData.Value, target, player.Value, false);

        if (TryComp(_constructionGhost.Value, out sprite))
            sprite.Color = isValid ? _validConstructionColor : _invalidConstructionColor;

        // Update the construction ghost position and rotation if they have changed
        if (_constructionGhostPosition != mapGridData.Value.Position)
        {
            _constructionGhostPosition = mapGridData.Value.Position;
            _transformSystem.SetWorldPosition(_constructionGhost.Value, tileWorldPosition);
        }

        if (_constructionGhostDirection != rcd.ConstructionDirection &&
            _cachedPrototype.Rotation == RcdRotation.User)
        {
            _constructionGhostDirection = rcd.ConstructionDirection;
            _transformSystem.SetLocalRotation(_constructionGhost.Value, rcd.ConstructionDirection.ToAngle());
        }
    }

    // Work around to capture player input
    private bool WasKeybindActivatedThisFrame(EntityUid player)
    {
        if (!_inputManager.TryGetKeyBinding(_keyfunction, out var keybinding))
            return false;

        if (!_inputManager.IsKeyDown(keybinding.BaseKey))
        {
            _isKeyBindActive = false;
            return false;
        }

        if (_inputManager.IsKeyDown(Keyboard.Key.Shift) &&
            !(keybinding.Mod1 == Keyboard.Key.Shift ||
              keybinding.Mod2 == Keyboard.Key.Shift ||
              keybinding.Mod3 == Keyboard.Key.Shift))
        {
            _isKeyBindActive = false;
            return false;
        }

        if (_inputManager.IsKeyDown(Keyboard.Key.Alt) &&
            !(keybinding.Mod1 == Keyboard.Key.Alt ||
              keybinding.Mod2 == Keyboard.Key.Alt ||
              keybinding.Mod3 == Keyboard.Key.Alt))
        {
            _isKeyBindActive = false;
            return false;
        }

        if (_inputManager.IsKeyDown(Keyboard.Key.Control) &&
            !(keybinding.Mod1 == Keyboard.Key.Control ||
              keybinding.Mod2 == Keyboard.Key.Control ||
              keybinding.Mod3 == Keyboard.Key.Control))
        {
            _isKeyBindActive = false;
            return false;
        }

        if (_isKeyBindActive)
            return false;

        _isKeyBindActive = true;

        // Prevent the key bind activating when using non-default input contexts
        if (!EntityManager.TryGetComponent<InputComponent>(player, out var input))
            return false;

        if (_inputManager.Contexts.ActiveContext.Name != input.ContextName)
            return false;

        return true;
    }

    private void RotateRCDConstructionGhost(EntityUid uid, RCDComponent rcd)
    {
        var direction = Direction.South;

        switch (rcd.ConstructionDirection)
        {
            case Direction.North:
                direction = Direction.East;
                break;
            case Direction.East:
                direction = Direction.South;
                break;
            case Direction.South:
                direction = Direction.West;
                break;
            case Direction.West:
                direction = Direction.North;
                break;
        }

        RaiseNetworkEvent(new RCDConstructionGhostRotationEvent(GetNetEntity(uid), direction));
    }

    private bool CurrentMousePosition(EntityUid player, out ScreenCoordinates coordinates)
    {
        var map = MapId.Nullspace;

        if (TryComp(player, out TransformComponent? xform))
            map = xform.MapID;

        if (map == MapId.Nullspace)
        {
            coordinates = default;
            return false;
        }

        coordinates = _inputManager.MouseScreenPosition;
        return true;
    }

    private EntityCoordinates ScreenToCursorGrid(ScreenCoordinates coords)
    {
        var mapCoords = _eyeManager.PixelToMap(coords.Position);

        if (!_mapManager.TryFindGridAt(mapCoords, out var gridUid, out var _))
            return EntityCoordinates.FromMap(_mapManager, mapCoords);

        return EntityCoordinates.FromMap(gridUid, mapCoords, _transformSystem);
    }

    private void DeleteConstructionGhost()
    {
        if (_constructionGhost == null)
            return;

        QueueDel(_constructionGhost);
        _constructionGhost = null;
    }
}
