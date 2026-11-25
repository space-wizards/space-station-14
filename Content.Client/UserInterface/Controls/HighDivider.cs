using Content.Client.Stylesheets;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Controls
{
    public sealed class HighDivider : Control
    {
        public HighDivider()
        {
            Children.Add(new PanelContainer {StyleClasses = {StyleClass.HighDivider}});
        }
    }
}
