using Content.Shared.Xenoarchaeology.Artifact.Components;
using Robust.Shared.Containers;

namespace Content.Shared.Xenoarchaeology.Artifact;

public sealed partial class SharedXenoArtifactSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<XenoArtifactComponent, ComponentStartup>(OnStartup);

        InitializeNode();
    }

    private void OnStartup(Entity<XenoArtifactComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.NodeContainer = _container.EnsureContainer<Container>(ent, XenoArtifactComponent.NodeContainerId);
    }
}
