#nullable enable
using System;
using Content.Server.GameObjects.Components.Construction;
using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Destructible.Thresholds.Behaviors
{
    [Serializable]
    public class ChangeConstructionNodeBehavior : IThresholdBehavior
    {
        public string Node { get; private set; } = string.Empty;

        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Node, "node", string.Empty);
        }

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
