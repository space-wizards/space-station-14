using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;
using Robust.Server.GameObjects;
using XAEChargeBatteryComponent = Content.Server.Xenoarchaeology.Artifact.XAE.Components.XAEChargeBatteryComponent;

namespace Content.Server.Xenoarchaeology.Artifact.XAE;

public sealed class XAEChargeBatterySystem : BaseXAESystem<XAEChargeBatteryComponent>
{
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAEChargeBatteryComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        var chargeBatteryComponent = ent.Comp;

        var artifactCoordinates = _transform.GetMapCoordinates(ent);
        foreach (var battery in _lookup.GetEntitiesInRange<BatteryComponent>(artifactCoordinates, chargeBatteryComponent.Radius))
        {
            _battery.SetCharge(battery, battery.Comp.MaxCharge, battery);
        }
    }
}
