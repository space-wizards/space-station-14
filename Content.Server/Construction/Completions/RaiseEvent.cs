using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    public class RaiseEvent : IGraphAction
    {
        [DataField("event", required:true)]
        public EntityEventArgs? Event { get; } = null;

        [DataField("directed")]
        public bool Directed { get; } = true;

        [DataField("broadcast")]
        public bool Broadcast { get; } = true;

        public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
        {
            if (Event == null || !Directed && !Broadcast)
                return;

            if(Directed)
                entityManager.EventBus.RaiseLocalEvent(uid, (object)Event, false);

            if(Broadcast)
                entityManager.EventBus.RaiseEvent(EventSource.Local, (object)Event);
        }
    }
}
