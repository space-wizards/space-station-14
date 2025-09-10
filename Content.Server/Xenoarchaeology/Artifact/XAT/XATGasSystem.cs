using Content.Server.Atmos.EntitySystems;
using Content.Server.Xenoarchaeology.Artifact.XAT.Components;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT;

namespace Content.Server.Xenoarchaeology.Artifact.XAT;

/// <summary>
/// System for xeno artifact trigger, which gets activated from some gas being on the same time as artifact with certain concentration.
/// </summary>
public sealed class XATGasSystem : BaseQueryUpdateXATSystem<XATGasComponent>
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;

    protected override void UpdateXAT(Entity<XenoArtifactComponent> artifact, Entity<XATGasComponent, XenoArtifactNodeComponent> node, float frameTime)
    {
        var xform = Transform(artifact);

        if (_atmosphere.GetTileMixture((artifact, xform)) is not { } mixture)
            return;

        var gasTrigger = node.Comp1;
        var moles = mixture.GetMoles(gasTrigger.TargetGas);

        if (gasTrigger.ShouldBePresent)
        {
            if (moles >= gasTrigger.Moles)
                Trigger(artifact, node);
        }
        else
        {
            if (moles <= gasTrigger.Moles)
                Trigger(artifact, node);
        }
    }
}
