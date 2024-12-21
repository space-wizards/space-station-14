using Content.Shared.Xenoarchaeology.Artifact.XAE.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Xenoarchaeology.Artifact.XAE;

/// <summary>
/// System for applying component-registry when artifact effect is activated.
/// </summary>
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

        foreach (var registry in ent.Comp.Components)
        {
            if (!ent.Comp.ApplyIfAlreadyHave && EntityManager.HasComponent(artifact, registry.Value.Component.GetType()))
            {
                continue;
            }

            if (ent.Comp.RefreshOnReactivate)
            {
                EntityManager.RemoveComponent(artifact, registry.Value.Component.GetType());
            }

            var clone = _componentFactory.GetComponent(registry.Value);
            EntityManager.AddComponent(artifact, clone);
        }
    }
}
