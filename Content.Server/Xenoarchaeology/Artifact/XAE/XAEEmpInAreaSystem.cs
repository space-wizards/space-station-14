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
        var range = ent.Comp.Range;
        if (args.Modifications.TryGetValue(XenoArtifactEffectModifier.Range, out var rangeModifier))
            range = Math.Max(4, rangeModifier.Modify(range));

        var duration = ent.Comp.DisableDuration;
        if (args.Modifications.TryGetValue(XenoArtifactEffectModifier.Duration, out var durationModifier))
            duration = Math.Max(1, durationModifier.Modify(duration));

        _emp.EmpPulse(args.Coordinates, range, ent.Comp.EnergyConsumption, duration);
    }
}
