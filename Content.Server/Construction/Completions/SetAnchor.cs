#nullable enable
using System.Threading.Tasks;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    public class SetAnchor : IGraphAction
    {
        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Value, "value", true);
        }

        public bool Value { get; private set; } = true;

        public async Task PerformAction(IEntity entity, IEntity? user)
        {
            if (!entity.TryGetComponent(out CollidableComponent? collidable)) return;

            collidable.Anchored = Value;
        }
    }
}
