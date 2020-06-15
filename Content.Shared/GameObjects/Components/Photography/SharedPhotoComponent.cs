using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared.GameObjects.Components.Photography
{
    public class SharedPhotoComponent : Component
    {
        public override string Name => "Photo";

        public override uint? NetID => ContentNetIDs.PHOTO;
    }

    /// <summary>
    /// Open the photo UI displaying the appropriate photo known as photoId.
    /// Client may request the photo from the server if it doesn't already have it cached.
    /// </summary>
    [Serializable, NetSerializable]
    public class OpenPhotoUiMessage : ComponentMessage
    {
        public readonly string PhotoId;
        public OpenPhotoUiMessage(string photoId)
        {
            PhotoId = photoId;
        }
    }
}
