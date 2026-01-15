using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Shared.Destructible;
using Content.Shared.Destructible.Thresholds.Behaviors;

namespace Content.Server.Destructible.Thresholds.Behaviors;

[Serializable]
[DataDefinition]
public sealed partial class ChangeConstructionNodeBehavior : IThresholdBehavior
{
    [Dependency] private readonly ConstructionSystem _construction = default!;

    [DataField]
    public string Node { get; private set; } = string.Empty;

    public void Execute(EntityUid owner, SharedDestructibleSystem system, EntityUid? cause = null)
    {
        if (string.IsNullOrEmpty(Node) || !system.EntityManager.TryGetComponent(owner, out ConstructionComponent? construction))
            return;

        _construction.ChangeNode(owner, null, Node, true, construction);
    }
}
