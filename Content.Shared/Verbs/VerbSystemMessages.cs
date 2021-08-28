using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Verbs
{
    [Serializable, NetSerializable]
    public class RequestVerbsEvent : EntityEventArgs
    {
        public readonly EntityUid EntityUid;

        public RequestVerbsEvent(EntityUid entityUid)
        {
            EntityUid = entityUid;
        }
    }

    [Serializable, NetSerializable]
    public class VerbsResponseMessage : EntityEventArgs
    {
        public readonly Verb[] Verbs;
        public readonly EntityUid Entity;

        public VerbsResponseMessage(Verb[] verbs, EntityUid entity)
        {
            Verbs = verbs;
            Entity = entity;
        }
    }

    [Serializable, NetSerializable]
    public class UseVerbMessage : EntityEventArgs
    {
        public readonly EntityUid EntityUid;
        public readonly string VerbKey;

        public UseVerbMessage(EntityUid entityUid, string verbKey)
        {
            EntityUid = entityUid;
            VerbKey = verbKey;
        }
    }
}
