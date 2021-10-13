using Content.Shared.Body.Components;
using Content.Shared.CharacterAppearance;
using Content.Shared.CharacterAppearance.Components;
using Content.Shared.CharacterAppearance.Systems;
using Content.Shared.Preferences;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Player;

namespace Content.Server.CharacterAppearance.Systems
{
    public sealed class HumanoidAppearanceSystem : SharedHumanoidAppearanceSystem
    {
        public override void Initialize()
        {
            SubscribeLocalEvent<HumanoidAppearanceProfileChangedEvent>(ProfileUpdate);
            SubscribeNetworkEvent<HumanoidAppearanceComponentInitEvent>(OnHumanoidAppearanceInit);
        }

        public override void UpdateFromProfile(EntityUid uid, ICharacterProfile profile)
        {
            if (!EntityManager.GetEntity(uid).TryGetComponent(out HumanoidAppearanceComponent? component))
                return;

            var humanoid = (HumanoidCharacterProfile) profile;
            var profileChangeEvent = new HumanoidAppearanceProfileChangedEvent(uid, humanoid);
            RaiseLocalEvent(profileChangeEvent);
            RaiseNetworkEvent(profileChangeEvent);
        }

        public void UpdateAppearance(EntityUid uid)
        {
            if (!EntityManager.TryGetEntity(uid, out var entity)) return;
            if (!EntityManager.GetEntity(entity.Uid).TryGetComponent(out HumanoidAppearanceComponent? component))
                return;
            var profileChangeEvent = new HumanoidAppearanceProfileChangedEvent(uid, component.Appearance, component.Sex, component.Gender);

            RaiseLocalEvent(profileChangeEvent);
            RaiseNetworkEvent(profileChangeEvent);
        }

        private void OnHumanoidAppearanceInit(HumanoidAppearanceComponentInitEvent args, EntitySessionEventArgs user)
        {
            // the server tries to get the entity from its own manager
            if (!EntityManager.TryGetEntity(args.Uid, out var entity)) return;
            if (!EntityManager.GetEntity(entity.Uid).TryGetComponent(out HumanoidAppearanceComponent? component))
                return;

            Logger.DebugS("HAS", $"Component init detected, updating client {user.SenderSession.Name} now. UID: {args.Uid}");
            // then sends a network event saying that this humanoid's profile has changed
            // TODO: if this works, clean up HEAVILY, holy Fuck
            RaiseNetworkEvent(new HumanoidAppearanceProfileChangedEvent(args.Uid, component.Appearance, component.Sex, component.Gender), Filter.SinglePlayer(user.SenderSession));
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
