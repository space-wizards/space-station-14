using System.Threading.Tasks;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Server.Construction.Conditions
{
    [UsedImplicitly]
    public class EntityAnchored : IEdgeCondition
    {
        public bool Anchored { get; private set; }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Anchored, "anchored", true);
        }

        public async Task<bool> Condition(IEntity entity)
        {
            if (!entity.TryGetComponent(out ICollidableComponent collidable)) return false;

            return collidable.Anchored == Anchored;
        }

        public void DoExamine(IEntity entity, FormattedMessage message, bool inDetailsRange)
        {
            if (!entity.TryGetComponent(out ICollidableComponent collidable)) return;

            if(Anchored && !collidable.Anchored)
                message.AddMarkup("First, anchor it.\n");

            if(!Anchored && collidable.Anchored)
                message.AddMarkup("First, unanchor it.\n");
        }
    }
}
