using Content.Client.GameObjects.Components.Mobs;
using Content.Client.UserInterface;
using Content.Client.UserInterface.Stylesheets;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Client.GameObjects.Components.Actor
{
    [RegisterComponent]
    public sealed class CharacterInfoComponent : Component, ICharacterUI
    {
        [Dependency] private readonly ILocalizationManager _loc = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;

        private CharacterInfoControl _control;

        public override string Name => "CharacterInfo";

        public Control Scene { get; private set; }
        public UIPriority Priority => UIPriority.Info;

        public override void OnAdd()
        {
            base.OnAdd();

            Scene = _control = new CharacterInfoControl(_resourceCache, _loc);
        }

        public override void Initialize()
        {
            base.Initialize();

            if (Owner.TryGetComponent(out ISpriteComponent spriteComponent))
            {
                _control.SpriteView.Sprite = spriteComponent;
            }

            _control.NameLabel.Text = Owner.Name;
            // ReSharper disable once StringLiteralTypo
            _control.SubText.Text = _loc.GetString("Professional Greyshirt");
        }

        private sealed class CharacterInfoControl : VBoxContainer
        {
            public SpriteView SpriteView { get; }
            public Label NameLabel { get; }
            public Label SubText { get; }

            public CharacterInfoControl(IResourceCache resourceCache, ILocalizationManager loc)
            {
                AddChild(new HBoxContainer
                {
                    Children =
                    {
                        (SpriteView = new SpriteView { Scale = (2, 2)}),
                        new VBoxContainer
                        {
                            SizeFlagsVertical = SizeFlags.None,
                            Children =
                            {
                                (NameLabel = new Label()),
                                (SubText = new Label
                                {
                                    SizeFlagsVertical = SizeFlags.None,
                                    StyleClasses = {StyleNano.StyleClassLabelSubText}
                                })
                            }
                        }
                    }
                });

                AddChild(new Placeholder(resourceCache)
                {
                    PlaceholderText = loc.GetString("Health & status effects")
                });

                AddChild(new Placeholder(resourceCache)
                {
                    PlaceholderText = loc.GetString("Objectives")
                });

                AddChild(new Placeholder(resourceCache)
                {
                    PlaceholderText = loc.GetString("Antagonist Roles")
                });
            }
        }
    }
}
