using System;
using Content.Client.GameObjects.Components;
using Content.Client.Interfaces;
using Content.Shared.Preferences;
using Content.Shared.Text;
using Robust.Client.Graphics.Drawing;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Random;

namespace Content.Client.UserInterface
{
    public class HumanoidProfileEditor : Control
    {
        private static readonly StyleBoxFlat HighlightedStyle = new StyleBoxFlat
        {
            BackgroundColor = new Color(47, 47, 53),
            ContentMarginTopOverride = 10,
            ContentMarginBottomOverride = 10,
            ContentMarginLeftOverride = 10,
            ContentMarginRightOverride = 10
        };

        private readonly LineEdit _ageEdit;

        private readonly LineEdit _nameEdit;
        private readonly IClientPreferencesManager _preferencesManager;
        private readonly Button _saveButton;
        private readonly Button _sexFemaleButton;
        private readonly Button _sexMaleButton;
        private readonly HairStylePicker _hairPicker;
        private readonly FacialHairStylePicker _facialHairPicker;

        private bool _isDirty;
        public int CharacterSlot;
        public HumanoidCharacterProfile Profile;
        public event Action<HumanoidCharacterProfile> OnProfileChanged;

        private void NameChanged(string newName)
        {
            Profile = Profile?.WithName(newName);
            IsDirty = true;
        }

