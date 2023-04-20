using System.Collections.Generic;
using System.Text.RegularExpressions;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using static Content.Shared.Configurable.ConfigurationComponent;

namespace Content.Client.Configurable.UI
{
    public sealed class ConfigurationBoundUserInterface : BoundUserInterface
    {
        public Regex? Validation { get; internal set; }

        public ConfigurationBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        private ConfigurationMenu? _menu;

        protected override void Open()
        {
            base.Open();
            _menu = new ConfigurationMenu(this);

            _menu.OnClose += Close;
            _menu.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not ConfigurationBoundUserInterfaceState configurationState)
            {
                return;
            }

            _menu?.Populate(configurationState);
        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            base.ReceiveMessage(message);

            if (message is ValidationUpdateMessage msg)
            {
                Validation = new Regex(msg.ValidationString, RegexOptions.Compiled);
            }
        }

        public void SendConfiguration(Dictionary<string, string> config)
        {
            SendMessage(new ConfigurationUpdatedMessage(config));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing && _menu != null)
            {
                _menu.OnClose -= Close;
                _menu.Close();
            }
        }
    }
}
