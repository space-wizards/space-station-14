using System;
using SS14.Shared.GameObjects;
using SS14.Shared.Serialization;

namespace Content.Shared.GameObjects.EntitySystemMessages
{
    public static class VerbSystemMessages
    {
        [Serializable, NetSerializable]
        public class RequestVerbsMessage : EntitySystemMessage
        {
            public readonly EntityUid EntityUid;

            public RequestVerbsMessage(EntityUid entityUid)
            {
                EntityUid = entityUid;
            }
        }


    }
}
