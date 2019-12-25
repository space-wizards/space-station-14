using Content.Client.GameObjects.Components.Mobs;
using Content.Client.Interfaces;
using Content.Client.Utility;
using Content.Shared.Preferences;
using Robust.Client.GameObjects;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Client.UserInterface
{
    public class CharacterSetupGui : Control
    {
        private readonly VBoxContainer _charactersVBox;
        private readonly Button _createNewCharacterButton;
        private readonly IEntityManager _entityManager;
        private readonly HumanoidProfileEditorPanel _humanoidProfileEditorPanel;
        private readonly IClientPreferencesManager _preferencesManager;
        public readonly Button CloseButton;

        public CharacterSetupGui(IEntityManager entityManager,
            ILocalizationManager localization,
            IResourceCache resourceCache,
            IClientPreferencesManager preferencesManager)
        {
            _entityManager = entityManager;
            _preferencesManager = preferencesManager;
            var margin = new MarginContainer
            {
                MarginBottomOverride = 20,
                MarginLeftOverride = 20,
                MarginRightOverride = 20,
                MarginTopOverride = 20
            };

            AddChild(margin);

            var panelTex = resourceCache.GetTexture("/Nano/button.svg.96dpi.png");
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

            CloseButton = new Button
            {
                SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.ShrinkEnd,
                Text = localization.GetString("Save and close"),
                StyleClasses = {NanoStyle.StyleClassButtonBig}
            };

            var topHBox = new HBoxContainer
            {
                CustomMinimumSize = (0, 40),
                Children =
                {
                    new MarginContainer
                    {
                        MarginLeftOverride = 8,
                        Children =
                        {
                            new Label
                            {
                                Text = localization.GetString("Character Setup"),
                                StyleClasses = {NanoStyle.StyleClassLabelHeadingBigger},
                                VAlign = Label.VAlignMode.Center,
                                SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.ShrinkCenter
                            }
                        }
                    },
                    CloseButton
                }
            };

            vBox.AddChild(topHBox);

            vBox.AddChild(new PanelContainer
            {
                PanelOverride = new StyleBoxFlat
                {
                    BackgroundColor = NanoStyle.NanoGold,
                    ContentMarginTopOverride = 2
                }
            });

            var hBox = new HBoxContainer
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
                SeparationOverride = 0
            };
            vBox.AddChild(hBox);

            _charactersVBox = new VBoxContainer();

            hBox.AddChild(new MarginContainer
            {
                CustomMinimumSize = (420, 0),
                SizeFlagsHorizontal = SizeFlags.Fill,
                MarginTopOverride = 5,
                MarginLeftOverride = 5,
                Children =
                {
                    new ScrollContainer
                    {
                        SizeFlagsVertical = SizeFlags.FillExpand,
                        Children =
                        {
                            _charactersVBox
                        }
                    }
                }
            });

            _createNewCharacterButton = new Button
            {
                Text = "Create new slot...",
                ToolTip = $"A maximum of {preferencesManager.Settings.MaxCharacterSlots} characters are allowed."
            };
            _createNewCharacterButton.OnPressed += args =>
            {
                preferencesManager.CreateCharacter(HumanoidCharacterProfile.Default());
                UpdateUI();
            };

            hBox.AddChild(new PanelContainer
            {
                PanelOverride = new StyleBoxFlat {BackgroundColor = NanoStyle.NanoGold},
                CustomMinimumSize = (2, 0)
            });
            _humanoidProfileEditorPanel = new HumanoidProfileEditorPanel(localization,
                resourceCache,
                preferencesManager
            );
            _humanoidProfileEditorPanel.OnProfileChanged += newProfile => { UpdateUI(); };
            hBox.AddChild(_humanoidProfileEditorPanel);

            UpdateUI();
        }

        private void UpdateUI()
        {
            var numberOfFullSlots = 0;
            var characterButtonsGroup = new ButtonGroup();
            _charactersVBox.RemoveAllChildren();
            var characterIndex = 0;
            foreach (var character in _preferencesManager.Preferences.Characters)
            {
                if (character is null)
                {
                    characterIndex++;
                    continue;
                }

                numberOfFullSlots++;
                var characterPickerButton = new CharacterPickerButton(_entityManager,
                    _preferencesManager,
                    characterButtonsGroup,
                    character,
                    character.Name,
                    character.Name,
                    "Assistant");
                _charactersVBox.AddChild(characterPickerButton);

                var characterIndexCopy = characterIndex;
                characterPickerButton.ActualButton.OnPressed += args =>
                {
                    _humanoidProfileEditorPanel.Profile = (HumanoidCharacterProfile) character;
                    _humanoidProfileEditorPanel.CharacterSlot = characterIndexCopy;
                    _humanoidProfileEditorPanel.UpdateControls();
                    _preferencesManager.SelectCharacter(character);
                };
                characterIndex++;
            }

            _createNewCharacterButton.Disabled =
                numberOfFullSlots >= _preferencesManager.Settings.MaxCharacterSlots;
            _charactersVBox.AddChild(_createNewCharacterButton);
        }

        private class CharacterPickerButton : Control
        {
            public readonly Button ActualButton;

            public CharacterPickerButton(
                IEntityManager entityManager,
                IClientPreferencesManager preferencesManager,
                ButtonGroup group,
                ICharacterProfile profile,
                string slotName,
                string characterName,
                string jobTitle)
            {
                var previewDummy = entityManager.SpawnEntityAt("HumanMob_Content",
                    new MapCoordinates(Vector2.Zero, MapId.Nullspace));
                previewDummy.GetComponent<LooksComponent>().UpdateFromProfile(profile);

                var spriteComponent = previewDummy.GetComponent<SpriteComponent>();

                ActualButton = new Button
                {
                    SizeFlagsHorizontal = SizeFlags.FillExpand,
                    SizeFlagsVertical = SizeFlags.FillExpand,
                    ToggleMode = true,
                    Group = group
                };
                AddChild(ActualButton);

                var view = new SpriteView
                {
                    Sprite = spriteComponent,
                    Scale = (2, 2),
                    MouseFilter = MouseFilterMode.Ignore,
                    OverrideDirection = Direction.South
                };
                if (slotName != characterName) slotName = $"({slotName}) {characterName}";

                var descriptionLabel = new Label
                {
                    Text = $"{slotName}\n{jobTitle}"
                };
                var deleteButton = new Button
                {
                    Text = "Delete",
                    Visible = profile != preferencesManager.Preferences.SelectedCharacter,
                    SizeFlagsHorizontal = SizeFlags.ShrinkEnd
                };
                deleteButton.OnPressed += args =>
                {
                    Parent.RemoveChild(this);
                    preferencesManager.DeleteCharacter(profile);
                };

                var internalHBox = new HBoxContainer
                {
                    SizeFlagsHorizontal = SizeFlags.FillExpand,
                    MouseFilter = MouseFilterMode.Ignore,
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
        }
    }
}
