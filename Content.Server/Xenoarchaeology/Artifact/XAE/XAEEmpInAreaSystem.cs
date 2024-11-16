using Content.Server.Emp;
using Content.Server.Xenoarchaeology.Artifact.XAE.Components;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;
using Robust.Server.GameObjects;

namespace Content.Server.Xenoarchaeology.Artifact.XAE;

public sealed class XAEEmpInAreaSystem : BaseXAESystem<XAEEmpInAreaComponent>
{
    [Dependency] private readonly EmpSystem _emp = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAEEmpInAreaComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        var artifactCoords = _transform.GetMapCoordinates(ent.Owner);

        _emp.EmpPulse(artifactCoords, ent.Comp.Range, ent.Comp.EnergyConsumption, ent.Comp.DisableDuration);
    }
}
