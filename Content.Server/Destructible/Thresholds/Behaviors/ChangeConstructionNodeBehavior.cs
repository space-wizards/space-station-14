#nullable enable
using System;
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
