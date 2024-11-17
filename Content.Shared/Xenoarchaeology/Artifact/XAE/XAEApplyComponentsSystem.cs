using Content.Shared.Xenoarchaeology.Artifact.XAE.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Xenoarchaeology.Artifact.XAE;

public sealed class XAEApplyComponentsSystem : BaseXAESystem<XAEApplyComponentsComponent>
{
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAEApplyComponentsComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var artifact = args.Artifact;

        foreach (var registry in ent.Comp.PermanentComponents)
        {
            var clone = _componentFactory.GetComponent(registry.Value);
            EntityManager.AddComponent(artifact, clone, overwrite: true);
        }
    }
}