        public HumanoidProfileEditor(ILocalizationManager localization,
            IClientPreferencesManager preferencesManager)
        {
            Profile = (HumanoidCharacterProfile) preferencesManager.Preferences.SelectedCharacter;
            CharacterSlot = preferencesManager.Preferences.SelectedCharacterIndex;
            _preferencesManager = preferencesManager;

            var margin = new MarginContainer
            {
                MarginTopOverride = 10,
                MarginBottomOverride = 10,
                MarginLeftOverride = 10,
                MarginRightOverride = 10
            };
            AddChild(margin);

            var vBox = new VBoxContainer();
            margin.AddChild(vBox);

            var middleContainer = new HBoxContainer
            {
                SeparationOverride = 10
            };
            vBox.AddChild(middleContainer);

            var leftColumn = new VBoxContainer();
            middleContainer.AddChild(leftColumn);

            #region Randomize

            {
                var panel = HighlightedContainer();
                var randomizeEverythingButton = new Button
                {
                    Text = localization.GetString("Randomize everything"),
                    Disabled = true,
                    ToolTip = "Not yet implemented!"
                };
                panel.AddChild(randomizeEverythingButton);
                leftColumn.AddChild(panel);
            }

            #endregion Randomize

            var middleColumn = new VBoxContainer();
            leftColumn.AddChild(middleColumn);

            #region Name

            {
                var panel = HighlightedContainer();
                var hBox = new HBoxContainer
                {
                    SizeFlagsVertical = SizeFlags.FillExpand
                };
                var nameLabel = new Label {Text = localization.GetString("Name:")};
                _nameEdit = new LineEdit
                {
                    CustomMinimumSize = (270, 0),
                    SizeFlagsVertical = SizeFlags.ShrinkCenter
                };
                _nameEdit.OnTextChanged += args =>
                {
                    NameChanged(args.Text);
                };
                var nameRandomButton = new Button
                {
                    Text = localization.GetString("Randomize"),
                };
                nameRandomButton.OnPressed += args =>
                {
                    var random = IoCManager.Resolve<IRobustRandom>();
                    var firstName = random.Pick(
                        Profile.Sex == Sex.Male
                            ? Names.MaleFirstNames
                            : Names.FemaleFirstNames);
                    var lastName = random.Pick(Names.LastNames);
                    _nameEdit.Text = $"{firstName} {lastName}";
                    NameChanged(_nameEdit.Text);
                };
                hBox.AddChild(nameLabel);
                hBox.AddChild(_nameEdit);
                hBox.AddChild(nameRandomButton);
                panel.AddChild(hBox);
                middleColumn.AddChild(panel);
            }

            #endregion Name

            var sexAndAgeRow = new HBoxContainer
            {
                SeparationOverride = 10
            };
            middleColumn.AddChild(sexAndAgeRow);

            #region Sex

            {
                var panel = HighlightedContainer();
                var hBox = new HBoxContainer();
                var sexLabel = new Label {Text = localization.GetString("Sex:")};

                var sexButtonGroup = new ButtonGroup();

                _sexMaleButton = new Button
                {
                    Text = localization.GetString("Male"),
                    Group = sexButtonGroup
                };
                _sexMaleButton.OnPressed += args =>
                {
                    Profile = Profile?.WithSex(Sex.Male);
                    IsDirty = true;
                };
                _sexFemaleButton = new Button
                {
                    Text = localization.GetString("Female"),
                    Group = sexButtonGroup
                };
                _sexFemaleButton.OnPressed += args =>
                {
                    Profile = Profile?.WithSex(Sex.Female);
                    IsDirty = true;
                };
                hBox.AddChild(sexLabel);
                hBox.AddChild(_sexMaleButton);
                hBox.AddChild(_sexFemaleButton);
                panel.AddChild(hBox);
                sexAndAgeRow.AddChild(panel);
            }

            #endregion Sex

            #region Age

            {
                var panel = HighlightedContainer();
                var hBox = new HBoxContainer();
                var ageLabel = new Label {Text = localization.GetString("Age:")};
                _ageEdit = new LineEdit {CustomMinimumSize = (40, 0)};
                _ageEdit.OnTextChanged += args =>
                {
                    if (!int.TryParse(args.Text, out var newAge))
                        return;
                    Profile = Profile?.WithAge(newAge);
                    IsDirty = true;
                };
                hBox.AddChild(ageLabel);
                hBox.AddChild(_ageEdit);
                panel.AddChild(hBox);
                sexAndAgeRow.AddChild(panel);
            }

            #endregion Age

            var rightColumn = new VBoxContainer();
            middleContainer.AddChild(rightColumn);

            #region Import/Export

            {
                var panelContainer = HighlightedContainer();
                var hBox = new HBoxContainer();
                var importButton = new Button
                {
                    Text = localization.GetString("Import"),
                    Disabled = true,
                    ToolTip = "Not yet implemented!"
                };
                var exportButton = new Button
                {
                    Text = localization.GetString("Export"),
                    Disabled = true,
                    ToolTip = "Not yet implemented!"
                };
                hBox.AddChild(importButton);
                hBox.AddChild(exportButton);
                panelContainer.AddChild(hBox);
                rightColumn.AddChild(panelContainer);
            }

            #endregion Import/Export

            #region Save

            {
                var panel = HighlightedContainer();
                _saveButton = new Button
                {
                    Text = localization.GetString("Save"),
                    SizeFlagsHorizontal = SizeFlags.ShrinkCenter
                };
                _saveButton.OnPressed += args =>
                {
                    IsDirty = false;
                    _preferencesManager.UpdateCharacter(Profile, CharacterSlot);
                    OnProfileChanged?.Invoke(Profile);
                };
                panel.AddChild(_saveButton);
                rightColumn.AddChild(panel);
            }

            #endregion Save

            #region Hair

            {
                var panel = HighlightedContainer();
                panel.SizeFlagsHorizontal = SizeFlags.None;
                var hairHBox = new HBoxContainer();

                _hairPicker = new HairStylePicker();
                _hairPicker.Populate();

                _hairPicker.OnHairStylePicked += newStyle =>
                {
                    if (Profile is null)
                        return;
                    Profile = Profile.WithCharacterAppearance(
                        Profile.Appearance.WithHairStyleName(newStyle));
                    IsDirty = true;
                };

                _hairPicker.OnHairColorPicked += newColor =>
                {
                    if (Profile is null)
                        return;
                    Profile = Profile.WithCharacterAppearance(
                        Profile.Appearance.WithHairColor(newColor));
                    IsDirty = true;
                };

                _facialHairPicker = new FacialHairStylePicker();
                _facialHairPicker.Populate();

                _facialHairPicker.OnHairStylePicked += newStyle =>
                {
                    if (Profile is null)
                        return;
                    Profile = Profile.WithCharacterAppearance(
                        Profile.Appearance.WithFacialHairStyleName(newStyle));
                    IsDirty = true;
                };

                _facialHairPicker.OnHairColorPicked += newColor =>
                {
                    if (Profile is null)
                        return;
                    Profile = Profile.WithCharacterAppearance(
                        Profile.Appearance.WithFacialHairColor(newColor));
                    IsDirty = true;
                };

                hairHBox.AddChild(_hairPicker);
                hairHBox.AddChild(_facialHairPicker);

                panel.AddChild(hairHBox);
                vBox.AddChild(panel);
            }

            #endregion Hair

            UpdateControls();
        }

        private bool IsDirty
        {
            get => _isDirty;
            set
            {
                _isDirty = value;
                UpdateSaveButton();
            }
        }

        private static Control HighlightedContainer()
        {
            return new PanelContainer
            {
                PanelOverride = HighlightedStyle
            };
        }

        private void UpdateSexControls()
        {
            if (Profile.Sex == Sex.Male)
                _sexMaleButton.Pressed = true;
            else
                _sexFemaleButton.Pressed = true;
        }

        private void UpdateHairPickers()
        {
            _hairPicker.SetInitialData(
                Profile.Appearance.HairColor,
                Profile.Appearance.HairStyleName);
            _facialHairPicker.SetInitialData(
                Profile.Appearance.FacialHairColor,
                Profile.Appearance.FacialHairStyleName);
        }

        private void UpdateSaveButton()
        {
            _saveButton.Disabled = Profile is null || !IsDirty;
        }

        public void UpdateControls()
        {
            if (Profile is null) return;
            _nameEdit.Text = Profile?.Name;
            UpdateSexControls();
            _ageEdit.Text = Profile?.Age.ToString();
            UpdateHairPickers();
            UpdateSaveButton();
        }
    }
}
