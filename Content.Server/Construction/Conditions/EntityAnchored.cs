using System.Threading.Tasks;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Server.Construction.Conditions
{
    [UsedImplicitly]
    public class EntityAnchored : IEdgeCondition
    {
        public bool Anchored { get; private set; }

        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Anchored, "anchored", true);
        }

        public async Task<bool> Condition(IEntity entity)
        {
            if (!entity.TryGetComponent(out IPhysicsComponent physics)) return false;

            return physics.Anchored == Anchored;
        }

        public bool DoExamine(IEntity entity, FormattedMessage message, bool inDetailsRange)
        {
            if (!entity.TryGetComponent(out IPhysicsComponent physics)) return false;

            switch (Anchored)
            {
                case true when !physics.Anchored:
                    message.AddMarkup("First, anchor it.\n");
                    return true;
                case false when physics.Anchored:
                    message.AddMarkup("First, unanchor it.\n");
                    return true;
            }

            return false;
        }
    }
}
