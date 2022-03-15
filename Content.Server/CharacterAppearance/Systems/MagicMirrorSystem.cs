using Content.Server.CharacterAppearance.Components;
using Content.Server.UserInterface;
using Content.Shared.CharacterAppearance.Components;
using Robust.Server.GameObjects;

namespace Content.Server.CharacterAppearance.Systems
{
    public sealed class MagicMirrorSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MagicMirrorComponent, ActivatableUIOpenAttemptEvent>(OnOpenUIAttempt);
            SubscribeLocalEvent<MagicMirrorComponent, AfterActivatableUIOpenEvent>(AfterUIOpen);
        }

        private void OnOpenUIAttempt(EntityUid uid, MagicMirrorComponent mirror, ActivatableUIOpenAttemptEvent args)
        {
            if (!HasComp<HumanoidAppearanceComponent>(args.User))
                args.Cancel();
        }
        private void AfterUIOpen(EntityUid uid, MagicMirrorComponent component, AfterActivatableUIOpenEvent args)
        {
            var looks = Comp<HumanoidAppearanceComponent>(args.User);
            var actor = Comp<ActorComponent>(args.User);
            var appearance = looks.Appearance;

            var msg = new MagicMirrorComponent.MagicMirrorInitialDataMessage(
                appearance.HairColor,
                appearance.FacialHairColor,
                appearance.HairStyleId,
                appearance.FacialHairStyleId,
                appearance.EyeColor,
                looks.CategoriesHair,
                looks.CategoriesFacialHair,
                looks.CanColorHair,
                looks.CanColorFacialHair);

            component.UserInterface?.SendMessage(msg, actor.PlayerSession);
        }
    }
}
