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

        public CharacterSetupGui(IEntityManager entityManager,
            ILocalizationManager localization,
            IResourceCache resourceCache,
            IClientPreferencesManager preferencesManager,
            IPrototypeManager prototypeManager)
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
            _humanoidProfileEditor = new HumanoidProfileEditor(localization, preferencesManager, prototypeManager);
            _humanoidProfileEditor.OnProfileChanged += newProfile => { UpdateUI(); };
            hBox.AddChild(_humanoidProfileEditor);

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
                    character);
                _charactersVBox.AddChild(characterPickerButton);

                var characterIndexCopy = characterIndex;
                characterPickerButton.ActualButton.OnPressed += args =>
                {
                    _humanoidProfileEditor.Profile = (HumanoidCharacterProfile) character;
                    _humanoidProfileEditor.CharacterSlot = characterIndexCopy;
                    _humanoidProfileEditor.UpdateControls();
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
            private IEntity _previewDummy;

            public CharacterPickerButton(
                IEntityManager entityManager,
                IClientPreferencesManager preferencesManager,
                ButtonGroup group,
                ICharacterProfile profile)
            {
                _previewDummy = entityManager.SpawnEntityAt("HumanMob_Dummy",
                    new MapCoordinates(Vector2.Zero, MapId.Nullspace));
                _previewDummy.GetComponent<HumanoidAppearanceComponent>().UpdateFromProfile(profile);

                var isSelectedCharacter = profile == preferencesManager.Preferences.SelectedCharacter;

                ActualButton = new Button
                {
                    SizeFlagsHorizontal = SizeFlags.FillExpand,
                    SizeFlagsVertical = SizeFlags.FillExpand,
                    ToggleMode = true,
                    Group = group
                };
                if (isSelectedCharacter)
                    ActualButton.Pressed = true;
                AddChild(ActualButton);

                var view = new SpriteView
                {
                    Sprite = _previewDummy.GetComponent<SpriteComponent>(),
                    Scale = (2, 2),
                    MouseFilter = MouseFilterMode.Ignore,
                    OverrideDirection = Direction.South
                };

                var descriptionLabel = new Label
                {
                    Text = $"{profile.Name}\nAssistant" //TODO implement job selection
                };
                var deleteButton = new Button
                {
                    Text = "Delete",
                    Visible = !isSelectedCharacter,
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

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                if (!disposing) return;
                _previewDummy.Delete();
                _previewDummy = null;
            }
        }
    }
}
