using System.Linq;
using Content.Client.Weapons.Ranged.Systems;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
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

        CrosshairType crosshairType = CrosshairType.Available;

        var direction = (mousePos.Position - mapPos.Position);

        var uiScale = (args.ViewportControl as Control)?.UIScale ?? 1f;
        float limetedScale = uiScale > 1.25f ? 1.25f : uiScale;

        if (GetRayCastResult(direction, mapPos, player, collisionMask)
                is RayCastResults castRes)
        {
            var mouseDistance = direction.Length;

            if (0.5 > (castRes.HitPos - mousePos.Position).Length)
            {
                crosshairType = CrosshairType.InTarget;
            }
            else if (mouseDistance > castRes.Distance)
            {
                crosshairType = CrosshairType.Unavailable;
                var screenHitPosition = _eye.WorldToScreen(castRes.HitPos);
                var screenPlayerPos = _eye.CoordinatesToScreen(xform.Coordinates).Position;
                DrawОbstacleSign(screen, _crosssign, screenHitPosition, screenPlayerPos, limetedScale);
            }
        }

        DrawCrosshair(screen, _crosshair, mouseScreen.Position, limetedScale, crosshairType);
    }

    private RayCastResults? GetRayCastResult(Vector2 dir, MapCoordinates mapPos, EntityUid? player,
        int collision, float range = 20f)
    {
        var ray = new CollisionRay(mapPos.Position, dir.Normalized, collision);
        var rayCastResults =
            _physics.IntersectRay(mapPos.MapId, ray, 20f, player, false).ToList();

        if (rayCastResults.Any())
            return rayCastResults[0];
        else
            return null;
    }

    private UIBox2 GetBoxForTexture(Texture texture, Vector2 pos, float scale, float? expandedSize = null)
    {
        Vector2 textureSize = texture.Size * scale;
        if (expandedSize != null)
            textureSize += (float) expandedSize;
        Vector2 halfTextureSize = textureSize / 2;
        Vector2 beginPos = pos - halfTextureSize;
        return UIBox2.FromDimensions(beginPos, textureSize);
    }

    private void DrawОbstacleSign(DrawingHandleScreen screen,
        Texture? cross, Vector2 hitPos, Vector2 playerPos, float scale)
    {
        if (cross == null)
            return;

        Vector2 bitHitDirection = playerPos + (hitPos - playerPos) * 0.8f;
        screen.DrawLine(bitHitDirection, hitPos, Color.Red);

        screen.DrawTextureRect(cross,
            GetBoxForTexture(cross, hitPos, scale, 15), Color.Black);
        screen.DrawTextureRect(cross,
            GetBoxForTexture(cross, hitPos, scale), Color.Red);
    }

    private void DrawCrosshair(DrawingHandleScreen screen,
        Texture? circle, Vector2 circlePos, float scale, CrosshairType type)
    {
        if (circle == null)
        {
            return;
        }

        Color color = type switch
        {
            CrosshairType.Unavailable => Color.Red.WithAlpha(0.2f),
            CrosshairType.InTarget => Color.GreenYellow,
            _ => Color.LightGreen,
        };

        if (type == CrosshairType.Unavailable)
        {
            screen.DrawCircle(circlePos, 22 * scale, color);
        }
        else
        {
            screen.DrawTextureRect(circle,
                GetBoxForTexture(circle, circlePos, scale, 4), Color.Black);
            screen.DrawTextureRect(circle,
                GetBoxForTexture(circle, circlePos, scale), color);
        }

    }
}

public enum CrosshairType : byte
{
    Available,
    Unavailable,
    InTarget,
}
