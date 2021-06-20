using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client.CharacterAppearance;
using Content.Client.Lobby.UI;
using Content.Client.Stylesheets;
using Content.Shared.CharacterAppearance;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Preferences.UI
{
    public partial class HumanoidProfileEditor : Control
    {
        private static readonly StyleBoxFlat HighlightedStyle = new()
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
        private readonly OptionButton _genderButton;
        private readonly OptionButton _clothingButton;
        private readonly OptionButton _backpackButton;
        private readonly HairStylePicker _hairPicker;
        private readonly HairStylePicker _facialHairPicker;
        private readonly EyeColorPicker _eyesPicker;

        private readonly List<JobPrioritySelector> _jobPriorities;
        private readonly OptionButton _preferenceUnavailableButton;
        private readonly Dictionary<string, VBoxContainer> _jobCategories;

        private readonly List<AntagPreferenceSelector> _antagPreferences;

        private readonly IEntity _previewDummy;
        private readonly SpriteView _previewSprite;
        private readonly SpriteView _previewSpriteSide;

        private bool _isDirty;
        private bool _needUpdatePreview;
        public int CharacterSlot;
        public HumanoidCharacterProfile? Profile;

        public event Action<HumanoidCharacterProfile, int>? OnProfileChanged;

        public HumanoidProfileEditor(IClientPreferencesManager preferencesManager, IPrototypeManager prototypeManager,
            IEntityManager entityManager)
        {
            _random = IoCManager.Resolve<IRobustRandom>();
            _prototypeManager = prototypeManager;

            _preferencesManager = preferencesManager;

            var hbox = new HBoxContainer();
            AddChild(hbox);

            #region Left

            var vBox = new VBoxContainer {Margin = new Thickness(10)};
            hbox.AddChild(vBox);

            var middleContainer = new HBoxContainer
            {
                SeparationOverride = 10
            };
            vBox.AddChild(middleContainer);

            var leftColumn = new VBoxContainer();
            middleContainer.AddChild(leftColumn);

            #region Randomize

            var randomizePanel = HighlightedContainer();
            var randomizeEverythingButton = new Button
            {
                Text = Loc.GetString("Randomize everything")
            };
            randomizeEverythingButton.OnPressed += args => { RandomizeEverything(); };
            randomizePanel.AddChild(randomizeEverythingButton);
            leftColumn.AddChild(randomizePanel);

            #endregion Randomize

            #region Name

            var namePanel = HighlightedContainer();
            var nameHBox = new HBoxContainer
            {
                VerticalExpand = true
            };
            var nameLabel = new Label { Text = Loc.GetString("Name:") };
            _nameEdit = new LineEdit
            {
                MinSize = (270, 0),
                VerticalAlignment = VAlignment.Center
            };
            _nameEdit.OnTextChanged += args => { SetName(args.Text); };
            var nameRandomButton = new Button
            {
                Text = Loc.GetString("Randomize"),
            };
            nameRandomButton.OnPressed += args => RandomizeName();
            nameHBox.AddChild(nameLabel);
            nameHBox.AddChild(_nameEdit);
            nameHBox.AddChild(nameRandomButton);
            randomizePanel.AddChild(nameHBox);

            #endregion Name

            var tabContainer = new TabContainer {VerticalExpand = true};
            vBox.AddChild(tabContainer);

            #region Appearance

            var appearanceList = new VBoxContainer();

            var appearanceVBox = new VBoxContainer
            {
                Children =
                {
                    new ScrollContainer
                    {
                        VerticalExpand = true,
                        Children =
                        {
                            appearanceList
                        }
                    }
                }
            };
            tabContainer.AddChild(appearanceVBox);
            tabContainer.SetTabTitle(0, Loc.GetString("Appearance"));

            var sexAndAgeRow = new HBoxContainer
            {
                SeparationOverride = 10
            };

            appearanceList.AddChild(sexAndAgeRow);

            #region Sex

            var sexPanel = HighlightedContainer();
            var sexHBox = new HBoxContainer();
            var sexLabel = new Label { Text = Loc.GetString("Sex:") };

            var sexButtonGroup = new ButtonGroup();

            _sexMaleButton = new Button
            {
                Text = Loc.GetString("Male"),
                Group = sexButtonGroup
            };
            _sexMaleButton.OnPressed += args =>
            {
                SetSex(Sex.Male);
                if (Profile?.Gender == Gender.Female)
                {
                    SetGender(Gender.Male);
                    UpdateGenderControls();
                }
            };

            _sexFemaleButton = new Button
            {
                Text = Loc.GetString("Female"),
                Group = sexButtonGroup
            };
            _sexFemaleButton.OnPressed += _ =>
            {
                SetSex(Sex.Female);

                if (Profile?.Gender == Gender.Male)
                {
                    SetGender(Gender.Female);
                    UpdateGenderControls();
                }
            };

            sexHBox.AddChild(sexLabel);
            sexHBox.AddChild(_sexMaleButton);
            sexHBox.AddChild(_sexFemaleButton);
            sexPanel.AddChild(sexHBox);
            sexAndAgeRow.AddChild(sexPanel);

            #endregion Sex

            #region Age

            var agePanel = HighlightedContainer();
            var ageHBox = new HBoxContainer();
            var ageLabel = new Label { Text = Loc.GetString("Age:") };
            _ageEdit = new LineEdit { MinSize = (40, 0) };
            _ageEdit.OnTextChanged += args =>
            {
                if (!int.TryParse(args.Text, out var newAge))
                    return;
                SetAge(newAge);
            };
            ageHBox.AddChild(ageLabel);
            ageHBox.AddChild(_ageEdit);
            agePanel.AddChild(ageHBox);
            sexAndAgeRow.AddChild(agePanel);

            #endregion Age

            #region Gender

            var genderPanel = HighlightedContainer();
            var genderHBox = new HBoxContainer();
            var genderLabel = new Label { Text = Loc.GetString("Pronouns:") };

            _genderButton = new OptionButton();

            _genderButton.AddItem(Loc.GetString("He / Him"), (int) Gender.Male);
            _genderButton.AddItem(Loc.GetString("She / Her"), (int) Gender.Female);
            _genderButton.AddItem(Loc.GetString("They / Them"), (int) Gender.Epicene);
            _genderButton.AddItem(Loc.GetString("It / It"), (int) Gender.Neuter);

            _genderButton.OnItemSelected += args =>
            {
                _genderButton.SelectId(args.Id);
                SetGender((Gender) args.Id);
            };

            genderHBox.AddChild(genderLabel);
            genderHBox.AddChild(_genderButton);
            genderPanel.AddChild(genderHBox);
            sexAndAgeRow.AddChild(genderPanel);

            #endregion Gender

            #region Hair

            var hairPanel = HighlightedContainer();
            var hairHBox = new HBoxContainer();

            _hairPicker = new HairStylePicker
            {
                HorizontalAlignment = HAlignment.Center
            };
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

            _facialHairPicker = new HairStylePicker();
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

            hairPanel.AddChild(hairHBox);
            appearanceList.AddChild(hairPanel);

            #endregion Hair

            #region Clothing

            var clothingPanel = HighlightedContainer();
            var clothingHBox = new HBoxContainer();
            var clothingLabel = new Label { Text = Loc.GetString("Clothing:") };

            _clothingButton = new OptionButton();

            _clothingButton.AddItem(Loc.GetString("Jumpsuit"), (int) ClothingPreference.Jumpsuit);
            _clothingButton.AddItem(Loc.GetString("Jumpskirt"), (int) ClothingPreference.Jumpskirt);

            _clothingButton.OnItemSelected += args =>
            {
                _clothingButton.SelectId(args.Id);
                SetClothing((ClothingPreference) args.Id);
            };

            clothingHBox.AddChild(clothingLabel);
            clothingHBox.AddChild(_clothingButton);
            clothingPanel.AddChild(clothingHBox);
            appearanceList.AddChild(clothingPanel);

            #endregion Clothing

            #region Backpack

            var backpackPanel = HighlightedContainer();
            var backpackHBox = new HBoxContainer();
            var backpackLabel = new Label { Text = Loc.GetString("Backpack:") };

            _backpackButton = new OptionButton();

            _backpackButton.AddItem(Loc.GetString("Backpack"), (int) BackpackPreference.Backpack);
            _backpackButton.AddItem(Loc.GetString("Satchel"), (int) BackpackPreference.Satchel);
            _backpackButton.AddItem(Loc.GetString("Duffelbag"), (int) BackpackPreference.Duffelbag);

            _backpackButton.OnItemSelected += args =>
            {
                _backpackButton.SelectId(args.Id);
                SetBackpack((BackpackPreference) args.Id);
            };

            backpackHBox.AddChild(backpackLabel);
            backpackHBox.AddChild(_backpackButton);
            backpackPanel.AddChild(backpackHBox);
            appearanceList.AddChild(backpackPanel);

            #endregion Backpack

            #region Eyes

            var eyesPanel = HighlightedContainer();
            var eyesVBox = new VBoxContainer();
            var eyesLabel = new Label { Text = Loc.GetString("Eye color:") };

            _eyesPicker = new EyeColorPicker();

            _eyesPicker.OnEyeColorPicked += newColor =>
            {
                if (Profile is null)
                    return;
                Profile = Profile.WithCharacterAppearance(
                    Profile.Appearance.WithEyeColor(newColor));
                IsDirty = true;
            };

            eyesVBox.AddChild(eyesLabel);
            eyesVBox.AddChild(_eyesPicker);
            eyesPanel.AddChild(eyesVBox);
            appearanceList.AddChild(eyesPanel);

            #endregion Eyes

            #endregion Appearance

            #region Jobs

            var jobList = new VBoxContainer();

                var jobVBox = new VBoxContainer
                {
                    Children =
                    {
                        (_preferenceUnavailableButton = new OptionButton()),
                        new ScrollContainer
                        {
                            VerticalExpand = true,
                            Children =
                            {
                                jobList
                            }
                        }
                    }
                };

            tabContainer.AddChild(jobVBox);

            tabContainer.SetTabTitle(1, Loc.GetString("Jobs"));

            _preferenceUnavailableButton.AddItem(
                Loc.GetString("Stay in lobby if preference unavailable."),
                (int) PreferenceUnavailableMode.StayInLobby);
            _preferenceUnavailableButton.AddItem(
                Loc.GetString("Be an {0} if preference unavailable.",
                    Loc.GetString(SharedGameTicker.OverflowJobName)),
                (int) PreferenceUnavailableMode.SpawnAsOverflow);

            _preferenceUnavailableButton.OnItemSelected += args =>
            {
                _preferenceUnavailableButton.SelectId(args.Id);

                Profile = Profile?.WithPreferenceUnavailable((PreferenceUnavailableMode) args.Id);
                IsDirty = true;
            };

            _jobPriorities = new List<JobPrioritySelector>();
            _jobCategories = new Dictionary<string, VBoxContainer>();

            var firstCategory = true;

            foreach (var job in prototypeManager.EnumeratePrototypes<JobPrototype>().OrderBy(j => j.Name))
            {
                foreach (var department in job.Departments)
                {
                    if (!_jobCategories.TryGetValue(department, out var category))
                    {
                        category = new VBoxContainer
                        {
                            Name = department,
                            ToolTip = Loc.GetString("Jobs in the {0} department", department)
                        };

                            if (firstCategory)
                            {
                                firstCategory = false;
                            }
                            else
                            {
                                category.AddChild(new Control
                                {
                                    MinSize = new Vector2(0, 23),
                                });
                            }

                        category.AddChild(new PanelContainer
                        {
                            PanelOverride = new StyleBoxFlat {BackgroundColor = Color.FromHex("#464966")},
                            Children =
                            {
                                new Label
                                {
                                    Text = Loc.GetString("{0} jobs", department)
                                }
                            }
                        });

                        _jobCategories[department] = category;
                        jobList.AddChild(category);
                    }

                    var selector = new JobPrioritySelector(job);
                    category.AddChild(selector);
                    _jobPriorities.Add(selector);

                    selector.PriorityChanged += priority =>
                    {
                        Profile = Profile?.WithJobPriority(job.ID, priority);
                        IsDirty = true;

                        foreach (var jobSelector in _jobPriorities)
                        {
                            // Sync other selectors with the same job in case of multiple department jobs
                            if (jobSelector.Job == selector.Job)
                            {
                                jobSelector.Priority = priority;
                            }

                            // Lower any other high priorities to medium.
                            if (priority == JobPriority.High)
                            {
                                if (jobSelector.Job != selector.Job && jobSelector.Priority == JobPriority.High)
                                {
                                    jobSelector.Priority = JobPriority.Medium;
                                    Profile = Profile?.WithJobPriority(jobSelector.Job.ID, JobPriority.Medium);
                                }
                            }
                        }
                    };
                }
            }

            #endregion Jobs

            #region Antags

            var antagList = new VBoxContainer();

                var antagVBox = new VBoxContainer
                {
                    Children =
                    {
                        new ScrollContainer
                        {
                            VerticalExpand = true,
                            Children =
                            {
                                antagList
                            }
                        }
                    }
                };

            tabContainer.AddChild(antagVBox);

            tabContainer.SetTabTitle(2, Loc.GetString("Antags"));

            _antagPreferences = new List<AntagPreferenceSelector>();

                foreach (var antag in prototypeManager.EnumeratePrototypes<AntagPrototype>().OrderBy(a => a.Name))
                {
                    if (!antag.SetPreference)
                    {
                        continue;
                    }

                    var selector = new AntagPreferenceSelector(antag);
                    antagList.AddChild(selector);
                    _antagPreferences.Add(selector);

                selector.PreferenceChanged += preference =>
                {
                    Profile = Profile?.WithAntagPreference(antag.ID, preference);
                    IsDirty = true;
                };
            }

            #endregion Antags

            var rightColumn = new VBoxContainer();
            middleContainer.AddChild(rightColumn);

            #region Import/Export

            var importExportPanelContainer = HighlightedContainer();
            var importExportHBox = new HBoxContainer();
            var importButton = new Button
            {
                Text = Loc.GetString("Import"),
                Disabled = true,
                ToolTip = "Not yet implemented!"
            };
            var exportButton = new Button
            {
                Text = Loc.GetString("Export"),
                Disabled = true,
                ToolTip = "Not yet implemented!"
            };
            importExportHBox.AddChild(importButton);
            importExportHBox.AddChild(exportButton);
            importExportPanelContainer.AddChild(importExportHBox);
            rightColumn.AddChild(importExportPanelContainer);

            #endregion Import/Export

            #region Save

            {
                var panel = HighlightedContainer();
                _saveButton = new Button
                {
                    Text = Loc.GetString("Save"),
                    HorizontalAlignment = HAlignment.Center
                };
                _saveButton.OnPressed += args => { Save(); };
                panel.AddChild(_saveButton);
                rightColumn.AddChild(panel);
            }

            #endregion Save

            #endregion Left

            #region Right

            vBox = new VBoxContainer()
            {
                VerticalExpand = true,
                HorizontalExpand = true,
            };
            hbox.AddChild(vBox);

            #region Preview

            _previewDummy = entityManager.SpawnEntity("HumanMob_Dummy", MapCoordinates.Nullspace);
            var sprite = _previewDummy.GetComponent<SpriteComponent>();

            // Front
            var box = new Control()
            {
                VerticalExpand = true,
                SizeFlagsStretchRatio = 1f,
            };
            vBox.AddChild(box);
            _previewSprite = new SpriteView
            {
                Sprite = sprite,
                Scale = (6, 6),
                OverrideDirection = Direction.South,
                VerticalAlignment = VAlignment.Center,
                SizeFlagsStretchRatio = 1
            };
            box.AddChild(_previewSprite);

            // Side
            box = new Control()
            {
                VerticalExpand = true,
                SizeFlagsStretchRatio = 1f,
            };
            vBox.AddChild(box);
            _previewSpriteSide = new SpriteView
            {
                Sprite = sprite,
                Scale = (6, 6),
                OverrideDirection = Direction.East,
                VerticalAlignment = VAlignment.Center,
                SizeFlagsStretchRatio = 1
            };
            box.AddChild(_previewSpriteSide);

            #endregion Right

            #endregion

            if (preferencesManager.ServerDataLoaded)
            {
                LoadServerData();
            }

            preferencesManager.OnServerDataLoaded += LoadServerData;

            IsDirty = false;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            _previewDummy.Delete();
            _preferencesManager.OnServerDataLoaded -= LoadServerData;
        }

        private void LoadServerData()
        {
            Profile = (HumanoidCharacterProfile) _preferencesManager.Preferences!.SelectedCharacter;
            CharacterSlot = _preferencesManager.Preferences.SelectedCharacterIndex;
            UpdateControls();
        }

        private void SetAge(int newAge)
        {
            Profile = Profile?.WithAge(newAge);
            IsDirty = true;
        }

        private void SetSex(Sex newSex)
        {
            Profile = Profile?.WithSex(newSex);
            IsDirty = true;
        }

        private void SetGender(Gender newGender)
        {
            Profile = Profile?.WithGender(newGender);
            IsDirty = true;
        }

        private void SetName(string newName)
        {
            Profile = Profile?.WithName(newName);
            IsDirty = true;
        }

        private void SetClothing(ClothingPreference newClothing)
        {
            Profile = Profile?.WithClothingPreference(newClothing);
            IsDirty = true;
        }

        private void SetBackpack(BackpackPreference newBackpack)
        {
            Profile = Profile?.WithBackpackPreference(newBackpack);
            IsDirty = true;
        }

        public void Save()
        {
            IsDirty = false;

            if (Profile != null)
            {
                _preferencesManager.UpdateCharacter(Profile, CharacterSlot);
                OnProfileChanged?.Invoke(Profile, CharacterSlot);
            }
        }

        private bool IsDirty
        {
            get => _isDirty;
            set
            {
                _isDirty = value;
                _needUpdatePreview = true;
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

        private void UpdateNameEdit()
        {
            _nameEdit.Text = Profile?.Name ?? "";
        }

        private void UpdateAgeEdit()
        {
            _ageEdit.Text = Profile?.Age.ToString() ?? "";
        }

        private void UpdateSexControls()
        {
            if (Profile?.Sex == Sex.Male)
                _sexMaleButton.Pressed = true;
            else
                _sexFemaleButton.Pressed = true;
        }

        private void UpdateGenderControls()
        {
            if (Profile == null)
            {
                return;
            }

            _genderButton.SelectId((int) Profile.Gender);
        }

        private void UpdateClothingControls()
        {
            if (Profile == null)
            {
                return;
            }

            _clothingButton.SelectId((int) Profile.Clothing);
        }

        private void UpdateBackpackControls()
        {
            if (Profile == null)
            {
                return;
            }

            _backpackButton.SelectId((int) Profile.Backpack);
        }

        private void UpdateHairPickers()
        {
            if (Profile == null)
            {
                return;
            }

            _hairPicker.SetData(
                Profile.Appearance.HairColor,
                Profile.Appearance.HairStyleId,
                SpriteAccessoryCategories.HumanHair,
                true);
            _facialHairPicker.SetData(
                Profile.Appearance.FacialHairColor,
                Profile.Appearance.FacialHairStyleId,
                SpriteAccessoryCategories.HumanFacialHair,
                true);
        }

        private void UpdateEyePickers()
        {
            if (Profile == null)
            {
                return;
            }

            _eyesPicker.SetData(Profile.Appearance.EyeColor);
        }

        private void UpdateSaveButton()
        {
            _saveButton.Disabled = Profile is null || !IsDirty;
        }

        private void UpdatePreview()
        {
            if (Profile is null)
                return;

            _previewDummy.GetComponent<HumanoidAppearanceComponent>().UpdateFromProfile(Profile);
            LobbyCharacterPreviewPanel.GiveDummyJobClothes(_previewDummy, Profile);
        }

        public void UpdateControls()
        {
            if (Profile is null) return;
            UpdateNameEdit();
            UpdateSexControls();
            UpdateGenderControls();
            UpdateClothingControls();
            UpdateBackpackControls();
            UpdateAgeEdit();
            UpdateHairPickers();
            UpdateEyePickers();
            UpdateSaveButton();
            UpdateJobPriorities();
            UpdateAntagPreferences();

            _needUpdatePreview = true;

            _preferenceUnavailableButton.SelectId((int) Profile.PreferenceUnavailable);
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            if (_needUpdatePreview)
            {
                UpdatePreview();

                _needUpdatePreview = false;
            }
        }

        private void UpdateJobPriorities()
        {
            foreach (var prioritySelector in _jobPriorities)
            {
                var jobId = prioritySelector.Job.ID;

                var priority = Profile?.JobPriorities.GetValueOrDefault(jobId, JobPriority.Never) ?? JobPriority.Never;

                prioritySelector.Priority = priority;
            }
        }

        private class JobPrioritySelector : Control
        {
            public JobPrototype Job { get; }
            private readonly RadioOptions<int> _optionButton;

            public JobPriority Priority
            {
                get => (JobPriority) _optionButton.SelectedValue;
                set => _optionButton.SelectByValue((int) value);
            }

            public event Action<JobPriority>? PriorityChanged;

            public JobPrioritySelector(JobPrototype job)
            {
                Job = job;

                _optionButton = new RadioOptions<int>(RadioOptionsLayout.Horizontal)
                {
                    FirstButtonStyle = StyleBase.ButtonOpenRight,
                    ButtonStyle = StyleBase.ButtonOpenBoth,
                    LastButtonStyle = StyleBase.ButtonOpenLeft
                };

                // Text, Value
                _optionButton.AddItem(Loc.GetString("High"), (int) JobPriority.High);
                _optionButton.AddItem(Loc.GetString("Medium"), (int) JobPriority.Medium);
                _optionButton.AddItem(Loc.GetString("Low"), (int) JobPriority.Low);
                _optionButton.AddItem(Loc.GetString("Never"), (int) JobPriority.Never);

                _optionButton.OnItemSelected += args =>
                {
                    _optionButton.Select(args.Id);
                    PriorityChanged?.Invoke(Priority);
                };

                var icon = new TextureRect
                {
                    TextureScale = (2, 2),
                    Stretch = TextureRect.StretchMode.KeepCentered
                };

                if (job.Icon != null)
                {
                    var specifier = new SpriteSpecifier.Rsi(new ResourcePath("/Textures/Interface/Misc/job_icons.rsi"),
                        job.Icon);
                    icon.Texture = specifier.Frame0();
                }

                AddChild(new HBoxContainer
                {
                    Children =
                    {
                        icon,
                        new Label {Text = job.Name, MinSize = (175, 0)},
                        _optionButton
                    }
                });
            }
        }

        private void UpdateAntagPreferences()
        {
            foreach (var preferenceSelector in _antagPreferences)
            {
                var antagId = preferenceSelector.Antag.ID;
                var preference = Profile?.AntagPreferences.Contains(antagId) ?? false;

                preferenceSelector.Preference = preference;
            }
        }

        private class AntagPreferenceSelector : Control
        {
            public AntagPrototype Antag { get; }
            private readonly CheckBox _checkBox;

            public bool Preference
            {
                get => _checkBox.Pressed;
                set => _checkBox.Pressed = value;
            }

            public event Action<bool>? PreferenceChanged;

            public AntagPreferenceSelector(AntagPrototype antag)
            {
                Antag = antag;

                _checkBox = new CheckBox {Text = $"{antag.Name}"};
                _checkBox.OnToggled += OnCheckBoxToggled;

                AddChild(new HBoxContainer
                {
                    Children =
                    {
                        _checkBox
                    }
                });
            }

            private void OnCheckBoxToggled(BaseButton.ButtonToggledEventArgs args)
            {
                PreferenceChanged?.Invoke(Preference);
            }
        }
    }
}
