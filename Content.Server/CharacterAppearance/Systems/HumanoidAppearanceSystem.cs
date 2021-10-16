using Content.Shared.Body.Components;
using Content.Shared.CharacterAppearance.Components;
using Content.Shared.CharacterAppearance.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Server.CharacterAppearance.Systems
{
    public sealed class HumanoidAppearanceSystem : SharedHumanoidAppearanceSystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HumanoidAppearanceComponent, ChangedHumanoidAppearanceEvent>(UpdateSkinColor);
        }

        private void UpdateSkinColor(EntityUid uid, HumanoidAppearanceComponent component, ChangedHumanoidAppearanceEvent _)
        {
            if (EntityManager.TryGetComponent<SharedBodyComponent>(uid, out SharedBodyComponent?  body))
            {
                foreach (var (part, _) in body.Parts)
                {
                    if (part.Owner.TryGetComponent(out SpriteComponent? sprite))
                    {
                        sprite!.Color = component.Appearance.SkinColor;
                    }
                }
            }
        }
    }
}
