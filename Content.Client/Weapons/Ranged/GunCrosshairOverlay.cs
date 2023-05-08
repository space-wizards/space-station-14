using System.Linq;
using Content.Client.Weapons.Ranged.Systems;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.State;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Utility;
using Content.Client.Gameplay;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Physics;

namespace Content.Client.Weapons.Ranged;

/// <summary>
/// This crosshair near mouse pointer helps with aiming.
/// When there are obstacles and the projectile cannot reach the target (mouse pointer),
/// it turns red. Light green, when the projectile hits the target precisely.
/// Use IntersectRay.
/// </summary>
public sealed class GunCrosshairOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    private readonly IEntityManager _entManager;
    private readonly IEyeManager _eye;
    private readonly IInputManager _input;
    private readonly IPlayerManager _player;
    private readonly GunSystem _guns;
    private readonly IPrototypeManager _protoManager;
    private readonly SharedPhysicsSystem _physics;
    private readonly IStateManager _stateManager;
    private readonly Texture _crosshair;
    private readonly Texture _crosshairMarked;
    private readonly float _unavailableSignSize = 22f;

    public float? MaxRange; // in default maxRange get for centered player
    public float MainScale = 0.72f; // for set see CombatModeIndicatorsOverlay
    public float MaxExpectedBulletSpread = 0.3f;
    public float SanctuaryCircleCoeff = 0.1f; // circle of sanctuary, awoid those who are nearby

    public GunCrosshairOverlay(IEntityManager entManager, IEyeManager eyeManager, IInputManager input,
        IPlayerManager player, IPrototypeManager prototypes, SharedPhysicsSystem physics,
        IStateManager stManager, GunSystem system)
    {
        _entManager = entManager;
        _eye = eyeManager;
        _input = input;
        _player = player;
        _guns = system;
        _protoManager = prototypes;
        _physics = physics;
        _stateManager = stManager;

        var spriteSys = _entManager.EntitySysManager.GetEntitySystem<SpriteSystem>();
        _crosshair = spriteSys.Frame0(new SpriteSpecifier.Rsi(
            new ResPath("/Textures/Interface/Misc/crosshair_pointers.rsi"), "crosshair"));
        _crosshairMarked = spriteSys.Frame0(new SpriteSpecifier.Rsi(
            new ResPath("/Textures/Interface/Misc/crosshair_pointers.rsi"), "crosshair_marked"));
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var screen = args.ScreenHandle;

        var player = _player.LocalPlayer?.ControlledEntity;

        // get gun
        if (player == null || !_guns.TryGetGun(player.Value, out var gunUid, out var gun))
            return;

        // get player position
        if (!_entManager.TryGetComponent<TransformComponent>(player, out var xform))
            return;
        var playerMapPos = xform.MapPosition;
        if (playerMapPos.MapId != args.MapId)
            return;

        // get mouse position
        var mouseScreen = _input.MouseScreenPosition;
        var mousePos = _eye.ScreenToMap(mouseScreen);
        if (mousePos.MapId != args.MapId)
            return;

        // get collision mask
        int collisionMask;
        if (_entManager.TryGetComponent<HitscanBatteryAmmoProviderComponent>(gunUid, out var hitscan))
            collisionMask = _protoManager.Index<HitscanPrototype>(hitscan.Prototype).CollisionMask;
        else
            collisionMask = (int) CollisionGroup.BulletImpassable;

        var uiScale = (args.ViewportControl as Control)?.UIScale ?? 1f;
        var scale = (uiScale > 1.25f ? 1.25f : uiScale) * MainScale;

        var maxAngle = gun.MaxAngle;

        var worldViewport = _eye.GetWorldViewport();
        var maxRange = MaxRange ?? (float) (0.3 * (worldViewport.Width + worldViewport.Height));

        var sanctuaryPos = playerMapPos.Position
             - (mousePos.Position - playerMapPos.Position).Normalized * SanctuaryCircleCoeff;
        var direction = mousePos.Position - sanctuaryPos;

        // set data for calculating raycastResult
        GunCrosshairDataForCalculatingRCResult raycastData;
        raycastData.MapPos = new MapCoordinates(sanctuaryPos, playerMapPos.MapId);
        raycastData.Player = player;
        raycastData.CollisionType = collisionMask;
        raycastData.MaxRange = maxRange;

        // when it doesn't have any obstacles
        var crosshairType = GunCrosshairTypes.Available;

        // find rayCast for main gun vector
        if (GetRayCastResult(direction, raycastData) is RayCastResults castRes)
        {
            var mouseDistance = direction.Length;
            var currentState = _stateManager.CurrentState;

            // use entityUid for an mouse over object if you can found it
            if (currentState is GameplayStateBase tickScreen
                && tickScreen.GetClickedEntity(mousePos) is EntityUid objectEntityUid
                    && castRes.HitEntity == objectEntityUid
                        && CheckBulletSpread(objectEntityUid, direction, raycastData, maxAngle))
            {
                crosshairType = GunCrosshairTypes.InTarget;
            }
            // the wall or glass is not far from hit
            else if (MaxExpectedBulletSpread > (castRes.HitPos - mousePos.Position).Length
                && CheckBulletSpread(castRes.Distance - MaxExpectedBulletSpread, direction, raycastData, maxAngle))
            {
                crosshairType = GunCrosshairTypes.InTarget;
            }
            // if it have some obstacle
            else if (mouseDistance > castRes.Distance)
            {
                crosshairType = GunCrosshairTypes.Unavailable;
                var screenHitPosition = _eye.WorldToScreen(castRes.HitPos);
                var screenPlayerPos = _eye.CoordinatesToScreen(xform.Coordinates).Position;
                DrawОbstacleSign(screen, screenHitPosition, screenPlayerPos, scale);
            }
        }

        // draw crosshair for some type
        DrawCrosshair(screen, mouseScreen.Position, scale, crosshairType);
    }

    private RayCastResults? GetRayCastResult(Vector2 dir, GunCrosshairDataForCalculatingRCResult data,
        float? maxRange = null)
    {
        var ray = new CollisionRay(data.MapPos.Position, dir.Normalized, data.CollisionType);
        var rayCastResults = _physics.IntersectRay(
            data.MapPos.MapId, ray, maxRange ?? data.MaxRange, data.Player, false
            ).ToList();

        return rayCastResults.Any() ? rayCastResults[0] : null;
    }

    private bool CheckBulletSpread(EntityUid selectedEntityUid,
        Vector2 dir, GunCrosshairDataForCalculatingRCResult data, Angle angleSpread)
    {
        for (var i = 0; i < 2; i++)
        {
            var angle = (i == 0) ? angleSpread : (-angleSpread);

            var rayCastRes = GetRayCastResult(angle.RotateVec(dir), data);
            if (rayCastRes is not RayCastResults castResuslt || castResuslt.HitEntity != selectedEntityUid)
                return false;
        }

        return true;
    }

    private bool CheckBulletSpread(float maxDistance, Vector2 dir, GunCrosshairDataForCalculatingRCResult data,
        Angle angleSpread)
    {
        for (var i = 0; i < 2; i++)
        {
            var angle = (i == 0) ? angleSpread : (-angleSpread);
            if (GetRayCastResult(angle.RotateVec(dir), data, maxDistance) != null)
                return false;
        }

        return true;
    }
    private static void DrawОbstacleSign(DrawingHandleScreen screen,
        Vector2 hitPos, Vector2 playerPos, float scale)
    {
        screen.DrawLine(playerPos + (hitPos - playerPos) * 0.8f, hitPos, Color.Red);
        var circleRadius = 2f * scale;

        screen.DrawCircle(hitPos, circleRadius + 0.5f, Color.Black);
        screen.DrawCircle(hitPos, circleRadius, Color.Red);
    }

    private void DrawCrosshair(DrawingHandleScreen screen, Vector2 circlePos,
        float scale, GunCrosshairTypes type)
    {
        var color = type switch
        {
            GunCrosshairTypes.Unavailable => Color.Red.WithAlpha(0.3f),
            GunCrosshairTypes.InTarget => Color.GreenYellow,
            _ => Color.Green,
        };

        if (type == GunCrosshairTypes.Unavailable)
        {
            screen.DrawCircle(circlePos, _unavailableSignSize * scale, color);
        }
        else
        {
            // active crosshair | not active
            var crosshair = type == GunCrosshairTypes.InTarget
                ? _crosshairMarked : _crosshair;

            var textureSize = crosshair.Size * scale;

            screen.DrawTextureRect(crosshair,
                UIBox2.FromDimensions(circlePos - textureSize * 0.5f, textureSize), color);
        }
    }
}

public enum GunCrosshairTypes : byte
{
    Available,
    Unavailable,
    InTarget,
}

public struct GunCrosshairDataForCalculatingRCResult
{
    public MapCoordinates MapPos;
    public EntityUid? Player;
    public int CollisionType;
    public float MaxRange;
}
