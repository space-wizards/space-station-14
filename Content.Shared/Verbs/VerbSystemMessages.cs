using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Verbs
{
    public static class VerbSystemMessages
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
            public readonly NetVerbData[] Verbs;
            public readonly EntityUid Entity;

            public VerbsResponseMessage(NetVerbData[] verbs, EntityUid entity)
            {
                Verbs = verbs;
                Entity = entity;
            }

            [Serializable, NetSerializable]
            public readonly struct NetVerbData
            {
                public readonly string Text;
                public readonly string Key;
                public readonly string Category;
                public readonly SpriteSpecifier? Icon;
                public readonly SpriteSpecifier? CategoryIcon;
                public readonly bool IsDisabled;

                public NetVerbData(Verb verb)
                {
                    Text = verb.Text;
                    Key = verb.Key;
                    Category = verb.Category;
                    CategoryIcon = verb.CategoryIcon;
                    Icon = verb.Icon;
                    IsDisabled = verb.IsDisabled;
                }
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
}
