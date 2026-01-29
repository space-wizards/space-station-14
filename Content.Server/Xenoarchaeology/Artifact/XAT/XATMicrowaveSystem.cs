using Content.Server.Kitchen.Components;
using Content.Server.Xenoarchaeology.Artifact.XAT.Components;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT;


namespace Content.Server.Xenoarchaeology.Artifact.XAT;

/// <summary>
/// System for checking if microwaved xenoartifact should be triggered.
/// </summary>
public sealed class XATMicrowaveSystem : BaseXATSystem<XATMicrowaveComponent>
{

    [Dependency] private readonly SharedXenoArtifactSystem _artifactSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<XenoArtifactComponent, BeingMicrowavedEvent>(OnMicrowaved);
    }

    private void OnMicrowaved(EntityUid uid, XenoArtifactComponent component, BeingMicrowavedEvent args)
    {
        var nodes = _artifactSystem.GetAllNodes((uid, component));

        foreach (var node in nodes)
        {
            if (TryComp<XATMicrowaveComponent>(node, out var microwave) && CanTrigger((uid, component), node))
                Trigger((uid, component), (node.Owner, microwave, node.Comp));
        }
    }
}