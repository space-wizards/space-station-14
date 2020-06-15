using Content.Client.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Photography;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Players;

namespace Content.Client.GameObjects.Components.Photography
{
    [RegisterComponent]
    public class PhotoComponent : SharedPhotoComponent
    {
        private string _photoId;
        private PhotoWindow _window;

        public override void Initialize()
        {
            base.Initialize();

            _window = new PhotoWindow();
        }

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession session = null)
        {
            base.HandleNetworkMessage(message, netChannel, session);

            switch (message)
            {
                case OpenPhotoUiMessage photo:
                    if (_photoId != photo.PhotoId)
                    {
                        _photoId = photo.PhotoId;
                        _window.Populate(_photoId);
                        _window.OpenCentered();
                    } else
                    {
                        _window.OpenCentered();
                    }
                    break;
            }
        }
    }
}
