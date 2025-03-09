using Content.Server.GameTicking.Rules.VariationPass.Components;
using Content.Server.Power.Components;
using Content.Server.Wires;
using Content.Shared.Whitelist;
using Robust.Shared.Random;

namespace Content.Server.GameTicking.Rules.VariationPass;

/// <summary>
/// Handles unpowering stuff around the station.
/// This system identifies target devices and adds <see cref="UnpowerOnMapInitComponent"/> to them.
/// The actual wire cutting is handled by <see cref="CutWireOnMapInitSystem"/>.
/// </summary>
public sealed class UnpowerAllVariationPassSystem : VariationPassSystem<UnpowerAllVariationPassComponent>
{
    protected override void ApplyVariation(Entity<UnpowerAllVariationPassComponent> ent, ref StationVariationPassEvent args)
    {
        var query = AllEntityQuery<BatteryComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var transform))
        {
            // Ignore if not part of the station
            if (!IsMemberOfStation((uid, transform), ref args))
                continue;

            EnsureComp<UnpowerOnMapInitComponent>(uid);
        }
    }
}
