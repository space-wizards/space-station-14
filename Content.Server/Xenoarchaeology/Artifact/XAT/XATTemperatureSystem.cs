using Content.Server.Atmos.EntitySystems;
using Content.Server.Xenoarchaeology.Artifact.XAT.Components;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT;

namespace Content.Server.Xenoarchaeology.Artifact.XAT;

public sealed class XATTemperatureSystem : BaseXATSystem<XATTemperatureComponent>
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;

    protected override void UpdateXAT(Entity<XenoArtifactComponent> artifact, Entity<XATTemperatureComponent, XenoArtifactNodeComponent> node, float frameTime)
    {
        base.UpdateXAT(artifact, node, frameTime);

        var xform = Transform(artifact);

        if (_atmosphere.GetTileMixture((artifact, xform)) is not { } mixture)
            return;

        var curTemp = mixture.Temperature;

        if (node.Comp1.TriggerOnHigherTemp)
        {
            if (curTemp >= node.Comp1.TargetTemperature)
                Trigger(artifact, node);
        }
        else
        {
            if (curTemp <= node.Comp1.TargetTemperature)
                Trigger(artifact, node);
        }
    }
}
