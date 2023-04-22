using System.Linq;
using Content.Client.Weapons.Ranged.Systems;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Robust.Shared.Prototypes;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Physics;

namespace Content.Client.Weapons.Ranged;

public sealed class GunCrosshairOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private IEntityManager _entManager;
    private readonly IEyeManager _eye;
    private readonly IGameTiming _timing;
    private readonly IInputManager _input;
    private readonly IPlayerManager _player;
    private readonly GunSystem _guns;
    private readonly IPrototypeManager _protoManager;
    private readonly SharedPhysicsSystem _physics;

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
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        DrawingHandleWorld worldHandle = args.WorldHandle;

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

        var mouseScreenPos = _input.MouseScreenPosition;
        var mousePos = _eye.ScreenToMap(mouseScreenPos);

        if (mapPos.MapId != mousePos.MapId)
            return;

        var direction = (mousePos.Position - mapPos.Position);

        var ray = new CollisionRay(mapPos.Position, direction.Normalized, collisionMask);
        var rayCastResults =
            _physics.IntersectRay(mapPos.MapId, ray, 20f, player, false).ToList();

        Color crosshairColor = Color.DarkGreen;

        if (rayCastResults.Any())
        {
            RayCastResults result = rayCastResults[0];
            var mouseDistance = direction.Length;

            if (1 > (result.HitPos - mousePos.Position).Length)
            {
                crosshairColor = Color.LightGreen;
            }
            else if (mouseDistance > result.Distance)
            {
                DrawОbstacleSign(worldHandle, result.HitPos);
                crosshairColor = Color.Red;
            }
        }

        DrawCrosshair(worldHandle, mousePos.Position, crosshairColor);
        worldHandle.DrawLine(mapPos.Position, mousePos.Position + direction, Color.Orange);
    }

    private void DrawОbstacleSign(DrawingHandleWorld handle, Vector2 posCircle)
    {
        float radius = 1f;
        handle.DrawCircle(posCircle, radius, Color.Red.WithAlpha(0.75f));
    }

    private void DrawCrosshair(DrawingHandleWorld handle, Vector2 posCircle, Color color)
    {
        handle.DrawCircle(posCircle, 0.5f, color);
    }
}
