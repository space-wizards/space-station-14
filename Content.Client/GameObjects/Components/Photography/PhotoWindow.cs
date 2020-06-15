using Content.Client.GameObjects.EntitySystems;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Maths;
using System.IO;

namespace Content.Client.GameObjects.Components
{
    public class PhotoWindow : SS14Window
    {
        private PhotoSystem _photoSystem;

        protected override Vector2? CustomSize => (0,0); //texture rect fills the ui and scales it for us
        private TextureRect _photo;

        public PhotoWindow()
        {
            Title = "Photo";
            var container = new VBoxContainer();
            _photo = new TextureRect();
            container.AddChild(_photo);
            Contents.AddChild(container);

            _photoSystem = EntitySystem.Get<PhotoSystem>();
        }

        public async void Populate(string photoId)
        {
            var photoBytes = await _photoSystem.GetPhotoBytes(photoId);
            _photo.Texture = Texture.LoadFromPNGStream(new MemoryStream(photoBytes));
        }
    }
}
