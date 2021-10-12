using Content.Shared.Body.Components;
using Content.Shared.CharacterAppearance;
using Content.Shared.CharacterAppearance.Components;
using Content.Shared.CharacterAppearance.Systems;
using Content.Shared.Preferences;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;

namespace Content.Server.CharacterAppearance.Systems
{
    public sealed class HumanoidAppearanceSystem : SharedHumanoidAppearanceSystem
    {
        public override void Initialize()
        {
            SubscribeLocalEvent<HumanoidAppearanceProfileChangedEvent>(ProfileUpdate);
            SubscribeNetworkEvent<HumanoidAppearanceComponentInitEvent>(OnHumanoidAppearanceInit);
        }

        private void OnHumanoidAppearanceInit(HumanoidAppearanceComponentInitEvent args)
        {
            // the server tries to get the entity from its own manager
            if (!EntityManager.GetEntity(args.Uid).TryGetComponent(out HumanoidAppearanceComponent? component))
                return;

            // then sends a network event saying that this humanoid's profile has changed
            // TODO: if this works, clean up HEAVILY, holy Fuck
            RaiseNetworkEvent(new HumanoidAppearanceProfileChangedEvent(args.Uid, component.Appearance, component.Sex, component.Gender));
        }

        private void ProfileUpdate(HumanoidAppearanceProfileChangedEvent args)
        {
            if (!EntityManager.GetEntity(args.Uid).TryGetComponent(out HumanoidAppearanceComponent? component))
                return;

            Logger.DebugS("HAS", "HUMANOIDAPPEARANCE SERVER UPDATE");
            component.Appearance = args.Appearance;
            Logger.DebugS("HAS", $"{component.Appearance}, {args.Appearance}");
            component.Sex = args.Sex;
            Logger.DebugS("HAS", $"{component.Sex}, {args.Sex}");
            component.Gender = args.Gender;
            Logger.DebugS("HAS", $"{component.Gender}, {args.Gender}");
            UpdateSkinColor(args.Uid, component);
        }

        public void UpdateSkinColor(EntityUid uid, HumanoidAppearanceComponent component)
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
