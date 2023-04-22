using Content.Client.Weapons.Ranged.Systems;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged;
using Robust.Shared.Prototypes;

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

    public GunCrosshairOverlay(IEntityManager entManager, IEyeManager eyeManager,
        IGameTiming timing, IInputManager input, IPlayerManager player, IPrototypeManager prototypes, GunSystem system)
    {
        _entManager = entManager;
        _eye = eyeManager;
        _input = input;
        _timing = timing;
        _player = player;
        _guns = system;
        _protoManager = prototypes;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var worldHandle = args.WorldHandle;

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

        // use here CollisionGroup
        if (_entManager.TryGetComponent<HitscanBatteryAmmoProviderComponent>(
            gunUid, out var hitscan))
        {
            var collisionMask = _protoManager.Index<HitscanPrototype>(hitscan.Prototype).CollisionMask;
            Logger.Debug($"collision mask for hitscan {collisionMask}");
        }

        var mouseScreenPos = _input.MouseScreenPosition;
        var mousePos = _eye.ScreenToMap(mouseScreenPos);

        if (mapPos.MapId != mousePos.MapId)
            return;

        // (☞ﾟヮﾟ)☞
        // var maxSpread = gun.MaxAngle;
        // var minSpread = gun.MinAngle;
        // var timeSinceLastFire = (_timing.CurTime - gun.NextFire).TotalSeconds;
        // var currentAngle = new Angle(MathHelper.Clamp(gun.CurrentAngle.Theta - gun.AngleDecay.Theta * timeSinceLastFire,
        //     gun.MinAngle.Theta, gun.MaxAngle.Theta));
        var direction = (mousePos.Position - mapPos.Position);

        worldHandle.DrawLine(mapPos.Position, mousePos.Position + direction, Color.Orange);
    }
}
