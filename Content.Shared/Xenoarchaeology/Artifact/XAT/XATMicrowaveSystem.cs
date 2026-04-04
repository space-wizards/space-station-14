using Content.Shared.Kitchen;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT.Components;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT;

/// <summary>
/// System for checking if microwaved xenoartifact should be triggered.
/// </summary>
public sealed class XATMicrowaveSystem : BaseXATSystem<XATMicrowaveComponent>
{

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        XATSubscribeDirectEvent<BeingMicrowavedEvent>(OnMicrowaved);
    }

    private void OnMicrowaved(Entity<XenoArtifactComponent> artifact, Entity<XATMicrowaveComponent, XenoArtifactNodeComponent> node, ref BeingMicrowavedEvent args)
    {
        Trigger(artifact, node);
    }
}
