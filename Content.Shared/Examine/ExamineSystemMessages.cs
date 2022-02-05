using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Examine
{
    public static class ExamineSystemMessages
    {
        [Serializable, NetSerializable]
        public class RequestExamineInfoMessage : EntityEventArgs
        {
            public readonly EntityUid EntityUid;

            public readonly bool GetVerbs;

            public RequestExamineInfoMessage(EntityUid entityUid, bool getVerbs=false)
            {
                EntityUid = entityUid;
                GetVerbs = getVerbs;
            }
        }

        [Serializable, NetSerializable]
        public class ExamineInfoResponseMessage : EntityEventArgs
        {
            public readonly EntityUid EntityUid;
            public readonly FormattedMessage Message;

            public readonly bool GetVerbs;

            public ExamineInfoResponseMessage(EntityUid entityUid, FormattedMessage message, bool getVerbs=false)
            {
                EntityUid = entityUid;
                Message = message;
                GetVerbs = getVerbs;
            }
        }
    }
}
