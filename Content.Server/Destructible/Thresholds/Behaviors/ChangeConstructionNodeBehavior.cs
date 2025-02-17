using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Shared.Destructible.Thresholds.Behaviors;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    [Serializable]
    [DataDefinition]
    public sealed partial class ChangeConstructionNodeBehavior : IThresholdBehavior
    {
        [DataField]
        public string Node { get; private set; } = string.Empty;

        public void Execute(EntityUid owner,
            IDependencyCollection collection,
            EntityManager entManager,
            EntityUid? cause = null)
        {
            if (string.IsNullOrEmpty(Node) || !entManager.TryGetComponent(owner, out ConstructionComponent? construction))
                return;

            entManager.System<ConstructionSystem>().ChangeNode(owner, null, Node, true, construction);
        }
    }
}
