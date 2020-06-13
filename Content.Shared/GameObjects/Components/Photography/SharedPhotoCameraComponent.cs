using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Photography
{
    public abstract class SharedPhotoCameraComponent : Component
    {
        public override string Name => "PhotoCamera";
        public override uint? NetID => ContentNetIDs.PHOTO_CAMERA;
    }

    /// <summary>
    /// The user started taking a photo, this is an async process on the client
    /// So we'll play a "took photo sound" and then a looping "printing a photo" sound
    /// And notify the user that they're just waiting for the camera to print their photo.
    /// </summary>
    [Serializable, NetSerializable]
    public class TakingPhotoMessage : ComponentMessage
    {
        public TakingPhotoMessage()
        {
        }
    }



    /// <summary>
    /// Photo was taken clientside, upload image *SOMEWHERE* so others can access it *SOMEHOW*.
    /// Also makes the photo item.
    /// </summary>
    [Serializable, NetSerializable]
    public class TookPhotoMessage : ComponentMessage
    {
        public readonly EntityUid Author;
        public readonly byte[] Data;
        public readonly bool Suicide;
        public TookPhotoMessage(EntityUid author, byte[] data, bool suicide)
        {
            Author = author;
            Data = data;
            Suicide = suicide;
        }
    }

    /// <summary>
    /// Ask the client to take a "suicide selfie"
    /// This is as dumb as it sounds.
    /// </summary>
    [Serializable, NetSerializable]
    public class SuicideSelfieMessage : ComponentMessage
    {
        public readonly EntityUid Who;
        public SuicideSelfieMessage(EntityUid who)
        {
            Who = who;
        }
    }

    [Serializable, NetSerializable]
    public class PhotoCameraComponentState : ComponentState
    {
        public readonly bool On;
        public readonly int Radius;
        public readonly int Film;
        public readonly int FilmMax;
        public PhotoCameraComponentState(bool on, int radius, int film, int filmMax) : base(ContentNetIDs.PHOTO_CAMERA)
        {
            On = on;
            Radius = radius;
            Film = film;
            FilmMax = filmMax;
        }
    }

    [Serializable, NetSerializable]
    public enum PhotoUiKey
    {
        Key
    }
}
