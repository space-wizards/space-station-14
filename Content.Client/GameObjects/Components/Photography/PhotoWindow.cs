using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Maths;

namespace Content.Client.GameObjects.Components
{
    public class PhotoWindow : SS14Window
    {
        protected override Vector2? CustomSize => (0,0); //texture rect fills the ui and scales it for us
        private TextureRect _photo;

        public PhotoWindow()
        {
            Title = "Photo";
            var container = new VBoxContainer();
            _photo = new TextureRect();
            container.AddChild(_photo);
            Contents.AddChild(container);
        }

        public void Populate(Texture photo)
        {
            _photo.Texture = photo;
        }
    }
}
