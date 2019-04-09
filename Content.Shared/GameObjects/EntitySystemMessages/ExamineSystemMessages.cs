using System;
using SS14.Shared.GameObjects;
using SS14.Shared.Serialization;

namespace Content.Shared.GameObjects.EntitySystemMessages
{
    public static class ExamineSystemMessages
    {
        [Serializable, NetSerializable]
        public class RequestExamineInfoMessage : EntitySystemMessage
        {
            public readonly EntityUid EntityUid;

            public RequestExamineInfoMessage(EntityUid entityUid)
            {
                EntityUid = entityUid;
            }
        }

        [Serializable, NetSerializable]
        public class ExamineInfoResponseMessage : EntitySystemMessage
        {
            public readonly EntityUid EntityUid;
            public readonly string ExamineText;

            public ExamineInfoResponseMessage(EntityUid entityUid, string text)
            {
                EntityUid = entityUid;
                ExamineText = text;
            }
        }
    }
}
