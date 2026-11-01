using Content.Shared.Xenoarchaeology.Artifact.XAE.Components;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.Xenoarchaeology.Artifact.XAE;

/// <summary>
/// System for removing the ArtifactComponent when artifact effect is activated.
/// </summary>
public sealed class XAERemoveArtifactSystem : BaseXAESystem<XAERemoveArtifactComponent>
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private INetManager _net = default!;

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAERemoveArtifactComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (_net.IsClient)
            return;

        _entityManager.RemoveComponentDeferred<XenoArtifactNodeComponent>(args.Node.Owner); //kill the node so it stops updating
        _entityManager.RemoveComponentDeferred<XenoArtifactComponent>(args.Artifact.Owner); //kill the artifact so it stops reacting
    }
}
