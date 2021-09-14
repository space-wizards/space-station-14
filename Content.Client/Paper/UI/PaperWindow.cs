using Content.Shared.Paper;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Paper.UI
{
    public class PaperWindow : SS14Window
    {
        private readonly RichTextLabel _label;
        public readonly LineEdit Input;

        public PaperWindow()
        {
            MinSize = SetSize = (300, 300);
            var container = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical
            };
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
