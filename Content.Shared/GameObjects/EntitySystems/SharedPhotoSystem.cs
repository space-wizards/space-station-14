using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Resources;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Content.Shared.GameObjects.EntitySystems
{
    /// <summary>
    /// Manages photos stored on disk for both Client and Server.
    /// </summary>
    public abstract class SharedPhotoSystem : EntitySystem
    {
        /// <summary>
        /// Directory that photos are stored in.
        /// </summary>
        public static ResourcePath PhotosDir = new ResourcePath("/Photos");

        /// <summary>
        /// Dictionary mapping photoId to the ResourcePath it is stored at.
        /// </summary>
        private Dictionary<string, ResourcePath> _photos = new Dictionary<string, ResourcePath>();

#pragma warning disable 649
        [Dependency] private readonly IResourceManager _resourceManager = default;
#pragma warning restore 649

        protected void EnsurePhotoDirExists()
        {
            if (!_resourceManager.UserData.IsDir(PhotosDir))
            {
                _resourceManager.UserData.CreateDir(PhotosDir);
            }
        }

        /// <summary>
        /// Stores data (PNG byte array) to disk as photoId.png in /Photos.
        /// </summary>
        /// <param name="data">PNG byte array of photo to save.</param>
        /// <param name="photoId">Name of the photo on disk.</param>
        /// <returns></returns>
        protected async Task<ResourcePath> StorePhotoImpl(byte[] data, string photoId)
        {
            Logger.InfoS("photo", "Storing a photo...");

            EnsurePhotoDirExists();

            var path = PhotosDir / $"{photoId}.png";

            await using var file = _resourceManager.UserData.Open(path, FileMode.Create);

            using (file)
            {
                using var writer = new BinaryWriter(file);
                foreach (var dat in data)
                {
                    writer.Write(dat);
                }
            }

            Logger.InfoS("photo", $"Stored a photo to {path}");

            _photos.Add(photoId, path);

            return path;
        }

        /// <summary>
        /// Returns the ResourcePath that photoId is stored at on disk.
        /// </summary>
        /// <param name="photoId">Id of the photo to retrieve the path of.</param>
        /// <param name="photoPath">Path of the photo with Id photoId, if stored.</param>
        /// <returns></returns>
        public bool TryGetPhotoPath(string photoId, out ResourcePath photoPath)
        {
            return _photos.TryGetValue(photoId, out photoPath);
        }

        /// <summary>
        /// Returns a PNG byte array of the photo stored on disk as photoId.
        /// </summary>
        /// <param name="photoId">Id of the photo to retrieve the path of.</param>
        /// <param name="photoBytes">PNG byte array of the photo with Id photoId, if stored.</param>
        /// <returns></returns>
        public bool TryGetPhotoBytes(string photoId, out byte[] photoBytes)
        {
            if(TryGetPhotoPath(photoId, out var photoPath))
            {
                var photo = _resourceManager.UserData.Open(photoPath, FileMode.Open);
                photoBytes = photo.CopyToArray();
                return true;
            } else
            {
                photoBytes = null;
                return false;
            }
        }
    }
}
