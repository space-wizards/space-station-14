using Content.Server.Beam;
using Content.Shared.Revenant.Components;
using Content.Shared.Revenant.EntitySystems;

namespace Content.Server.Revenant.EntitySystems;

/// <summary>
/// This handles...
/// </summary>
public sealed class RevenantOverloadedLightsSystem : SharedRevenantOverloadedLightsSystem
{
    [Dependency] private readonly BeamSystem _beam = default!;

    protected override void OnZap(RevenantOverloadedLightsComponent component)
    {
        if (component.Target == null)
            return;

        var lxform = Transform(component.Owner);
        var txform = Transform(component.Target.Value);

        if (!lxform.Coordinates.TryDistance(EntityManager, txform.Coordinates, out var distance))
            return;
        if (distance > component.ZapRange)
            return;

        _beam.TryCreateBeam(component.Owner, component.Target.Value, component.ZapBeamEntityId);
    }
}
