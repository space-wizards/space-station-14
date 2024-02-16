using Content.Shared.Weapons.Misc;
using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client.Weapons.Misc;

public sealed class TetherGunOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    private IEntityManager _entManager;

    public TetherGunOverlay(IEntityManager entManager)
    {
        _entManager = entManager;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var query = _entManager.EntityQueryEnumerator<TetheredComponent>();
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
        var tetherQuery = _entManager.GetEntityQuery<TetherGunComponent>();
        var forceQuery = _entManager.GetEntityQuery<ForceGunComponent>();
        var worldHandle = args.WorldHandle;
        var xformSystem = _entManager.System<SharedTransformSystem>();

        while (query.MoveNext(out var uid, out var tethered))
        {
            var gun = tethered.Tetherer;

            if (!xformQuery.TryGetComponent(gun, out var gunXform) ||
                !xformQuery.TryGetComponent(uid, out var xform))
            {
                continue;
            }

            if (xform.MapID != gunXform.MapID)
                continue;

            var worldPos = xformSystem.GetWorldPosition(xform, xformQuery);
            var gunWorldPos = xformSystem.GetWorldPosition(gunXform, xformQuery);
            var diff = worldPos - gunWorldPos;
            var angle = diff.ToWorldAngle();
            var length = diff.Length() / 2f;
            var midPoint = gunWorldPos + diff / 2;
            const float Width = 0.05f;

            var box = new Box2(-Width, -length, Width, length);
            var rotated = new Box2Rotated(box.Translated(midPoint), angle, midPoint);

            var color = Color.Red;

            if (forceQuery.TryGetComponent(tethered.Tetherer, out var force))
            {
                color = force.LineColor;
            }
            else if (tetherQuery.TryGetComponent(tethered.Tetherer, out var tether))
            {
                color = tether.LineColor;
            }

            worldHandle.DrawRect(rotated, color.WithAlpha(0.3f));
        }
    }
}
