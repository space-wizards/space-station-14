using Content.Server.Chainsaw.Components;
using Content.Shared.Audio;
using Content.Shared.Damage;
using Content.Shared.Damage.Events;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Toggleable;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.Chainsaw.Systems
{
    public sealed class ChainsawSystem : EntitySystem
    {
        [Dependency] private readonly SharedItemSystem _item = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ChainsawComponent, UseInHandEvent>(OnUseInHand);
            SubscribeLocalEvent<ChainsawComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<ChainsawComponent, GetMeleeDamageEvent>(OnGetMeleeDamage);
        }

        private void OnUseInHand(EntityUid uid, ChainsawComponent comp, UseInHandEvent args)
        {
            if (comp.Activated)
            {
                TurnOff(uid, comp);
            }
            else
            {
                TurnOn(uid, comp, args.User);
            }
        }

        private void OnExamined(EntityUid uid, ChainsawComponent comp, ExaminedEvent args)
        {
            var msg = comp.Activated
                ? Loc.GetString("comp-chainsaw-examined-on")
                : Loc.GetString("comp-chainsaw-examined-off");
            args.PushMarkup(msg);
        }

        private void OnGetMeleeDamage(EntityUid uid, ChainsawComponent comp, ref GetMeleeDamageEvent args)
        {
            if (!comp.Activated)
                return;

            args.Damage = comp.OnDamage;
        }

        private void TurnOff(EntityUid uid, ChainsawComponent comp)
        {
            if (!comp.Activated)
                return;

            if (TryComp<AppearanceComponent>(comp.Owner, out var appearance) &&
                TryComp<ItemComponent>(comp.Owner, out var item))
            {
                _item.SetHeldPrefix(comp.Owner, "off", item);
                _item.SetSize(uid, 90, item);
                _appearance.SetData(uid, ToggleVisuals.Toggled, false, appearance);
            }

            comp.Activated = false;
        }

        private void TurnOn(EntityUid uid, ChainsawComponent comp, EntityUid user)
        {

            if (comp.Activated)
                return;

            var playerFilter = Filter.Pvs(comp.Owner, entityManager: EntityManager);


            if (EntityManager.TryGetComponent<AppearanceComponent>(comp.Owner, out var appearance) &&
                EntityManager.TryGetComponent<ItemComponent>(comp.Owner, out var item))
            {
                _item.SetHeldPrefix(comp.Owner, "on", item);
                _item.SetSize(uid, 9999, item);
                _appearance.SetData(uid, ToggleVisuals.Toggled, true, appearance);
            }

            SoundSystem.Play(comp.ActiveSound.GetSound(), playerFilter, comp.Owner, AudioHelpers.WithVariation(0.5f));
            comp.Activated = true;
        }
    }
}