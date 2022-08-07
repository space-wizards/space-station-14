using Content.Shared.Weapon.Melee;
using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client.Weapons.Melee;

public sealed class MeleeWindupOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    protected override void Draw(in OverlayDrawArgs args)
    {
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();

        foreach (var comp in _entManager.EntityQuery<ActiveNewMeleeWeaponComponent>())
        {
            if (!xformQuery.TryGetComponent(comp.Owner, out var xform) ||
                xform.MapID != args.MapId ||
                args.WorldAABB.Contains(xform.WorldPosition))
            {
                continue;
            }


        }
    }
}
