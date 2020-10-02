#nullable enable
using System.Threading.Tasks;
using Content.Shared.Construction;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.Construction.Completions
{
    public class SetAnchor : IEdgeCompleted, IStepCompleted
    {
        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Value, "value", true);
        }

        public bool Value { get; private set; }

        public async Task StepCompleted(IEntity entity, IEntity user)
        {
            await Completed(entity, user);
        }

        public async Task Completed(IEntity entity, IEntity user)
        {
            if (!entity.TryGetComponent(out CollidableComponent? collidable)) return;

            collidable.Anchored = Value;
        }
    }
}
