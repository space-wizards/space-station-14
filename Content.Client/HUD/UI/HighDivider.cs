using Content.Client.Stylesheets;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.HUD.UI
{
    public sealed class HighDivider : Control
    {
        public HighDivider()
        {
            Children.Add(new PanelContainer {StyleClasses = {StyleBase.ClassHighDivider}});
        }
    }
}
