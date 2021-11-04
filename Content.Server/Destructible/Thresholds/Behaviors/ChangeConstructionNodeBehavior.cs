using System;
using Content.Server.Construction;
using Content.Server.Construction.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    [Serializable]
    [DataDefinition]
    public class ChangeConstructionNodeBehavior : IThresholdBehavior
    {
        [DataField("node")]
        public string Node { get; private set; } = string.Empty;

        public void Execute(IEntity owner, DestructibleSystem system)
        {
            if (string.IsNullOrEmpty(Node) || !owner.TryGetComponent(out ConstructionComponent? construction))
                return;

            EntitySystem.Get<ConstructionSystem>().ChangeNode(owner.Uid, null, Node, true, construction);
        }
    }
}
