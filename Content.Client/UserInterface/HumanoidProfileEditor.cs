using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client.GameObjects.Components;
using Content.Client.GameObjects.Components.Mobs;
using Content.Client.Interfaces;
using Content.Client.UserInterface.Stylesheets;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Enums;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface
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
        private readonly FacialHairStylePicker _facialHairPicker;

        private readonly List<JobPrioritySelector> _jobPriorities;
        private readonly OptionButton _preferenceUnavailableButton;
        private readonly Dictionary<string, VBoxContainer> _jobCategories;

        private readonly List<AntagPreferenceSelector> _antagPreferences;

        private readonly IEntity _previewDummy;
        private readonly SpriteView _previewSprite;
        private readonly SpriteView _previewSpriteSide;

        private bool _isDirty;
        public int CharacterSlot;
        public HumanoidCharacterProfile Profile;
        public event Action<HumanoidCharacterProfile> OnProfileChanged;

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

            {
                var panel = HighlightedContainer();
                var randomizeEverythingButton = new Button
                {
                    Text = Loc.GetString("Randomize everything")
                };
                randomizeEverythingButton.OnPressed += args => { RandomizeEverything(); };
                panel.AddChild(randomizeEverythingButton);
                leftColumn.AddChild(panel);
            }

            #endregion Randomize

            #region Name

            {
                var panel = HighlightedContainer();
                var hBox = new HBoxContainer
                {
                    VerticalExpand = true
                };
                var nameLabel = new Label {Text = Loc.GetString("Name:")};
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
                nameRandomButton.OnPressed += _ => RandomizeName();
                hBox.AddChild(nameLabel);
                hBox.AddChild(_nameEdit);
                hBox.AddChild(nameRandomButton);
                panel.AddChild(hBox);
                leftColumn.AddChild(panel);
            }

            #endregion Name

            var tabContainer = new TabContainer {VerticalExpand = true};
            vBox.AddChild(tabContainer);

            #region Appearance

            {
                var appearanceVBox = new VBoxContainer();
                tabContainer.AddChild(appearanceVBox);
                tabContainer.SetTabTitle(0, Loc.GetString("Appearance"));

                var sexAndAgeRow = new HBoxContainer
                {
                    SeparationOverride = 10
                };

                appearanceVBox.AddChild(sexAndAgeRow);

                #region Sex

                {
                    var panel = HighlightedContainer();
                    var hBox = new HBoxContainer();
                    var sexLabel = new Label {Text = Loc.GetString("Sex:")};

                    var sexButtonGroup = new ButtonGroup();

                    _sexMaleButton = new Button
                    {
                        Text = Loc.GetString("Male"),
                        Group = sexButtonGroup
                    };
                    _sexMaleButton.OnPressed += args =>
                    {
                        SetSex(Sex.Male);
                        if (Profile.Gender == Gender.Female)
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
                    _sexFemaleButton.OnPressed += args =>
                    {
                        SetSex(Sex.Female);
                        if (Profile.Gender == Gender.Male)
                        {
                            SetGender(Gender.Female);
                            UpdateGenderControls();
                        }
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
                    var ageLabel = new Label {Text = Loc.GetString("Age:")};
                    _ageEdit = new LineEdit {MinSize = (40, 0)};
                    _ageEdit.OnTextChanged += args =>
                    {
                        if (!int.TryParse(args.Text, out var newAge))
                            return;
                        SetAge(newAge);
                    };
                    hBox.AddChild(ageLabel);
                    hBox.AddChild(_ageEdit);
                    panel.AddChild(hBox);
                    sexAndAgeRow.AddChild(panel);
                }

                #endregion Age

                #region Gender

                {
                    var panel = HighlightedContainer();
                    var hBox = new HBoxContainer();
                    var genderLabel = new Label {Text = Loc.GetString("Pronouns:")};

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

                    hBox.AddChild(genderLabel);
                    hBox.AddChild(_genderButton);
                    panel.AddChild(hBox);
                    sexAndAgeRow.AddChild(panel);
                }

                #endregion Gender

                #region Hair

                {
                    var panel = HighlightedContainer();
                    panel.HorizontalAlignment = HAlignment.Left;
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
                    appearanceVBox.AddChild(panel);
                }

                #endregion Hair

                #region Clothing

                {
                    var panel = HighlightedContainer();
                    var hBox = new HBoxContainer();
                    var clothingLabel = new Label {Text = Loc.GetString("Clothing:")};

                    _clothingButton = new OptionButton();

                    _clothingButton.AddItem(Loc.GetString("Jumpsuit"), (int) ClothingPreference.Jumpsuit);
                    _clothingButton.AddItem(Loc.GetString("Jumpskirt"), (int) ClothingPreference.Jumpskirt);

                    _clothingButton.OnItemSelected += args =>
                    {
                        _clothingButton.SelectId(args.Id);
                        SetClothing((ClothingPreference) args.Id);
                    };

                    hBox.AddChild(clothingLabel);
                    hBox.AddChild(_clothingButton);
                    panel.AddChild(hBox);
                    appearanceVBox.AddChild(panel);
                }

                #endregion Clothing

                #region Backpack

                {
                    var panel = HighlightedContainer();
                    var hBox = new HBoxContainer();
                    var backpackLabel = new Label {Text = Loc.GetString("Backpack:")};

                    _backpackButton = new OptionButton();

                    _backpackButton.AddItem(Loc.GetString("Backpack"), (int) BackpackPreference.Backpack);
                    _backpackButton.AddItem(Loc.GetString("Satchel"), (int) BackpackPreference.Satchel);
                    _backpackButton.AddItem(Loc.GetString("Duffelbag"), (int) BackpackPreference.Duffelbag);

                    _backpackButton.OnItemSelected += args =>
                    {
                        _backpackButton.SelectId(args.Id);
                        SetBackpack((BackpackPreference) args.Id);
                    };

                    hBox.AddChild(backpackLabel);
                    hBox.AddChild(_backpackButton);
                    panel.AddChild(hBox);
                    appearanceVBox.AddChild(panel);
                }

                #endregion Clothing
            }

            #endregion

            #region Jobs

            {
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

                    Profile = Profile.WithPreferenceUnavailable((PreferenceUnavailableMode) args.Id);
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
                            Profile = Profile.WithJobPriority(job.ID, priority);
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
                                        Profile = Profile.WithJobPriority(jobSelector.Job.ID, JobPriority.Medium);
                                    }
                                }
                            }
                        };
                    }
                }
            }

            #endregion

            #region Antags

            {
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
                        Profile = Profile.WithAntagPreference(antag.ID, preference);
                        IsDirty = true;
                    };
                }
            }

            #endregion

            var rightColumn = new VBoxContainer();
            middleContainer.AddChild(rightColumn);

            #region Import/Export

            {
                var panelContainer = HighlightedContainer();
                var hBox = new HBoxContainer();
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
                    Text = Loc.GetString("Save"),
                    HorizontalAlignment = HAlignment.Center
                };
                _saveButton.OnPressed += args => { Save(); };
                panel.AddChild(_saveButton);
                rightColumn.AddChild(panel);
            }

            #endregion Save

            #endregion

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

            #endregion

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
            Profile = (HumanoidCharacterProfile) _preferencesManager.Preferences.SelectedCharacter;
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
            _preferencesManager.UpdateCharacter(Profile, CharacterSlot);
            OnProfileChanged?.Invoke(Profile);
        }

        private bool IsDirty
        {
            get => _isDirty;
            set
            {
                _isDirty = value;
                UpdatePreview();
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
            _nameEdit.Text = Profile.Name;
        }

        private void UpdateAgeEdit()
        {
            _ageEdit.Text = Profile.Age.ToString();
        }

        private void UpdateSexControls()
        {
            if (Profile.Sex == Sex.Male)
                _sexMaleButton.Pressed = true;
            else
                _sexFemaleButton.Pressed = true;
        }

        private void UpdateGenderControls()
        {
            _genderButton.SelectId((int) Profile.Gender);
        }

        private void UpdateClothingControls()
        {
            _clothingButton.SelectId((int) Profile.Clothing);
        }

        private void UpdateBackpackControls()
        {
            _backpackButton.SelectId((int) Profile.Backpack);
        }

        private void UpdateHairPickers()
        {
            _hairPicker.SetData(
                Profile.Appearance.HairColor,
                Profile.Appearance.HairStyleName);
            _facialHairPicker.SetData(
                Profile.Appearance.FacialHairColor,
                Profile.Appearance.FacialHairStyleName);
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
            UpdateSaveButton();
            UpdateJobPriorities();
            UpdateAntagPreferences();

            UpdatePreview();

            _preferenceUnavailableButton.SelectId((int) Profile.PreferenceUnavailable);
        }

        private void UpdateJobPriorities()
        {
            foreach (var prioritySelector in _jobPriorities)
            {
                var jobId = prioritySelector.Job.ID;

                var priority = Profile.JobPriorities.GetValueOrDefault(jobId, JobPriority.Never);

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

            public event Action<JobPriority> PriorityChanged;

            public JobPrioritySelector(JobPrototype job)
            {
                Job = job;
                _optionButton = new RadioOptions<int>(RadioOptionsLayout.Horizontal);

                _optionButton.FirstButtonStyle = StyleBase.ButtonOpenRight;
                _optionButton.ButtonStyle = StyleBase.ButtonOpenBoth;
                _optionButton.LastButtonStyle = StyleBase.ButtonOpenLeft;


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

                var preference = Profile.AntagPreferences.Contains(antagId);

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

            public event Action<bool> PreferenceChanged;

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
