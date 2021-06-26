using Content.Shared.Body.Components;
using Content.Shared.CharacterAppearance;
using Content.Shared.CharacterAppearance.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Server.CharacterAppearance.Components
{
    [RegisterComponent]
    public sealed class HumanoidAppearanceComponent : SharedHumanoidAppearanceComponent
    {
        public override HumanoidCharacterAppearance Appearance
        {
            get => base.Appearance;
            set
            {
                base.Appearance = value;

                if (Owner.TryGetComponent(out SharedBodyComponent? body))
                {
                    foreach (var (part, _) in body.Parts)
                    {
                        if (!part.Owner.TryGetComponent(out SpriteComponent? sprite))
                        {
                            continue;
                        }

                        sprite.Color = value.SkinColor;
                    }
                }
            }
        }

        protected override void Startup()
        {
            base.Startup();

            if (Appearance != null! && Owner.TryGetComponent(out SharedBodyComponent? body))
            {
                foreach (var (part, _) in body.Parts)
                {
                    if (!part.Owner.TryGetComponent(out SpriteComponent? sprite))
                    {
                        continue;
                    }

                    sprite.Color = Appearance.SkinColor;
                }
            }
        }
    }
}
