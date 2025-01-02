using Content.Server.Atmos.EntitySystems;
using Content.Server.Xenoarchaeology.Artifact.XAT.Components;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT;

namespace Content.Server.Xenoarchaeology.Artifact.XAT;

public sealed class XATTemperatureSystem : BaseQueryUpdateXATSystem<XATTemperatureComponent>
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;

    protected override void UpdateXAT(Entity<XenoArtifactComponent> artifact, Entity<XATTemperatureComponent, XenoArtifactNodeComponent> node, float frameTime)
    {
        var xform = Transform(artifact);

        if (_atmosphere.GetTileMixture((artifact, xform)) is not { } mixture)
            return;

        var curTemp = mixture.Temperature;

        var temperatureTriggerComponent = node.Comp1;
        if (temperatureTriggerComponent.TriggerOnHigherTemp)
        {
            if (curTemp >= temperatureTriggerComponent.TargetTemperature)
                Trigger(artifact, node);
        }
        else
        {
            if (curTemp <= temperatureTriggerComponent.TargetTemperature)
                Trigger(artifact, node);
        }
    }
}
