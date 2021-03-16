using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.Preferences;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Mobs
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

                if (Owner.TryGetComponent(out IBody? body))
                {
                    foreach (var part in body.Parts.Values)
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

            if (Appearance != null! && Owner.TryGetComponent(out IBody? body))
            {
                foreach (var part in body.Parts.Values)
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
