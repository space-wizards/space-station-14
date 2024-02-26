using Content.Client.Weapons.Ranged.Systems;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.Weapons.Ranged;

public sealed class GunSpreadOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private IEntityManager _entManager;
    private readonly IEyeManager _eye;
    private readonly IGameTiming _timing;
    private readonly IInputManager _input;
    private readonly IPlayerManager _player;
    private readonly GunSystem _guns;

    public GunSpreadOverlay(IEntityManager entManager, IEyeManager eyeManager, IGameTiming timing, IInputManager input, IPlayerManager player, GunSystem system)
    {
        _entManager = entManager;
        _eye = eyeManager;
        _input = input;
        _timing = timing;
        _player = player;
        _guns = system;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var worldHandle = args.WorldHandle;

        var player = _player.LocalEntity;

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

        var mouseScreenPos = _input.MouseScreenPosition;
        var mousePos = _eye.PixelToMap(mouseScreenPos);

        if (mapPos.MapId != mousePos.MapId)
            return;

        // (☞ﾟヮﾟ)☞
        var maxSpread = gun.MaxAngleModified;
        var minSpread = gun.MinAngleModified;
        var timeSinceLastFire = (_timing.CurTime - gun.NextFire).TotalSeconds;
        var currentAngle = new Angle(MathHelper.Clamp(gun.CurrentAngle.Theta - gun.AngleDecayModified.Theta * timeSinceLastFire,
            gun.MinAngleModified.Theta, gun.MaxAngleModified.Theta));
        var direction = (mousePos.Position - mapPos.Position);

        worldHandle.DrawLine(mapPos.Position, mousePos.Position + direction, Color.Orange);

        // Show max spread either side
        worldHandle.DrawLine(mapPos.Position, mousePos.Position + maxSpread.RotateVec(direction), Color.Red);
        worldHandle.DrawLine(mapPos.Position, mousePos.Position + (-maxSpread).RotateVec(direction), Color.Red);

        // Show min spread either side
        worldHandle.DrawLine(mapPos.Position, mousePos.Position + minSpread.RotateVec(direction), Color.Green);
        worldHandle.DrawLine(mapPos.Position, mousePos.Position + (-minSpread).RotateVec(direction), Color.Green);

        // Show current angle
        worldHandle.DrawLine(mapPos.Position, mousePos.Position + currentAngle.RotateVec(direction), Color.Yellow);
        worldHandle.DrawLine(mapPos.Position, mousePos.Position + (-currentAngle).RotateVec(direction), Color.Yellow);
    }
}
