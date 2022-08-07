using Content.Shared.Weapon.Melee;
using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client.Weapons.Melee;

public sealed class MeleeWindupOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private readonly SharedTransformSystem _transform = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    protected override void Draw(in OverlayDrawArgs args)
    {
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
        var handle = args.WorldHandle;

        foreach (var comp in _entManager.EntityQuery<ActiveNewMeleeWeaponComponent>())
        {
            if (!xformQuery.TryGetComponent(comp.Owner, out var xform) ||
                xform.MapID != args.MapId)
            {
                continue;
            }

            var worldPos = _transform.GetWorldPosition(xform, xformQuery);

            if (!args.WorldAABB.Contains(worldPos))
            {
                continue;
            }

            handle.DrawCircle(worldPos, 0.25f, Color.White.WithAlpha(0.25f));
        }
    }
}
