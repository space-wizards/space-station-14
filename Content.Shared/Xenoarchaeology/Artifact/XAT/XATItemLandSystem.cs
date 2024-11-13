using Content.Shared.Throwing;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT.Components;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT;

public sealed class XATItemLandSystem : BaseXATSystem<XATItemLandComponent>
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        XATSubscribeDirectEvent<LandEvent>(OnLand);
    }

    private void OnLand(Entity<XenoArtifactComponent> artifact, Entity<XATItemLandComponent, XenoArtifactNodeComponent> node, ref LandEvent args)
    {
        Trigger(artifact, node);
    }
}
