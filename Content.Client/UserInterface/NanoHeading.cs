using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client.UserInterface
{
    public class NanoHeading : Container
    {
        private readonly Label _label;
        private readonly PanelContainer _panel;

        public NanoHeading()
        {
            _panel = new PanelContainer
            {
                Children = {(_label = new Label
                {
                    StyleClasses = {NanoStyle.StyleClassLabelHeading}
                })}
            };
            AddChild(_panel);

            SizeFlagsHorizontal = SizeFlags.None;
        }

        public string Text
        {
            get => _label.Text;
            set => _label.Text = value;
        }

        protected override Vector2 CalculateMinimumSize()
        {
            return _panel.CombinedMinimumSize;
        }

        protected override void SortChildren()
        {
            FitChildInBox(_panel, SizeBox);
        }
    }
}
