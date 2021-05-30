using System.Linq;
using Content.Client.GameObjects.Components.Mobs;
using Content.Client.Interfaces;
using Content.Client.UserInterface.Stylesheets;
using Content.Client.Utility;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.UserInterface
{
    public class CharacterSetupGui : Control
    {
        private readonly VBoxContainer _charactersVBox;
        private readonly Button _createNewCharacterButton;
        private readonly IEntityManager _entityManager;
        private readonly HumanoidProfileEditor _humanoidProfileEditor;
        private readonly IClientPreferencesManager _preferencesManager;
        public readonly Button CloseButton;
        public readonly Button SaveButton;

        public CharacterSetupGui(
            IEntityManager entityManager,
            IResourceCache resourceCache,
            IClientPreferencesManager preferencesManager,
            IPrototypeManager prototypeManager)
        {
            AddChild(new ParallaxControl());

            _entityManager = entityManager;
            _preferencesManager = preferencesManager;
            var margin = new Control
            {
                Margin = new Thickness(20),
            };

            AddChild(margin);

            var panelTex = resourceCache.GetTexture("/Textures/Interface/Nano/button.svg.96dpi.png");
            var back = new StyleBoxTexture
            {
                Texture = panelTex,
                Modulate = new Color(37, 37, 42)
            };
            back.SetPatchMargin(StyleBox.Margin.All, 10);

            var panel = new PanelContainer
            {
                PanelOverride = back
            };

            margin.AddChild(panel);

            var vBox = new VBoxContainer {SeparationOverride = 0};

            margin.AddChild(vBox);

            var topHBox = new HBoxContainer
            {
                MinSize = (0, 40),
                Children =
                {
                    new Label
                    {
                        Margin = new Thickness(8, 0, 0, 0),
                        Text = Loc.GetString("Character Setup"),
                        StyleClasses = {StyleNano.StyleClassLabelHeadingBigger},
                        VAlign = Label.VAlignMode.Center,
                    },
                    (SaveButton = new Button
                    {
                        HorizontalExpand = true,
                        HorizontalAlignment = HAlignment.Right,
                        Text = Loc.GetString("Save"),
                        StyleClasses = {StyleNano.StyleClassButtonBig},
                    }),
                    (CloseButton = new Button
                    {
                        Text = Loc.GetString("Close"),
                        StyleClasses = {StyleNano.StyleClassButtonBig},
                    })
                }
            };

            vBox.AddChild(topHBox);

            vBox.AddChild(new PanelContainer
            {
                PanelOverride = new StyleBoxFlat
                {
                    BackgroundColor = StyleNano.NanoGold,
                    ContentMarginTopOverride = 2
                }
            });

            var hBox = new HBoxContainer
            {
                VerticalExpand = true,
                SeparationOverride = 0
            };
            vBox.AddChild(hBox);

            _charactersVBox = new VBoxContainer();

            hBox.AddChild(new ScrollContainer
            {
                MinSize = (325, 0),
                Margin = new Thickness(5, 5, 0, 0),
                Children =
                {
                    _charactersVBox
                }
            });

            _createNewCharacterButton = new Button
            {
                Text = "Create new slot...",
            };
            _createNewCharacterButton.OnPressed += args =>
            {
                preferencesManager.CreateCharacter(HumanoidCharacterProfile.Random());
                UpdateUI();
                args.Event.Handle();
            };

            hBox.AddChild(new PanelContainer
            {
                PanelOverride = new StyleBoxFlat {BackgroundColor = StyleNano.NanoGold},
                MinSize = (2, 0)
            });
            _humanoidProfileEditor = new HumanoidProfileEditor(preferencesManager, prototypeManager, entityManager);
            _humanoidProfileEditor.OnProfileChanged += ProfileChanged;
            hBox.AddChild(_humanoidProfileEditor);

            UpdateUI();

            preferencesManager.OnServerDataLoaded += UpdateUI;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            _preferencesManager.OnServerDataLoaded -= UpdateUI;
        }

        public void Save() => _humanoidProfileEditor.Save();

        private void ProfileChanged(ICharacterProfile profile, int profileSlot)
        {
            _humanoidProfileEditor.UpdateControls();
            UpdateUI();
        }

        private void UpdateUI()
        {
            var numberOfFullSlots = 0;
            var characterButtonsGroup = new ButtonGroup();
            _charactersVBox.RemoveAllChildren();

            if (!_preferencesManager.ServerDataLoaded)
            {
                return;
            }

            _createNewCharacterButton.ToolTip =
                $"A maximum of {_preferencesManager.Settings!.MaxCharacterSlots} characters are allowed.";

            foreach (var (slot, character) in _preferencesManager.Preferences!.Characters)
            {
                if (character is null)
                {
                    continue;
                }

                numberOfFullSlots++;
                var characterPickerButton = new CharacterPickerButton(_entityManager,
                    _preferencesManager,
                    characterButtonsGroup,
                    character);
                _charactersVBox.AddChild(characterPickerButton);

                var characterIndexCopy = slot;
                characterPickerButton.OnPressed += args =>
                {
                    _humanoidProfileEditor.Profile = (HumanoidCharacterProfile) character;
                    _humanoidProfileEditor.CharacterSlot = characterIndexCopy;
                    _humanoidProfileEditor.UpdateControls();
                    _preferencesManager.SelectCharacter(character);
                    UpdateUI();
                    args.Event.Handle();
                };
            }

            _createNewCharacterButton.Disabled =
                numberOfFullSlots >= _preferencesManager.Settings.MaxCharacterSlots;
            _charactersVBox.AddChild(_createNewCharacterButton);
        }

        private class CharacterPickerButton : ContainerButton
        {
            private IEntity _previewDummy;

            public CharacterPickerButton(
                IEntityManager entityManager,
                IClientPreferencesManager preferencesManager,
                ButtonGroup group,
                ICharacterProfile profile)
            {
                AddStyleClass(StyleClassButton);
                ToggleMode = true;
                Group = group;

                _previewDummy = entityManager.SpawnEntity("HumanMob_Dummy", MapCoordinates.Nullspace);
                _previewDummy.GetComponent<HumanoidAppearanceComponent>().UpdateFromProfile(profile);
                var humanoid = profile as HumanoidCharacterProfile;
                if (humanoid != null)
                {
                    LobbyCharacterPreviewPanel.GiveDummyJobClothes(_previewDummy, humanoid);
                }

                var isSelectedCharacter = profile == preferencesManager.Preferences?.SelectedCharacter;

                if (isSelectedCharacter)
                    Pressed = true;

                var view = new SpriteView
                {
                    Sprite = _previewDummy.GetComponent<SpriteComponent>(),
                    Scale = (2, 2),
                    OverrideDirection = Direction.South
                };

                var description = profile.Name;

                var highPriorityJob = humanoid?.JobPriorities.SingleOrDefault(p => p.Value == JobPriority.High).Key;
                if (highPriorityJob != null)
                {
                    var jobName = IoCManager.Resolve<IPrototypeManager>().Index<JobPrototype>(highPriorityJob).Name;
                    description = $"{description}\n{jobName}";
                }

                var descriptionLabel = new Label
                {
                    Text = description,
                    ClipText = true,
                    HorizontalExpand = true
                };
                var deleteButton = new Button
                {
                    Text = "Delete",
                    Visible = !isSelectedCharacter,
                };
                deleteButton.OnPressed += _ =>
                {
                    Parent?.RemoveChild(this);
                    preferencesManager.DeleteCharacter(profile);
                };

                var internalHBox = new HBoxContainer
                {
                    HorizontalExpand = true,
                    SeparationOverride = 0,
                    Children =
                    {
                        view,
                        descriptionLabel,
                        deleteButton
                    }
                };

                AddChild(internalHBox);
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                if (!disposing)
                    return;

                _previewDummy.Delete();
                _previewDummy = null!;
            }
        }
    }
}
