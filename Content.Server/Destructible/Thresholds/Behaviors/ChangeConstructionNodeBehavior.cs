using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Shared.Destructible;
using Content.Shared.Destructible.Thresholds.Behaviors;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    [Serializable]
    [DataDefinition]
    public sealed partial class ChangeConstructionNodeBehavior : IThresholdBehavior
    {
        [DataField("node")]
        public string Node { get; private set; } = string.Empty;

        public void Execute(EntityUid owner, DestructibleBehaviorSystem system, EntityUid? cause = null)
        {
            var constructionSystem = system.EntityManager.System<ConstructionSystem>();

            if (string.IsNullOrEmpty(Node) || !system.EntityManager.TryGetComponent(owner, out ConstructionComponent? construction))
                return;

            constructionSystem.ChangeNode(owner, null, Node, true, construction);
        }
    }
}
