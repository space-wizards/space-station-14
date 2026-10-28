using Content.Shared.Xenoarchaeology.Artifact.XAE.Components;

namespace Content.Shared.Xenoarchaeology.Artifact.XAE;

/// <summary>
/// System for applying component-registry when artifact effect is activated.
/// </summary>
public sealed partial class XAEApplyComponentsSystem : BaseXAESystem<XAEApplyComponentsComponent>
{
    /// <inheritdoc />
    protected override void OnActivated(Entity<XAEApplyComponentsComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        var artifact = args.Artifact;
        foreach (var registry in ent.Comp.Components)
        {
            var componentType = registry.Value.Component.GetType();
            if (!ent.Comp.ApplyIfAlreadyHave && HasComp(artifact, componentType))
            {
                continue;
            }

            if (ent.Comp.RefreshOnReactivate)
            {
                RemComp(artifact, componentType);
            }

            var clone = EntityManager.ComponentFactory.GetComponent(registry.Value);
            AddComp(artifact, clone);
        }
    }
}
