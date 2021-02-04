using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Body;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Destructible.Thresholds.Behavior
{
    [UsedImplicitly]
    public class GibBehavior : IThresholdBehavior
    {
        private bool _recursive = true;

        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _recursive, "recursive", true);
        }

        public void Trigger(IEntity owner, DestructibleSystem system)
        {
            if (owner.TryGetComponent(out IBody body))
            {
                body.Gib(_recursive);
            }
        }
    }
}
