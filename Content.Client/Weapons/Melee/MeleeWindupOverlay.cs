using Content.Shared.Weapons.Melee;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Timing;

namespace Content.Client.Weapons.Melee;

public sealed class MeleeWindupOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    private readonly SharedTransformSystem _transform;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public MeleeWindupOverlay()
    {
        IoCManager.InjectDependencies(this);
        _transform = _entManager.EntitySysManager.GetEntitySystem<SharedTransformSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
        var tickTime = (float) _timing.TickPeriod.TotalSeconds;
        var tickFraction = _timing.TickFraction / (float) ushort.MaxValue * tickTime;

        foreach (var comp in _entManager.EntityQuery<MeleeWeaponComponent>())
        {
            if (comp.WindupAccumulator < SharedMeleeWeaponSystem.AttackBuffer)
                continue;

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

            var fraction = (comp.WindupAccumulator + SharedMeleeWeaponSystem.GracePeriod - SharedMeleeWeaponSystem.AttackBuffer) / (comp.WindupTime - SharedMeleeWeaponSystem.AttackBuffer);

            var lerp = fraction.Equals(0f) ? 0f : tickFraction;
            var sign = comp.Accumulating ? 1 : -1;
            var lerpedFraction = MathF.Min(1f, (fraction + lerp * sign));

            Color color;

            if (lerpedFraction.Equals(1f))
            {
                color = Color.Orange;
            }
            else
            {
                color = Color.White;
            }

            handle.DrawCircle(worldPos, 0.25f * lerpedFraction, color.WithAlpha(0.25f));
        }
    }
}
