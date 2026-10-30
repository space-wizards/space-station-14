using System.Numerics;
using System.Text.RegularExpressions;
using Content.Shared.Configurable;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
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
            if (EntMan.TryGetComponent(Owner, out ConfigurationComponent? component))
                Refresh((Owner, component));
        }

        public void Refresh(Entity<ConfigurationComponent> entity)
        {
            if (_menu == null)
                return;

            _menu.Column.Children.Clear();
            _menu.Inputs.Clear();

            foreach (var field in entity.Comp.Config)
            {
                var label = new Label
                {
                    Margin = new Thickness(0, 0, 8, 0),
                    Name = field.Key,
                    Text = field.Key + ":",
                    VerticalAlignment = Control.VAlignment.Center,
                    HorizontalExpand = true,
                    SizeFlagsStretchRatio = .2f,
                    MinSize = new Vector2(60, 0)
                };

                var input = new LineEdit
                {
                    Name = field.Key + "-input",
                    Text = field.Value ?? "",
                    IsValid = _menu.Validate,
                    HorizontalExpand = true,
                    SizeFlagsStretchRatio = .8f
                };

                _menu.Inputs.Add((field.Key, input));

                var row = new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Horizontal
                };

                ConfigurationMenu.CopyProperties(_menu.Row, row);

                row.AddChild(label);
                row.AddChild(input);
                _menu.Column.AddChild(row);
            }
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
