#nullable enable
using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

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
            public readonly FormattedMessage Message;

            public ExamineInfoResponseMessage(EntityUid entityUid, FormattedMessage message)
            {
                EntityUid = entityUid;
                Message = message;
            }
        }
    }
}
