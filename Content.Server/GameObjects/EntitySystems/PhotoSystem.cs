using Content.Server.GameObjects.Components.Photography;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Resources;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Content.Server.GameObjects.EntitySystems
{
    public class PhotoSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IResourceManager _resourceManager = default;
#pragma warning restore 649

        private int _filenameSource = 0;

        //TODO: should probably delete these photos on server shutdown.
        //Could also upload them to a photo site like Imgur or flickr or something?
        //might be fun.
        public async void StorePhoto(byte[] data, PhotoComponent photo)
        {
            Logger.InfoS("photo", "Storing a photo...");

            var filename = $"photo-{_filenameSource++}";
            var path = new ResourcePath($"{filename}.png");

            await using var file = _resourceManager.UserData.Open(path, FileMode.Create);

            using (file)
            {
                using var writer = new BinaryWriter(file);
                foreach (var dat in data)
                {
                    writer.Write(dat);
                }
            }

            photo.Path = path;

            Logger.InfoS("photo", $"Stored a photo to {path}");
        }
    }
}
