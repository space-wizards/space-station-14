using System;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;

namespace Content.Client.Administration.UI.CustomControls
{
    public sealed class UICommandButton : CommandButton
    {
        public Type? WindowType { get; set; }
        private DefaultWindow? _window;

        public event Action<DefaultWindow>? OnWindowCreated;

        protected override void Execute(ButtonEventArgs obj)
        {
            if (WindowType == null)
                return;
            _window = (DefaultWindow) IoCManager.Resolve<IDynamicTypeFactory>().CreateInstance(WindowType);
            //SS220-antag-add-objective begin
            if (_window is not null)
                OnWindowCreated?.Invoke(_window);
            //SS220-antag-add-objective end
            _window?.OpenCentered();
        }
    }
}
