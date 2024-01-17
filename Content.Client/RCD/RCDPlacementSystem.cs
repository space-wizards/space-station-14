using Content.Client.Construction;
using Content.Client.Interactable;
using Content.Shared.Hands.Components;
using Content.Shared.Input;
using Content.Shared.RCD;
using Content.Shared.RCD.Components;
using Content.Shared.RCD.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Placement;
using Robust.Client.Placement.Modes;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using System.Numerics;

namespace Content.Client.RCD;

public sealed class RCDPlacementSystem : EntitySystem
{
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly InteractionSystem _interactionSystem = default!;

    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly RCDSystem _rcdSystem = default!;

    private EntityUid? _constructionGhost = null;
    private ProtoId<RCDPrototype>? _constructionPrototype = null;
    private RCDPrototype? _cachedPrototype = null;

    private bool _isKeyBindActive = false;
    private readonly BoundKeyFunction _keyfunction = EngineKeyFunctions.EditorRotateObject;

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

        // Rotate the construction ghost if keybind was actived this frame
        if (WasKeybindActivatedThisFrame(player.Value) && hasRCD)
            RotateRCDConstructionGhost(uid!.Value, rcd!);

        // Delete the construction ghost if its no longer needed
        if (!hasRCD)
        {
            DeleteConstructionGhost();
            return;
        }

        if (_constructionPrototype != rcd!.ProtoId)
        {
            DeleteConstructionGhost();

            _constructionPrototype = rcd!.ProtoId;
            _protoManager.TryIndex(rcd!.ProtoId, out _cachedPrototype);
        }

        if (_cachedPrototype == null)
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

        var tilePosition = _mapSystem.GridTileToLocal(mapGridData.Value.GridUid, mapGridData.Value.Component, mapGridData.Value.Position);

        if (!_interactionSystem.InRangeUnobstructed(mapGridData.Value.Location.ToMap(EntityManager, _transformSystem), player.Value))
        {
            DeleteConstructionGhost();
            return;
        }

        // Create a new construction ghost if needed
        SpriteComponent? sprite;

        if (_constructionGhost == null)
        {
            _constructionGhost = EntityManager.SpawnEntity("rcdconstructionghost", tilePosition);

            sprite = EntityManager.GetComponent<SpriteComponent>(_constructionGhost.Value);
            sprite.Color = new Color(48, 255, 48, 128);

            sprite.AddBlankLayer(0);
            sprite.LayerSetSprite(0, _cachedPrototype.GhostIcon);
            sprite.LayerSetShader(0, "unshaded");
            sprite.LayerSetVisible(0, true);

            if (_cachedPrototype.RotationRule == RcdRotationRule.Camera)
                sprite.NoRotation = true;
        }

        // Update color of the construction ghost
        var isValid = _rcdSystem.IsConstructionLocationValid(uid!.Value, rcd, mapGridData.Value, player.Value, false);
        sprite = EntityManager.GetComponent<SpriteComponent>(_constructionGhost.Value);
        sprite.Color = isValid ? new Color(48, 255, 48, 128) : new Color(255, 48, 48, 128);

        // Update the construction ghost position and rotation
        _transformSystem.SetLocalPosition(_constructionGhost.Value, tilePosition.Position);

        if (_cachedPrototype.RotationRule == RcdRotationRule.User)
            _transformSystem.SetLocalRotation(_constructionGhost.Value, rcd!.PrototypeDirection.ToAngle());
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

        switch (rcd.PrototypeDirection)
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
        // Try to get current map.
        var map = MapId.Nullspace;

        if (EntityManager.TryGetComponent(player, out TransformComponent? xform))
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

        if (!_mapManager.TryFindGridAt(mapCoords, out var gridUid, out var grid))
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
