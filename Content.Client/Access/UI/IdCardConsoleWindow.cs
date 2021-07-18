using System.Collections.Generic;
using System.Linq;
using Content.Shared.Access;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using static Content.Shared.Access.SharedIdCardConsoleComponent;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Access.UI
{
    public class IdCardConsoleWindow : SS14Window
    {
        private readonly Button _privilegedIdButton;
        private readonly Button _targetIdButton;

        private readonly Label _privilegedIdLabel;
        private readonly Label _targetIdLabel;

        private readonly Label _fullNameLabel;
        private readonly LineEdit _fullNameLineEdit;
        private readonly Label _jobTitleLabel;
        private readonly LineEdit _jobTitleLineEdit;

        private readonly Button _fullNameSaveButton;
        private readonly Button _jobTitleSaveButton;

        private readonly IdCardConsoleBoundUserInterface _owner;

        private readonly Dictionary<string, Button> _accessButtons = new();

        private string? _lastFullName;
        private string? _lastJobTitle;

        public IdCardConsoleWindow(IdCardConsoleBoundUserInterface owner, IPrototypeManager prototypeManager)
        {
            MinSize = SetSize = (650, 290);
            _owner = owner;
            var vBox = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical
            };

            vBox.AddChild(new GridContainer
            {
                Columns = 3,
                Children =
                {
                    new Label {Text = Loc.GetString("id-card-console-window-privileged-id")},
                    (_privilegedIdButton = new Button()),
                    (_privilegedIdLabel = new Label()),

                    new Label {Text = Loc.GetString("id-card-console-window-target-id")},
                    (_targetIdButton = new Button()),
                    (_targetIdLabel = new Label())
                }
            });

            _privilegedIdButton.OnPressed += _ => _owner.ButtonPressed(UiButton.PrivilegedId);
            _targetIdButton.OnPressed += _ => _owner.ButtonPressed(UiButton.TargetId);

            // Separator
            vBox.AddChild(new Control {MinSize = (0, 8)});

            // Name and job title line edits.
            vBox.AddChild(new GridContainer
            {
                Columns = 3,
                HSeparationOverride = 4,
                Children =
                {
                    // Name
                    (_fullNameLabel = new Label
                    {
                        Text = Loc.GetString("id-card-console-window-full-name-label")
                    }),
                    (_fullNameLineEdit = new LineEdit
                    {
                        HorizontalExpand = true,
                    }),
                    (_fullNameSaveButton = new Button
                    {
                        Text = Loc.GetString("id-card-console-window-save-button"),
                        Disabled = true
                    }),

                    // Title
                    (_jobTitleLabel = new Label
                    {
                        Text = Loc.GetString("id-card-console-window-job-title-label")
                    }),
                    (_jobTitleLineEdit = new LineEdit
                    {
                        HorizontalExpand = true
                    }),
                    (_jobTitleSaveButton = new Button
                    {
                        Text = Loc.GetString("id-card-console-window-save-button"),
                        Disabled = true
                    }),
                },
            });

            _fullNameLineEdit.OnTextEntered += _ => SubmitData();
            _fullNameLineEdit.OnTextChanged += _ =>
            {
                _fullNameSaveButton.Disabled = _fullNameSaveButton.Text == _lastFullName;
            };
            _fullNameSaveButton.OnPressed += _ => SubmitData();

            _jobTitleLineEdit.OnTextEntered += _ => SubmitData();
            _jobTitleLineEdit.OnTextChanged += _ =>
            {
                _jobTitleSaveButton.Disabled = _jobTitleLineEdit.Text == _lastJobTitle;
            };
            _jobTitleSaveButton.OnPressed += _ => SubmitData();

            // Separator
            vBox.AddChild(new Control {MinSize = (0, 8)});

            {
                var grid = new GridContainer
                {
                    Columns = 5,
                    HorizontalAlignment = HAlignment.Center
                };
                vBox.AddChild(grid);

                foreach (var accessLevel in prototypeManager.EnumeratePrototypes<AccessLevelPrototype>())
                {
                    var newButton = new Button
                    {
                        Text = accessLevel.Name,
                        ToggleMode = true,
                    };
                    grid.AddChild(newButton);
                    _accessButtons.Add(accessLevel.ID, newButton);
                    newButton.OnPressed += _ => SubmitData();
                }
            }

            Contents.AddChild(vBox);
        }

        public void UpdateState(IdCardConsoleBoundUserInterfaceState state)
        {
            _privilegedIdButton.Text = state.IsPrivilegedIdPresent
                ? Loc.GetString("id-card-console-window-eject-button")
                : Loc.GetString("id-card-console-window-insert-button");

            _privilegedIdLabel.Text = state.PrivilegedIdName;

            _targetIdButton.Text = state.IsTargetIdPresent
                ? Loc.GetString("id-card-console-window-eject-button")
                : Loc.GetString("id-card-console-window-insert-button");

            _targetIdLabel.Text = state.TargetIdName;

            var interfaceEnabled =
                state.IsPrivilegedIdPresent && state.IsPrivilegedIdAuthorized && state.IsTargetIdPresent;

            var fullNameDirty = _lastFullName != null && _fullNameLineEdit.Text != state.TargetIdFullName;
            var jobTitleDirty = _lastJobTitle != null && _jobTitleLineEdit.Text != state.TargetIdJobTitle;

            _fullNameLabel.Modulate = interfaceEnabled ? Color.White : Color.Gray;
            _fullNameLineEdit.Editable = interfaceEnabled;
            if (!fullNameDirty)
            {
                _fullNameLineEdit.Text = state.TargetIdFullName ?? string.Empty;
            }

            _fullNameSaveButton.Disabled = !interfaceEnabled || !fullNameDirty;

            _jobTitleLabel.Modulate = interfaceEnabled ? Color.White : Color.Gray;
            _jobTitleLineEdit.Editable = interfaceEnabled;
            if (!jobTitleDirty)
            {
                _jobTitleLineEdit.Text = state.TargetIdJobTitle ?? string.Empty;
            }

            _jobTitleSaveButton.Disabled = !interfaceEnabled || !jobTitleDirty;

            foreach (var (accessName, button) in _accessButtons)
            {
                button.Disabled = !interfaceEnabled;
                if (interfaceEnabled)
                {
                    button.Pressed = state.TargetIdAccessList?.Contains(accessName) ?? false;
                }
            }

            _lastFullName = state.TargetIdFullName;
            _lastJobTitle = state.TargetIdJobTitle;
        }

        private void SubmitData()
        {
            _owner.SubmitData(
                _fullNameLineEdit.Text,
                _jobTitleLineEdit.Text,
                // Iterate over the buttons dictionary, filter by `Pressed`, only get key from the key/value pair
                _accessButtons.Where(x => x.Value.Pressed).Select(x => x.Key).ToList());
        }
    }
}
