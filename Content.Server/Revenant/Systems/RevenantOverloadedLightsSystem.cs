using Content.Server.Beam;
using Content.Shared.Revenant.Components;
using Content.Shared.Revenant.Systems;

namespace Content.Server.Revenant.Systems;

public sealed class RevenantOverloadedLightsSystem : SharedRevenantOverloadedLightsSystem
{
    [Dependency] private readonly BeamSystem _beam = default!;

    protected override void OnZap(Entity<RevenantOverloadedLightsComponent> lights)
    {
        var component = lights.Comp;
        if (component.Target == null)
            return;

        var lxform = Transform(lights);
        var txform = Transform(component.Target.Value);

        if (!lxform.Coordinates.TryDistance(EntityManager, txform.Coordinates, out var distance))
            return;
        if (distance > component.ZapRange)
            return;

        _beam.TryCreateBeam(lights, component.Target.Value, component.ZapBeamEntityId);
    }
}
