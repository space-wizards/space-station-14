using Content.Client.GameObjects.Components.Photography;
using Content.Client.Interfaces.GameObjects;
using Content.Shared.GameObjects.EntitySystemMessages;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Client.Interfaces.Input;
using Robust.Client.Player;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.Resources;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Content.Client.GameObjects.EntitySystems
{
    /// <summary>
    /// Manages
    /// - Requesting photos from the server
    /// - Caching photos to disk
    /// - Input management for click-to-take-photo
    /// </summary>
    public class PhotoSystem : SharedPhotoSystem
    {

#pragma warning disable 649
        [Dependency] private readonly IGameTiming _gameTiming = default;
        [Dependency] private readonly IInputManager _inputManager = default;
        [Dependency] private readonly IPlayerManager _playerManager = default;
        [Dependency] private readonly IResourceManager _resourceManager = default;
#pragma warning restore 649

        /// <summary>
        /// Dictionary mapping photoIds to the CancellationTokenSource for the request to download the photo.
        /// </summary>
        private Dictionary<string, CancellationTokenSource> _photoRequestTokens = new Dictionary<string, CancellationTokenSource>();

        private InputSystem _inputSystem;
        private PhotoSystem _photoSystem;
        private bool _blocked;
        private bool _down;

        public override void Initialize()
        {
            base.Initialize();

            IoCManager.InjectDependencies(this);
            _inputSystem = Get<InputSystem>();
            _photoSystem = Get<PhotoSystem>();
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (!_gameTiming.IsFirstTimePredicted)
            {
                return;
            }

            var state = _inputSystem.CmdStates.GetState(EngineKeyFunctions.Use);
            if (state != BoundKeyState.Down)
            {
                _blocked = false;
                _down = false;
                return;
            }

            //Rapid fire photos - no thanks
            if (_down)
            {
                return;
            }
            _down = true;

            var entity = _playerManager.LocalPlayer.ControlledEntity;
            if (entity == null || !entity.TryGetComponent(out IHandsComponent hands))
            {
                _blocked = true;
                return;
            }

            var held = hands.ActiveHand;
            if (held == null || !held.TryGetComponent(out PhotoCameraComponent camera))
            {
                _blocked = true;
                return;
            }

            if (_blocked)
            {
                return;
            }

            camera.TryTakePhoto(entity.Uid, _inputManager.MouseScreenPosition);
        }

        /// <summary>
        /// Stores data (PNG byte array) to disk as PNG in /Photos.
        /// </summary>
        /// <param name="data">PNG byte array of photo to save to disk.</param>
        /// <param name="photoId">Name of the photo on disk</param>
        /// <returns></returns>
        public async void StorePhoto(byte[] data, string photoId)
        {
            await StorePhotoImpl(data, photoId);
        }

        /// <summary>
        /// Store an Image<Rgb24> as a PNG on disk.
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public async Task<ResourcePath> StoreImagePNG(Image<Rgb24> image)
        {
            string photoId = "LAST_PHOTO_TAKEN.png";
            bool success = false;
            ResourcePath path;

            EnsurePhotoDirExists();

            for (var i = 0; i < 5; i++)
            {
                try
                {
                    if (i != 0)
                    {
                        photoId = $"LAST_PHOTO_TAKEN-{i}.png";
                    }

                    path = PhotosDir / photoId;

                    await using var file =
                        _resourceManager.UserData.Open(path, FileMode.OpenOrCreate);

                    await Task.Run(() =>
                    {
                        image.SaveAsPng(file);
                    });

                    return path;
                }
                catch (IOException e)
                {
                    Logger.WarningS("photo", "Failed to save photo, retrying?:\n{0}", e);
                }
            }
            if (!success)
            {
                Logger.ErrorS("photo", "Unable to save photo.");
            }
            return null;
        }

        /// <summary>
        /// Get the PNG bytes of the photo photoId.
        /// </summary>
        /// <param name="photoId">Id of the photo to get the bytes of.</param>
        /// <returns></returns>
        public async Task<byte[]> GetPhotoBytes(string photoId)
        {
            //Already cached
            if(TryGetPhotoBytes(photoId, out var photoBytes))
            {
                return photoBytes;
            }

            //Request photo from server
            RaiseNetworkEvent(new PhotoSystemMessages.RequestPhotoMessage(photoId));

            var requestCancelTokenSource = new CancellationTokenSource();
            _photoRequestTokens.Add(photoId, requestCancelTokenSource);
            PhotoSystemMessages.PhotoResponseMessage response;

            try
            {
                response =
                    await AwaitNetworkEvent<PhotoSystemMessages.PhotoResponseMessage>(requestCancelTokenSource.Token);
            }
            catch (TaskCanceledException)
            {
                return null;
            }
            finally
            {
                requestCancelTokenSource = null;
                _photoRequestTokens.Remove(photoId);
            }

            //Cache
            _photoSystem.StorePhoto(response.PhotoBytes, photoId);

            return response.PhotoBytes;
        }
    }
}
