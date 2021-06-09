#nullable enable
using System;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;

namespace Content.Client.Administration.UI.CustomControls
{
    public class UICommandButton : CommandButton
    {
        public Type? WindowType { get; set; }
        private SS14Window? _window;

        protected override void Execute(ButtonEventArgs obj)
        {
            if (WindowType == null)
                return;
            _window = (SS14Window) IoCManager.Resolve<IDynamicTypeFactory>().CreateInstance(WindowType);
            _window?.OpenCentered();
        }
    }
}
