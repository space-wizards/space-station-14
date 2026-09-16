using Content.Server.Emp;
using Content.Server.Xenoarchaeology.Artifact.XAE.Components;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;

namespace Content.Server.Xenoarchaeology.Artifact.XAE;

/// <summary>
/// System for xeno artifact effect that creates EMP on use.
/// </summary>
public sealed partial class XAEEmpInAreaSystem : BaseXAESystem<XAEEmpInAreaComponent>
{
    [Dependency] private EmpSystem _emp = default!;

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAEEmpInAreaComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        _emp.EmpPulse(args.Coordinates, ent.Comp.Range, ent.Comp.EnergyConsumption, ent.Comp.DisableDuration);
    }
}
