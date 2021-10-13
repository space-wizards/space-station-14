using Content.Shared.Body.Components;
using Content.Shared.CharacterAppearance.Components;
using Content.Shared.CharacterAppearance.Systems;
using Content.Shared.Preferences;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.CharacterAppearance.Systems
{
    public sealed class HumanoidAppearanceSystem : SharedHumanoidAppearanceSystem
    {
        public override void Initialize()
        {
            SubscribeLocalEvent<ChangedHumanoidAppearanceEvent>(OnAppearanceChange);
            SubscribeNetworkEvent<HumanoidAppearanceComponentInitEvent>(OnHumanoidAppearanceInit);
        }

        public override void UpdateFromProfile(EntityUid uid, ICharacterProfile profile)
        {
            if (!EntityManager.TryGetEntity(uid, out var entity)) return;
            if (!entity.HasComponent<HumanoidAppearanceComponent>()) return;

            var humanoid = (HumanoidCharacterProfile) profile;
            var appearanceChangeEvent = new ChangedHumanoidAppearanceEvent(uid, humanoid);
            RaiseLocalEvent(appearanceChangeEvent);
            RaiseNetworkEvent(appearanceChangeEvent);
        }

        public void UpdateAppearance(EntityUid uid)
        {
            if (!EntityManager.TryGetEntity(uid, out var entity)) return;
            if (!entity.TryGetComponent(out HumanoidAppearanceComponent? component))
                return;

            var appearanceChangeEvent = new ChangedHumanoidAppearanceEvent(uid, component.Appearance, component.Sex, component.Gender);

            RaiseLocalEvent(appearanceChangeEvent);
            RaiseNetworkEvent(appearanceChangeEvent);
        }

        private void OnHumanoidAppearanceInit(HumanoidAppearanceComponentInitEvent args, EntitySessionEventArgs user)
        {
            if (!EntityManager.TryGetEntity(args.Uid, out var entity)) return;
            if (!entity.TryGetComponent(out HumanoidAppearanceComponent? component))
                return;

            RaiseNetworkEvent(new ChangedHumanoidAppearanceEvent(args.Uid, component.Appearance, component.Sex, component.Gender), Filter.SinglePlayer(user.SenderSession));
        }

        public override void OnAppearanceChange(ChangedHumanoidAppearanceEvent args)
        {
            if (!EntityManager.TryGetEntity(args.Uid, out var entity)) return;
            if (!entity.TryGetComponent(out HumanoidAppearanceComponent? component))
                return;

            component.Appearance = args.Appearance;
            component.Sex = args.Sex;
            component.Gender = args.Gender;
            UpdateSkinColor(args.Uid, component);
        }

        private void UpdateSkinColor(EntityUid uid, HumanoidAppearanceComponent component)
        {
            if (!EntityManager.TryGetEntity(uid, out var owner)) return;

            if (owner.TryGetComponent(out SharedBodyComponent? body))
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
