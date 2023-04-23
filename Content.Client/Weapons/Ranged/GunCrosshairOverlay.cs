using System.Linq;
using Content.Client.Weapons.Ranged.Systems;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.GameObjects;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Robust.Shared.Prototypes;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Utility;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Physics;

namespace Content.Client.Weapons.Ranged;

public sealed class GunCrosshairOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    private IEntityManager _entManager;
    private readonly IEyeManager _eye;
    private readonly IGameTiming _timing;
    private readonly IInputManager _input;
    private readonly IPlayerManager _player;
    private readonly GunSystem _guns;
    private readonly IPrototypeManager _protoManager;
    private readonly SharedPhysicsSystem _physics;

    private Texture? _crosshair;
    private Texture? _crosssign;

    public GunCrosshairOverlay(IEntityManager entManager, IEyeManager eyeManager, IGameTiming timing, IInputManager input,
        IPlayerManager player, IPrototypeManager prototypes, SharedPhysicsSystem physics, GunSystem system)
    {
        _entManager = entManager;
        _eye = eyeManager;
        _input = input;
        _timing = timing;
        _player = player;
        _guns = system;
        _protoManager = prototypes;
        _physics = physics;

        _crosshair = GetTextureFromRsi("crosshair");
        _crosssign = GetTextureFromRsi("crosssign");
    }

    private Texture GetTextureFromRsi(string _spriteName)
    {
        var sprite = new SpriteSpecifier.Rsi(
            new ResPath("/Textures/Interface/Misc/cross_hair.rsi"), _spriteName);
        return _entManager.EntitySysManager.GetEntitySystem<SpriteSystem>().Frame0(sprite);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var screen = args.ScreenHandle;

        // get positions
        var player = _player.LocalPlayer?.ControlledEntity;
        if (player == null ||
            !_entManager.TryGetComponent<TransformComponent>(player, out var xform))
        {
            return;
        }

        var mapPos = xform.MapPosition;
        if (mapPos.MapId == MapId.Nullspace)
            return;

        if (!_guns.TryGetGun(player.Value, out var gunUid, out var gun))
            return;

        var mouseScreen = _input.MouseScreenPosition;
        var mousePos = _eye.ScreenToMap(mouseScreen);
        if (mapPos.MapId != mousePos.MapId)
            return;

        // get collision mask
        int collisionMask;
        if (_entManager.TryGetComponent<HitscanBatteryAmmoProviderComponent>(
            gunUid, out var hitscan))
        {
            collisionMask = _protoManager.Index<HitscanPrototype>(hitscan.Prototype).CollisionMask;
        }
        else
        {
            collisionMask = (int) CollisionGroup.BulletImpassable;
        }


        // get raycast positions
        var direction = (mousePos.Position - mapPos.Position);
        var ray = new CollisionRay(mapPos.Position, direction.Normalized, collisionMask);
        var rayCastResults =
            _physics.IntersectRay(mapPos.MapId, ray, 20f, player, false).ToList();

        CrosshairType crosshairType = CrosshairType.Available;

        if (rayCastResults.Any())
        {
            RayCastResults result = rayCastResults[0];
            var mouseDistance = direction.Length;

            if (0.5 > (result.HitPos - mousePos.Position).Length)
            {
                crosshairType = CrosshairType.InTarget;
            }
            else if (mouseDistance > result.Distance)
            {
                crosshairType = CrosshairType.Unavailable;
                var screenHitPosition = _eye.WorldToScreen(result.HitPos);
                var screenPlayerPos = _eye.CoordinatesToScreen(xform.Coordinates).Position;
                DrawОbstacleSign(screen, _crosssign, screenHitPosition, screenPlayerPos);
            } 
        }

        DrawCrosshair(screen, _crosshair, mouseScreen.Position, crosshairType);
    }

    private UIBox2 GetBoxForTexture(Texture texture, Vector2 pos) {
        // Vector2 textureSize = textureSize.Size * scale;
        Vector2 textureSize = texture.Size;
        Vector2 halfTextureSize = textureSize / 2;
        Vector2 beginPos = pos - halfTextureSize;
        return UIBox2.FromDimensions(beginPos, textureSize);
    }

    private void DrawОbstacleSign(DrawingHandleScreen screen,
        Texture? cross, Vector2 hitPos, Vector2 playerPos)
    {
        if (cross == null)
            return;

        Vector2 bitHitDirection = playerPos + (hitPos - playerPos) * 0.8f;
        screen.DrawLine(bitHitDirection, hitPos, Color.Red);

        screen.DrawTextureRect(cross,
            GetBoxForTexture(cross, hitPos), Color.Red);
    }

    private void DrawCrosshair(DrawingHandleScreen screen,
        Texture? circle, Vector2 circlePos, CrosshairType type)
    {
        if (circle == null) {
            return;
        }

        Color color = type switch {
            CrosshairType.Unavailable => Color.Red,
            CrosshairType.InTarget => Color.LightGreen,
            _ => Color.DarkGreen,
        };
        screen.DrawTextureRect(circle,
            GetBoxForTexture(circle, circlePos), color);
    }
}

public enum CrosshairType : byte
{
    Available,
    Unavailable,
    InTarget,

}
