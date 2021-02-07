using Content.Shared.GameObjects.Components;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Client.GameObjects.Components.Paper
{
    public class PaperWindow : SS14Window
    {
        private readonly RichTextLabel _label;
        public readonly LineEdit Input;
        protected override Vector2? CustomSize => (300, 300);

        public PaperWindow()
        {
            var container = new VBoxContainer();
            _label = new RichTextLabel();
            Input = new LineEdit {Visible = false};
            container.AddChild(_label);
            container.AddChild(Input);
            Contents.AddChild(container);
        }

        public void Populate(SharedPaperComponent.PaperBoundUserInterfaceState state)
        {
            if (state.Mode == SharedPaperComponent.PaperAction.Write)
            {
                Input.Visible = true;
            }
            var msg = new FormattedMessage();
            msg.AddMarkupPermissive(state.Text);
            _label.SetMessage(msg);
        }
    }
}
