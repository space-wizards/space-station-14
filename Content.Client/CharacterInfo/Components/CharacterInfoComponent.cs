using Content.Client.CharacterInterface;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.CharacterInfo.Components
{
    [RegisterComponent]
    public sealed class CharacterInfoComponent : Component, ICharacterUI
    {
        public CharacterInfoControl Control = default!;

        public Control Scene { get; set; } = default!;
        public UIPriority Priority => UIPriority.Info;

        public void Opened()
        {
            IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<CharacterInfoSystem>().RequestCharacterInfo(Owner);
        }

        public sealed class CharacterInfoControl : BoxContainer
        {
            public SpriteView SpriteView { get; }
            public Label NameLabel { get; }
            public Label SubText { get; }

            public BoxContainer ObjectivesContainer { get; }

            public CharacterInfoControl()
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
                                    StyleClasses = {StyleBase.StyleClassLabelSubText},

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

                AddChild(new Placeholder
                {
                    PlaceholderText = Loc.GetString("character-info-roles-antagonist-text")
                });
            }
        }
    }
}
