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
    /// Open the photo ui.
    /// Sets the photo to the message's data before opening it.
    /// </summary>
    [Serializable, NetSerializable]
    public class SetPhotoAndOpenUiMessage : ComponentMessage
    {
        public byte[] Data;
        public SetPhotoAndOpenUiMessage(byte[] data)
        {
            Data = data;
        }
    }

    /// <summary>
    /// Open the photo ui.
    /// Does not handle changing the photo.
    /// </summary>
    [Serializable, NetSerializable]
    public class OpenPhotoUiMessage : ComponentMessage
    {
        public OpenPhotoUiMessage()
        {
        }
    }
}
