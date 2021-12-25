using System.Linq;
using Content.Server.Items;
using Content.Shared.Audio;
using Content.Shared.Interaction;
using Content.Shared.ActionBlocker;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Log;

namespace Content.Server.Weapon.Melee.Esword
{
    internal class EswordSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<EswordComponent, MeleeHitEvent>(OnMeleeHit);
            SubscribeLocalEvent<EswordComponent, UseInHandEvent>(OnUseInHand);

        }

        private void OnMeleeHit(EntityUid uid, EswordComponent comp, MeleeHitEvent args)
        {
            if (!comp.Activated || !args.HitEntities.Any())
                return;
            
            SoundSystem.Play(Filter.Pvs(comp.Owner), comp.HitSound.GetSound(), comp.Owner, AudioHelpers.WithVariation(0.25f));
        }

        private void OnUseInHand(EntityUid uid, EswordComponent comp, UseInHandEvent args)
        {
            if (!Get<ActionBlockerSystem>().CanUse(args.User))
                return;

            if (comp.Activated)
            {
                TurnOff(comp);
            }
            else
            {
                TurnOn(comp, args.User);
            }
        }

        private void TurnOff(EswordComponent comp)
        {
            if (!comp.Activated)
            {
                return;
            }

            if (!EntityManager.TryGetComponent<SpriteComponent?>(comp.Owner, out var sprite) ||
                !EntityManager.TryGetComponent<ItemComponent?>(comp.Owner, out var item)) return;

            SoundSystem.Play(Filter.Pvs(comp.Owner), comp.DeActivateSound.GetSound(), comp.Owner);

            item.EquippedPrefix = "off";
            sprite.LayerSetState(0, "e_sword");
            comp.Activated = false;
        }

        private void TurnOn(EswordComponent comp, EntityUid user)
        {
            if (comp.Activated)
            {
                return;
            }

            if (!EntityManager.TryGetComponent<SpriteComponent?>(comp.Owner, out var sprite) ||
                !EntityManager.TryGetComponent<ItemComponent?>(comp.Owner, out var item))
                return;

            var playerFilter = Filter.Pvs(comp.Owner);

            SoundSystem.Play(playerFilter, comp.ActivateSound.GetSound(), comp.Owner);

            item.EquippedPrefix = "on";
            sprite.LayerSetState(0, "e_sword_on");
            comp.Activated = true;
        }
    }
}
