using System.Text.RegularExpressions;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using static Content.Shared.Configurable.ConfigurationComponent;

namespace Content.Client.Configurable.UI
{
    public sealed class ConfigurationBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private ConfigurationMenu? _menu;

        public ConfigurationBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();
            _menu = this.CreateWindow<ConfigurationMenu>();
            _menu.OnConfiguration += SendConfiguration;
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not ConfigurationBoundUserInterfaceState configurationState)
                return;

            _menu?.Populate(configurationState);
        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            base.ReceiveMessage(message);

            if (_menu == null)
                return;

            if (message is ValidationUpdateMessage msg)
            {
                _menu.Validation = new Regex(msg.ValidationString, RegexOptions.Compiled);
            }
        }

        public void SendConfiguration(Dictionary<string, string> config)
        {
            SendMessage(new ConfigurationUpdatedMessage(config));
        }
    }
}
