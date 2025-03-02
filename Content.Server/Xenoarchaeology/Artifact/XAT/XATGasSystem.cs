using Content.Server.Atmos.EntitySystems;
using Content.Server.Xenoarchaeology.Artifact.XAT.Components;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT;

namespace Content.Server.Xenoarchaeology.Artifact.XAT;

public sealed class XATGasSystem : BaseQueryUpdateXATSystem<XATGasComponent>
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;

    protected override void UpdateXAT(Entity<XenoArtifactComponent> artifact, Entity<XATGasComponent, XenoArtifactNodeComponent> node, float frameTime)
    {
        var xform = Transform(artifact);

        if (_atmosphere.GetTileMixture((artifact, xform)) is not { } mixture)
            return;

        var moles = mixture.GetMoles(node.Comp1.TargetGas);

        if (node.Comp1.ShouldBePresent)
        {
            if (moles >= node.Comp1.Moles)
                Trigger(artifact, node);
        }
        else
        {
            if (moles <= node.Comp1.Moles)
                Trigger(artifact, node);
        }
    }
}
