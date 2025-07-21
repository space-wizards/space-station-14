using Content.Shared.Xenoarchaeology.Artifact.XAE.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Xenoarchaeology.Artifact.XAE;

/// <summary>
/// System for applying component-registry when artifact effect is activated.
/// </summary>
public sealed class XAEApplyComponentsSystem : BaseXAESystem<XAEApplyComponentsComponent>
{
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAEApplyComponentsComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var artifact = args.Artifact;

        foreach (var registry in ent.Comp.Components)
        {
            var componentType = registry.Value.Component.GetType();
            if (!ent.Comp.ApplyIfAlreadyHave && EntityManager.HasComponent(artifact, componentType))
            {
                continue;
            }

            if (ent.Comp.RefreshOnReactivate)
            {
                EntityManager.RemoveComponent(artifact, componentType);
            }

            var clone = EntityManager.ComponentFactory.GetComponent(registry.Value);
            EntityManager.AddComponent(artifact, clone);
        }
    }
}
