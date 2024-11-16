using Content.Shared.Xenoarchaeology.Artifact.XAE.Components;

namespace Content.Shared.Xenoarchaeology.Artifact.XAE;

public sealed class XAEApplyComponentsSystem : BaseXAESystem<XAEApplyComponentsComponent>
{
    [Dependency] private IComponentFactory _componentFactory = default!;

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAEApplyComponentsComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        var artifact = args.Artifact;

        foreach (var (name, _) in ent.Comp.PermanentComponents)
        {
            var reg = _componentFactory.GetRegistration(name);

            if (EntityManager.HasComponent(artifact, reg.Type))
                continue;

            var comp = (Component)_componentFactory.GetComponent(reg);

            EntityManager.RemoveComponent(artifact, comp.GetType());
            EntityManager.AddComponent(artifact, comp);
        }
    }
}
