using Robust.Client.UserInterface.CustomControls;

namespace Content.Client.Administration.UI.CustomControls
{
    public sealed class UICommandButton : CommandButton
    {
        public Type? WindowType { get; set; }
        private DefaultWindow? _window;

        protected override void Execute(ButtonEventArgs obj)
        {
            if (WindowType == null)
                return;
            _window = (DefaultWindow) IoCManager.Resolve<IDynamicTypeFactory>().CreateInstance(WindowType);
            _window?.OpenCentered();
        }
    }
}
