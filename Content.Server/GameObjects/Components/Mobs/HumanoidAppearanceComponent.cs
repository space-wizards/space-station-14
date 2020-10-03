using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Body.Part;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.Preferences;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Mobs
{
    [RegisterComponent]
    public sealed class HumanoidAppearanceComponent : SharedHumanoidAppearanceComponent, IBodyPartAdded
    {
        public override HumanoidCharacterAppearance Appearance
        {
            get => base.Appearance;
            set
            {
                base.Appearance = value;

                if (Owner.TryGetBody(out var body))
                {
                    foreach (var part in body.Parts.Values)
                    {
                        if (!part.Owner.TryGetComponent(out SpriteComponent sprite))
                        {
                            continue;
                        }

                        sprite.Color = value.SkinColor;
                    }
                }
            }
        }

        public void BodyPartAdded(BodyPartAddedEventArgs args)
        {
            if (Appearance != null &&
                args.Part.Owner.TryGetComponent(out SpriteComponent sprite))
            {
                sprite.Color = Appearance.SkinColor;
            }
        }
    }
}
