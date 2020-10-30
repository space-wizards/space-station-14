using System.Collections.Generic;
using Namotion.Reflection;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using static Content.Shared.GameObjects.Components.SharedConfigurationComponent;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.GameObjects.Components.Wires
{
    public class ConfigurationMenu : SS14Window
    {
        public ConfigurationBoundUserInterface Owner { get; }

        private readonly VBoxContainer _baseContainer;
        private readonly VBoxContainer _column;
        private readonly HBoxContainer _row;

        private readonly List<(string  name, LineEdit input)> _inputs;

        protected override Vector2? CustomSize => (300, 250);

        public ConfigurationMenu(ConfigurationBoundUserInterface owner)
        {
            Owner = owner;

            _inputs = new List<(string name, LineEdit input)>();

            Title = Loc.GetString("Device Configuration");

            var margin = new MarginContainer
            {
                MarginBottomOverride = 8,
                MarginLeftOverride = 8,
                MarginRightOverride = 8,
                MarginTopOverride = 8
            };

            _baseContainer = new VBoxContainer
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
                SizeFlagsHorizontal = SizeFlags.FillExpand
            };

            _column = new VBoxContainer
            {
                SeparationOverride = 16,
                SizeFlagsVertical = SizeFlags.Fill
            };

            _row = new HBoxContainer
            {
                SeparationOverride = 16,
                SizeFlagsHorizontal = SizeFlags.FillExpand
            };

            var buttonRow = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand
            };

            var spacer1 = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.Expand
            };

            var spacer2 = new HBoxContainer()
            {
                SizeFlagsHorizontal = SizeFlags.Expand
            };

            var confirmButton = new Button
            {
                Text = Loc.GetString("Confirm"),
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                SizeFlagsVertical = SizeFlags.ShrinkCenter
            };

            confirmButton.OnButtonUp += OnConfirm;
            buttonRow.AddChild(spacer1);
            buttonRow.AddChild(confirmButton);
            buttonRow.AddChild(spacer2);

            var outerColumn = new ScrollContainer
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                ModulateSelfOverride = Color.FromHex("#202025")
            };

            margin.AddChild(_column);
            outerColumn.AddChild(margin);
            _baseContainer.AddChild(outerColumn);
            _baseContainer.AddChild(buttonRow);
            Contents.AddChild(_baseContainer);
        }

        public void Populate(ConfigurationBoundUserInterfaceState state)
        {
            _column.Children.Clear();
            _inputs.Clear();
            
            foreach (var field in state.Config)
            {
                var margin = new MarginContainer
                {
                    MarginRightOverride = 8
                };

                var label = new Label
                {
                    Name = field.Key,
                    Text = field.Key + ":",
                    SizeFlagsVertical = SizeFlags.ShrinkCenter,
                    SizeFlagsHorizontal = SizeFlags.FillExpand,
                    SizeFlagsStretchRatio = .2f,
                    CustomMinimumSize = new Vector2(60, 0)
                };

                var input = new LineEdit
                {
                    Name = field.Key + "-input",
                    Text = field.Value,
                    IsValid = Validate,
                    SizeFlagsHorizontal = SizeFlags.FillExpand,
                    SizeFlagsStretchRatio = .8f
                };

                _inputs.Add((field.Key, input));

                var row = new HBoxContainer();
                CopyProperties(_row, row);

                margin.AddChild(label);
                row.AddChild(margin);
                row.AddChild(input);
                _column.AddChild(row);
            }
        }

        private void OnConfirm(ButtonEventArgs args)
        {
            var config = GenerateDictionary<string, LineEdit>(_inputs, "Text");
            
            Owner.SendConfiguration(config);
            Close();
        }

        private bool Validate(string value)
        {
            return Owner.Validation == null || Owner.Validation.IsMatch(value);
        }

        private Dictionary<string, TConfig> GenerateDictionary<TConfig, TInput>(List<(string name, TInput input)> inputs, string propertyName) where TInput : Control
        {
            var dictionary = new Dictionary<string, TConfig>();
            foreach (var input in inputs)
            {
                var value = input.input.TryGetPropertyValue<TConfig>(propertyName);
                dictionary.Add(input.name, value);
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
