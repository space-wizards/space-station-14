using Content.Shared.GameObjects.Components.Photography;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Players;
using System.IO;

namespace Content.Client.GameObjects.Components.Photography
{
    [RegisterComponent]
    public class PhotoComponent : SharedPhotoComponent
    {
        private Texture _texture;
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
                //Open the photo ui, and store the new image
                case SetPhotoAndOpenUiMessage setPhoto:

                    //TODO: should write this to disk too?, so we're not keeping it in memory forever
                    _texture = Texture.LoadFromPNGStream(new MemoryStream(setPhoto.Data));

                    _window.Populate(_texture);
                    _window.ForceRunLayoutUpdate();
                    _window.OpenCentered();
                    break;

                //Simply open the photo ui
                case OpenPhotoUiMessage _:
                    _window.Populate(_texture);
                    _window.OpenCentered();
                    break;
            }
        }
    }
}
