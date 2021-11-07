using System;
using Content.Client.CharacterInterface;
using Content.Client.HUD.UI;
using Content.Client.Stylesheets;
using Content.Shared.CharacterInfo;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Players;

namespace Content.Client.CharacterInfo.Components
{
    [RegisterComponent]
    public sealed class CharacterInfoComponent : SharedCharacterInfoComponent, ICharacterUI
    {
        [Dependency] private readonly IResourceCache _resourceCache = default!;

        private CharacterInfoControl _control = default!;

        public Control Scene { get; private set; } = default!;
        public UIPriority Priority => UIPriority.Info;

        protected override void OnAdd()
        {
            base.OnAdd();

            Scene = _control = new CharacterInfoControl(_resourceCache);
        }

        public void Opened()
        {
#pragma warning disable 618
            SendNetworkMessage(new RequestCharacterInfoMessage());
#pragma warning restore 618
        }

        [Obsolete("Component Messages are deprecated, use Entity Events instead.")]
        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession? session = null)
        {
            base.HandleNetworkMessage(message, netChannel, session);

            switch (message)
            {
                case CharacterInfoMessage characterInfoMessage:
                    _control.UpdateUI(characterInfoMessage);
                    if (Owner.TryGetComponent(out ISpriteComponent? spriteComponent))
                    {
                        _control.SpriteView.Sprite = spriteComponent;
                    }

                    _control.NameLabel.Text = Owner.Name;
                    break;
            }
        }

        private sealed class CharacterInfoControl : BoxContainer
        {
            public SpriteView SpriteView { get; }
            public Label NameLabel { get; }
            public Label SubText { get; }

            public BoxContainer ObjectivesContainer { get; }

            public CharacterInfoControl(IResourceCache resourceCache)
            {
                IoCManager.InjectDependencies(this);

                Orientation = LayoutOrientation.Vertical;

                AddChild(new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                    Children =
                    {
                        (SpriteView = new SpriteView { OverrideDirection = Direction.South, Scale = (2,2)}),
                        new BoxContainer
                        {
                            Orientation = LayoutOrientation.Vertical,
                            VerticalAlignment = VAlignment.Top,
                            Children =
                            {
                                (NameLabel = new Label()),
                                (SubText = new Label
                                {
                                    VerticalAlignment = VAlignment.Top,
                                    StyleClasses = {StyleNano.StyleClassLabelSubText},

                                })
                            }
                        }
                    }
                });

                AddChild(new Label
                {
                    Text = Loc.GetString("character-info-objectives-label"),
                    HorizontalAlignment = HAlignment.Center
                });
                ObjectivesContainer = new BoxContainer
                {
                    Orientation = LayoutOrientation.Vertical
                };
                AddChild(ObjectivesContainer);

                AddChild(new Placeholder()
                {
                    PlaceholderText = Loc.GetString("character-info-roles-antagonist-text")
                });
            }

            public void UpdateUI(CharacterInfoMessage characterInfoMessage)
            {
                SubText.Text = characterInfoMessage.JobTitle;

                ObjectivesContainer.RemoveAllChildren();
                foreach (var (groupId, objectiveConditions) in characterInfoMessage.Objectives)
                {
                    var vbox = new BoxContainer
                    {
                        Orientation = LayoutOrientation.Vertical,
                        Modulate = Color.Gray
                    };

                    vbox.AddChild(new Label
                    {
                        Text = groupId,
                        Modulate = Color.LightSkyBlue
                    });

                    foreach (var objectiveCondition in objectiveConditions)
                    {
                        var hbox = new BoxContainer
                        {
                            Orientation = LayoutOrientation.Horizontal
                        };
                        hbox.AddChild(new ProgressTextureRect
                        {
                            Texture = objectiveCondition.SpriteSpecifier.Frame0(),
                            Progress = objectiveCondition.Progress,
                            VerticalAlignment = VAlignment.Center
                        });
                        hbox.AddChild(new Control
                        {
                            MinSize = (10,0)
                        });
                        hbox.AddChild(new BoxContainer
                            {
                                Orientation = LayoutOrientation.Vertical,
                                Children =
                                {
                                    new Label{Text = objectiveCondition.Title},
                                    new Label{Text = objectiveCondition.Description}
                                }
                            }
                        );
                        vbox.AddChild(hbox);
                    }
                    ObjectivesContainer.AddChild(vbox);
                }
            }
        }
    }
}
