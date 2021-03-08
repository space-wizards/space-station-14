#nullable enable
using System;
using Content.Server.GameObjects.Components.Construction;
using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.Destructible.Thresholds.Behaviors
{
    [Serializable]
    [DataDefinition]
    public class ChangeConstructionNodeBehavior : IThresholdBehavior
    {
        [DataField("node")]
        public string Node { get; private set; } = string.Empty;

        public async void Execute(IEntity owner, DestructibleSystem system)
        {
            if (string.IsNullOrEmpty(Node) ||
                !owner.TryGetComponent(out ConstructionComponent? construction))
            {
                return;
            }

            await construction.ChangeNode(Node);
        }
    }
}
