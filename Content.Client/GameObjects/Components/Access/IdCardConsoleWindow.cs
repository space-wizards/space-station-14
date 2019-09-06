using System.Collections.Generic;
using System.Linq;
using Content.Shared.Access;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using static Content.Shared.GameObjects.Components.Access.SharedIdCardConsoleComponent;

namespace Content.Client.GameObjects.Components.Access
{
    public class IdCardConsoleWindow : SS14Window
    {
        private readonly Label _fullNameLabel;
        private readonly LineEdit _fullNameLineEdit;
        private readonly Label _jobTitleLabel;
        private readonly LineEdit _jobTitleLineEdit;
        private readonly IdCardConsoleBoundUserInterface _owner;
        private readonly Button _privilegedIdButton;
        private readonly Button _targetIdButton;
        private readonly Button _submitButton;
        private readonly ILocalizationManager _localizationManager;

        private Dictionary<string, Button> _accessButtons = new Dictionary<string, Button>();

        public IdCardConsoleWindow(IdCardConsoleBoundUserInterface owner, ILocalizationManager localizationManager)
        {
            _localizationManager = localizationManager;
            _owner = owner;
            var vBox = new VBoxContainer();

            {
                var hBox = new HBoxContainer();
                vBox.AddChild(hBox);

                _privilegedIdButton = new Button();
                _privilegedIdButton.OnPressed += _ => _owner.ButtonPressed(UiButton.PrivilegedId);
                hBox.AddChild(_privilegedIdButton);

                _targetIdButton = new Button();
                _targetIdButton.OnPressed += _ => _owner.ButtonPressed(UiButton.TargetId);
                hBox.AddChild(_targetIdButton);
            }

            {
                var hBox = new HBoxContainer();
                vBox.AddChild(hBox);
                hBox.AddChild(_fullNameLabel = new Label()
                {
                    Text = localizationManager.GetString("Full name:")
                });

                _fullNameLineEdit = new LineEdit()
                {
                    SizeFlagsHorizontal = SizeFlags.FillExpand,
                };
                hBox.AddChild(_fullNameLineEdit);
            }

            {
                var hBox = new HBoxContainer();
                vBox.AddChild(hBox);
                hBox.AddChild(_jobTitleLabel = new Label()
                {
                    Text = localizationManager.GetString("Job title:")
                });

                _jobTitleLineEdit = new LineEdit()
                {
                    SizeFlagsHorizontal = SizeFlags.FillExpand
                };
                hBox.AddChild(_jobTitleLineEdit);
            }

            {
                var hBox = new HBoxContainer();
                vBox.AddChild(hBox);

                foreach (var accessName in SharedAccess.AllAccess)
                {
                    var newButton = new Button()
                    {
                        Text = accessName,
                        ToggleMode = true,
                    };
                    hBox.AddChild(newButton);
                    _accessButtons.Add(accessName, newButton);
                }
            }

            {
                var hBox = new HBoxContainer();
                vBox.AddChild(hBox);

                _submitButton = new Button()
                {
                    Text = localizationManager.GetString("Submit")
                };
                _submitButton.OnPressed += _ => owner.SubmitData(
                    _fullNameLineEdit.Text,
                    _jobTitleLineEdit.Text,
                    // Iterate over the buttons dictionary, filter by `Pressed`, only get key from the key/value pair
                    _accessButtons.Where(x => x.Value.Pressed).Select(x => x.Key).ToList());
                hBox.AddChild(_submitButton);
            }

            Contents.AddChild(vBox);
        }

        public void UpdateState(IdCardConsoleBoundUserInterfaceState state)
        {
            _privilegedIdButton.Text = state.IsPrivilegedIdPresent
                ? _localizationManager.GetString("Remove privileged ID card")
                : _localizationManager.GetString("Insert privileged ID card");

            _targetIdButton.Text = state.IsTargetIdPresent
                ? _localizationManager.GetString("Remove target ID card")
                : _localizationManager.GetString("Insert target ID card");

            var interfaceEnabled = state.IsPrivilegedIdPresent && state.IsPrivilegedIdAuthorized && state.IsTargetIdPresent;

            _fullNameLabel.Modulate = interfaceEnabled ? Color.White : Color.Gray;
            _fullNameLineEdit.Editable = interfaceEnabled;
            _fullNameLineEdit.Text = state.TargetIdFullName;

            _jobTitleLabel.Modulate = interfaceEnabled ? Color.White : Color.Gray;
            _jobTitleLineEdit.Editable = interfaceEnabled;
            _jobTitleLineEdit.Text = state.TargetIdJobTitle;

            foreach (var (accessName, button) in _accessButtons)
            {
                button.Disabled = !interfaceEnabled;
                if (interfaceEnabled)
                {
                    button.Pressed = state.TargetIdAccessList.Contains(accessName);
                }
            }

            _submitButton.Disabled = !interfaceEnabled;
        }
    }
}
