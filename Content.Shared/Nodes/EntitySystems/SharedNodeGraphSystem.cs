using Content.Shared.Nodes.Components;

namespace Content.Shared.Nodes.EntitySystems;

/// <summary>
/// </summary>
public abstract partial class SharedNodeGraphSystem : EntitySystem
{
    protected EntityQuery<GraphNodeComponent> NodeQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        NodeQuery = GetEntityQuery<GraphNodeComponent>();

        SubscribeLocalEvent<GraphNodeComponent, ComponentShutdown>(OnComponentShutdown);
    }
}
