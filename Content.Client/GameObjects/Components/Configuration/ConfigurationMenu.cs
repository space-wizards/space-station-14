using System.Collections.Generic;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using static Content.Shared.GameObjects.Components.SharedConfigurationComponent;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.GameObjects.Components.Configuration
{
    public class ConfigurationMenu : SS14Window
    {
        public ConfigurationBoundUserInterface Owner { get; }

        private readonly VBoxContainer _baseContainer;
        private readonly VBoxContainer _column;
        private readonly HBoxContainer _row;

        private readonly List<(string  name, LineEdit input)> _inputs;

        public ConfigurationMenu(ConfigurationBoundUserInterface owner)
        {
            MinSize = SetSize = (300, 250);
            Owner = owner;

            _inputs = new List<(string name, LineEdit input)>();

            Title = Loc.GetString("configuration-device-title");

            _baseContainer = new VBoxContainer
            {
                VerticalExpand = true,
                HorizontalExpand = true
            };

            _column = new VBoxContainer
            {
                Margin = new Thickness(8),
                SeparationOverride = 16,
            };

            _row = new HBoxContainer
            {
                SeparationOverride = 16,
                HorizontalExpand = true
            };

            var confirmButton = new Button
            {
                Text = Loc.GetString("configuration-confirm"),
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Center
            };

            confirmButton.OnButtonUp += OnConfirm;

            var outerColumn = new ScrollContainer
            {
                VerticalExpand = true,
                HorizontalExpand = true,
                ModulateSelfOverride = Color.FromHex("#202025")
            };

            outerColumn.AddChild(_column);
            _baseContainer.AddChild(outerColumn);
            _baseContainer.AddChild(confirmButton);
            Contents.AddChild(_baseContainer);
        }

        public void Populate(ConfigurationBoundUserInterfaceState state)
        {
            _column.Children.Clear();
            _inputs.Clear();

            foreach (var field in state.Config)
            {
                var label = new Label
                {
                    Margin = new Thickness(0, 0, 8, 0),
                    Name = field.Key,
                    Text = field.Key + ":",
                    VerticalAlignment = VAlignment.Center,
                    HorizontalExpand = true,
                    SizeFlagsStretchRatio = .2f,
                    MinSize = new Vector2(60, 0)
                };

                var input = new LineEdit
                {
                    Name = field.Key + "-input",
                    Text = field.Value,
                    IsValid = Validate,
                    HorizontalExpand = true,
                    SizeFlagsStretchRatio = .8f
                };

                _inputs.Add((field.Key, input));

                var row = new HBoxContainer();
                CopyProperties(_row, row);

                row.AddChild(label);
                row.AddChild(input);
                _column.AddChild(row);
            }
        }

        private void OnConfirm(ButtonEventArgs args)
        {
            var config = GenerateDictionary(_inputs, "Text");

            Owner.SendConfiguration(config);
            Close();
        }

        private bool Validate(string value)
        {
            return Owner.Validation == null || Owner.Validation.IsMatch(value);
        }

        private Dictionary<string, string> GenerateDictionary(IEnumerable<(string name, LineEdit input)> inputs, string propertyName)
        {
            var dictionary = new Dictionary<string, string>();

            foreach (var input in inputs)
            {
                dictionary.Add(input.name, input.input.Text);
            }

            return dictionary;
        }

        private static void CopyProperties<T>(T from, T to) where T : Control
        {
            foreach (var property in from.AllAttachedProperties)
            {
                to.SetValue(property.Key, property.Value);
            }
        }
    }
}
