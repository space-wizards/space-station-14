#nullable enable
using System;
using Content.Shared.GameObjects.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

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

        [Serializable, NetSerializable]
        public class VerbsResponseMessage : EntitySystemMessage
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
                public readonly bool Available;

                public NetVerbData(VerbData data, string key)
                {
                    Text = data.Text;
                    Key = key;
                    Category = data.Category;
                    CategoryIcon = data.CategoryIcon;
                    Icon = data.Icon;
                    Available = data.Visibility == VerbVisibility.Visible;
                }
            }
        }

        [Serializable, NetSerializable]
        public class UseVerbMessage : EntitySystemMessage
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
