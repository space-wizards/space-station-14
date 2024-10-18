using System.Collections.Generic;
using System.Numerics;
using System.Text.RegularExpressions;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using static Content.Shared.Configurable.ConfigurationComponent;
using static Robust.Client.UserInterface.Controls.BaseButton;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Configurable.UI
{
    public sealed class ConfigurationMenu : DefaultWindow
    {
        private readonly BoxContainer _column;
        private readonly BoxContainer _row;

        private readonly List<(string  name, LineEdit input)> _inputs;

        [ViewVariables]
        public Regex? Validation { get; internal set; }

        public event Action<Dictionary<string, string>>? OnConfiguration;

        public ConfigurationMenu()
        {
            MinSize = SetSize = new Vector2(300, 250);

            _inputs = new List<(string name, LineEdit input)>();

            Title = Loc.GetString("configuration-menu-device-title");

            var baseContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                VerticalExpand = true,
                HorizontalExpand = true
            };

            _column = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                Margin = new Thickness(8),
                SeparationOverride = 16,
            };

            _row = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                SeparationOverride = 16,
                HorizontalExpand = true
            };

            var confirmButton = new Button
            {
                Text = Loc.GetString("configuration-menu-confirm"),
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
            baseContainer.AddChild(outerColumn);
            baseContainer.AddChild(confirmButton);
            Contents.AddChild(baseContainer);
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
                    Text = field.Value ?? "",
                    IsValid = Validate,
                    HorizontalExpand = true,
                    SizeFlagsStretchRatio = .8f
                };

                _inputs.Add((field.Key, input));

                var row = new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal
                };
                CopyProperties(_row, row);

                row.AddChild(label);
                row.AddChild(input);
                _column.AddChild(row);
            }
        }

        private void OnConfirm(ButtonEventArgs args)
        {
            var config = GenerateDictionary(_inputs, "Text");
            OnConfiguration?.Invoke(config);
            Close();
        }

        private bool Validate(string value)
        {
            return Validation?.IsMatch(value) != false;
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
