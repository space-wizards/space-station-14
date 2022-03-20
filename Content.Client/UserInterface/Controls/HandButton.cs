using Content.Shared.Hands.Components;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Controls
{

    public sealed class RightHandButton : HandButton
    {
        public override HandLocation Location => HandLocation.Left;
    }
    public sealed class LeftHandButton : HandButton
    {
        public override HandLocation Location => HandLocation.Right;
    }

    [Virtual]
    public abstract class HandButton : ItemSlotButton
    {
        public abstract HandLocation Location { get;}
        public TextureRect Blocked { get; }
        public Texture? BlockedTexture { get => Blocked.Texture; }
        public string BlockedTexturePath
        {
            get => _blockedTexturePath;
            set
            {
                _blockedTexturePath = value;
                Blocked.Texture = Theme.ResolveTexture(_blockedTexturePath);
            }
        }

        private string _blockedTexturePath = "";
        private bool _activeHand;
        private bool _highlighted;

        public HandButton()
        {
            AddChild(Blocked = new TextureRect
            {
                TextureScale = (2, 2),
                MouseFilter = MouseFilterMode.Stop,
                Visible = false
            });
        }



        public void SetActiveHand(bool active)
        {
            _activeHand = active;
            UpdateHighlight();
        }

        public override void Highlight(bool highlight)
        {
            _highlighted = highlight;
            UpdateHighlight();
        }

        private void UpdateHighlight()
        {
            // always stay highlighted if active
            base.Highlight(_activeHand || _highlighted);
        }
        public override void UpdateTheme(HudTheme newTheme)
        {
            base.UpdateTheme(newTheme);
            Blocked.Texture = Theme.ResolveTexture(_blockedTexturePath);
        }
    }
}
