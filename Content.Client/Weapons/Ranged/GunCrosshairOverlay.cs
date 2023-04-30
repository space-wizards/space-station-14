using System.Linq;
using Content.Client.Weapons.Ranged.Systems;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Enums;
using Robust.Shared.Map;
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
    private readonly IInputManager _input;
    private readonly IPlayerManager _player;
    private readonly GunSystem _guns;
    private readonly IPrototypeManager _protoManager;
    private readonly SharedPhysicsSystem _physics;

    private Texture _crosshair;
    private Texture _crosshairMarked;
    private float _unavailableSignSize = 20f;
    private float _maxRange = 20f;

    public GunCrosshairOverlay(IEntityManager entManager, IEyeManager eyeManager, IInputManager input,
        IPlayerManager player, IPrototypeManager prototypes, SharedPhysicsSystem physics, GunSystem system)
    {
        _entManager = entManager;
        _eye = eyeManager;
        _input = input;
        _player = player;
        _guns = system;
        _protoManager = prototypes;
        _physics = physics;

        _crosshair = GetTextureFromRsi("crosshair");
        _crosshairMarked = GetTextureFromRsi("crosshair_marked");
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

        // get player position
        var player = _player.LocalPlayer?.ControlledEntity;
        if (player == null ||
            !_entManager.TryGetComponent<TransformComponent>(player, out var xform))
        {
            return;
        }

        var mapPos = xform.MapPosition;
        if (mapPos.MapId == MapId.Nullspace)
            return;

        // get gun
        if (!_guns.TryGetGun(player.Value, out var gunUid, out var gun))
            return;

        // get mouse position
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


        var uiScale = (args.ViewportControl as Control)?.UIScale ?? 1f;
        float limetedScale = uiScale > 1.25f ? 1.25f : uiScale;

        var direction = (mousePos.Position - mapPos.Position);

        // set data for calculating raycastResult
        DataForCalculatingRCGunResult raycastData;
        raycastData.MapPos = mapPos;
        raycastData.Player = player;
        raycastData.CollisionType = collisionMask;

        CrosshairType crosshairType = CrosshairType.Available;

        if (GetRayCastResult(direction, raycastData, _maxRange)
                is RayCastResults castRes)
        {
            var mouseDistance = direction.Length;

            // the target is not far from hit
            if (0.5 > (castRes.HitPos - mousePos.Position).Length)
            {
                // check bullet spread
                if (CheckBulletSpread(direction, castRes.Distance - 0.5f, raycastData, gun.MaxAngle))
                    crosshairType = CrosshairType.InTarget;
            }
            else if (mouseDistance > castRes.Distance)
            {
                crosshairType = CrosshairType.Unavailable;
                var screenHitPosition = _eye.WorldToScreen(castRes.HitPos);
                var screenPlayerPos = _eye.CoordinatesToScreen(xform.Coordinates).Position;
                // draw sign for obstacle
                DrawОbstacleSign(screen, screenHitPosition, screenPlayerPos, limetedScale);
            }
        }

        // draw crosshair for some type
        DrawCrosshair(screen, mouseScreen.Position, limetedScale, crosshairType);
    }

    private RayCastResults? GetRayCastResult(Vector2 dir, DataForCalculatingRCGunResult data,
        float maxRange)
    {
        var ray = new CollisionRay(data.MapPos.Position, dir.Normalized, data.CollisionType);
        var rayCastResults =
            _physics.IntersectRay(data.MapPos.MapId, ray, maxRange, data.Player, false).ToList();

        return rayCastResults.Any() ? rayCastResults[0] : null;
    }

    private bool CheckBulletSpread(Vector2 dir, float maxDistance, DataForCalculatingRCGunResult data,
        Angle angleSpread)
    {
        bool isRange = true;

        for (int i = 0; i < 2; i++)
        {
            var angle = (i == 0) ? angleSpread : (-angleSpread);

            if (GetRayCastResult(angle.RotateVec(dir), data, maxDistance) != null)
            {
                isRange = false;
                break;
            }
        }

        return isRange;
    }
    private void DrawОbstacleSign(DrawingHandleScreen screen,
        Vector2 hitPos, Vector2 playerPos, float scale)
    {
        Vector2 bitHitDirection = playerPos + (hitPos - playerPos) * 0.8f;
        screen.DrawLine(bitHitDirection, hitPos, Color.Red);

        float circleRadius = 2f * scale;

        screen.DrawCircle(hitPos, (circleRadius + 0.5f), Color.Black);
        screen.DrawCircle(hitPos, circleRadius, Color.Red);
    }

    private void DrawCrosshair(DrawingHandleScreen screen, Vector2 circlePos,
        float scale, CrosshairType type)
    {
        Color color = type switch
        {
            CrosshairType.Unavailable => Color.Red.WithAlpha(0.3f),
            CrosshairType.InTarget => Color.GreenYellow,
            _ => Color.PaleGreen,
        };

        if (type == CrosshairType.Unavailable)
        {
            var radius = _unavailableSignSize * scale;
            screen.DrawCircle(circlePos, radius + 1, Color.Black, false);
            screen.DrawCircle(circlePos, radius, color);
        }
        else
        {
            // active crosshair | not active
            var crosshair = type == CrosshairType.InTarget
                ? _crosshairMarked
                : _crosshair;

            screen.DrawTextureRect(crosshair,
                GetBoxForTexture(crosshair, circlePos, scale, 4), Color.Black);
            screen.DrawTextureRect(crosshair,
                GetBoxForTexture(crosshair, circlePos, scale), color);
        }
    }

    private UIBox2 GetBoxForTexture(Texture texture, Vector2 pos, float scale, float? expandedSize = null)
    {
        Vector2 textureSize = texture.Size * scale;
        if (expandedSize != null)
            textureSize += (float) expandedSize;
        Vector2 beginPos = pos - (textureSize / 2);
        return UIBox2.FromDimensions(beginPos, textureSize);
    }
}

public enum CrosshairType : byte
{
    Available,
    Unavailable,
    InTarget,
}

public struct DataForCalculatingRCGunResult
{
    public MapCoordinates MapPos;
    public EntityUid? Player;
    public int CollisionType;
}
