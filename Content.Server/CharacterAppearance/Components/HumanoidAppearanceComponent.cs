using Content.Shared.Body.Components;
using Content.Shared.CharacterAppearance;
using Content.Shared.CharacterAppearance.Components;
using Content.Shared.CharacterAppearance.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Server.CharacterAppearance.Components
{
    public sealed class HumanoidAppearanceSystem : SharedHumanoidAppearanceSystem
    {
        public override void Initialize()
        {
            SubscribeNetworkEvent<HumanoidAppearanceProfileChangedEvent>(UpdateSkinColor);
        }

        public void UpdateSkinColor(HumanoidAppearanceProfileChangedEvent args)
        {
            if (!EntityManager.TryGetEntity(args.Uid, out var owner)) return;
            var profile = args.Profile;

            if (owner.TryGetComponent(out SharedBodyComponent? body))
            {
                foreach (var (part, _) in body.Parts)
                {
                    if (part.Owner.TryGetComponent(out SpriteComponent? sprite))
                    {
                        sprite!.Color = profile.Appearance.SkinColor;
                    }

                }
            }
        }
    }
}
