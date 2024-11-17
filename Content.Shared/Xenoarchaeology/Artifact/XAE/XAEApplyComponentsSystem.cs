using Content.Shared.Xenoarchaeology.Artifact.XAE.Components;

namespace Content.Shared.Xenoarchaeology.Artifact.XAE;

public sealed class XAEApplyComponentsSystem : BaseXAESystem<XAEApplyComponentsComponent>
{
    [Dependency] private IComponentFactory _componentFactory = default!;

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAEApplyComponentsComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        var artifact = args.Artifact;

        foreach (var registry in ent.Comp.PermanentComponents)
        {
            var clone = _componentFactory.GetComponent(registry.Value);
            EntityManager.AddComponent(artifact, clone, overwrite: true);
        }
    }
}
