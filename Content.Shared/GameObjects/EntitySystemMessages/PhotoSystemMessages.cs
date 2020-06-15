using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.EntitySystemMessages
{
    public static class PhotoSystemMessages
    {
        [Serializable, NetSerializable]
        public class RequestPhotoMessage : EntitySystemMessage
        {
            public readonly string PhotoId;

            public RequestPhotoMessage(string photoId)
            {
                PhotoId = photoId;
            }
        }

        [Serializable, NetSerializable]
        public class PhotoResponseMessage : EntitySystemMessage
        {
            public readonly byte[] PhotoBytes;
            public readonly bool Success;

            public PhotoResponseMessage(byte[] photoBytes, bool success)
            {
                PhotoBytes = photoBytes;
                Success = success;
            }
        }
    }
}
