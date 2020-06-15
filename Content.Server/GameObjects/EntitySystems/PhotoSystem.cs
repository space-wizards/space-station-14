using Content.Server.GameObjects.Components.Photography;
using Content.Shared.GameObjects.EntitySystemMessages;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    public class PhotoSystem : SharedPhotoSystem
    {
        /// <summary>
        /// Int used as photoIds for player created photos.
        /// </summary>
        private int _photoIdSource = 0;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<PhotoSystemMessages.RequestPhotoMessage>(RequestPhoto);
        }

        /// <summary>
        /// Stores data (PNG byte array) to disk as PNG in /Photos.
        /// </summary>
        /// <param name="data">PNG byte array of photo to save to disk.</param>
        /// <param name="photo">A PhotoComponent to assign the resulting photoId to.</param>
        /// <returns></returns>
        public async void StorePhoto(byte[] data, PhotoComponent photo)
        {
            var photoId = $"{_photoIdSource++}";
            photo.PhotoId = photoId;
            await StorePhotoImpl(data, photoId);
        }

        /// <summary>
        /// Send a requested photo as an array of PNG bytes to the client.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="eventArgs"></param>
        private void RequestPhoto(PhotoSystemMessages.RequestPhotoMessage request, EntitySessionEventArgs eventArgs)
        {
            var player = (IPlayerSession) eventArgs.SenderSession;
            var channel = player.ConnectedClient;

            if (TryGetPhotoBytes(request.PhotoId, out var photoBytes))
            {
                RaiseNetworkEvent(new PhotoSystemMessages.PhotoResponseMessage(photoBytes, true), channel);
            } else
            {
                RaiseNetworkEvent(new PhotoSystemMessages.PhotoResponseMessage(null, false), channel);
            }
        }

    }
}
