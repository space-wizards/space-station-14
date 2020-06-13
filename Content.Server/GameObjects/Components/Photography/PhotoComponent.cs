using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Photography;
using Content.Shared.Interfaces;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Resources;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using System.IO;

namespace Content.Server.GameObjects.Components.Photography
{
    [RegisterComponent]
    public class PhotoComponent : SharedPhotoComponent, IUse
    {
#pragma warning disable 649
        [Dependency] private readonly IResourceManager _resourceManager = default;
#pragma warning restore 649

        private ResourcePath _path;
        private bool _cached;

        [ViewVariables]
        public ResourcePath Path
        {
            get => _path;
            set
            {
                _path = value;
                _cached = false;
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            IoCManager.InjectDependencies(this);
        }

        public bool UseEntity(UseEntityEventArgs eventArgs)
        {
            if(_path == null)
            {
                eventArgs.User.PopupMessage(eventArgs.User, "It's not dry yet!");
                return false;
            }

            if (!_cached)
            {
                _cached = true;
                var photo = _resourceManager.UserData.Open(_path, FileMode.Open);
                SendNetworkMessage(new SetPhotoAndOpenUiMessage(photo.CopyToArray()));
            }
            else
            {
                SendNetworkMessage(new OpenPhotoUiMessage());
            }
            return true;
        }
    }
}
