using System.Collections.Generic;
using Content.Server.Items;
using Content.Shared.Interaction;
using Content.Shared.ActionBlocker;
using Content.Shared.FixedPoint;
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
            
            if (comp.Activated == true)
            {
                args.BonusDamage.DamageDict = new Dictionary<string, FixedPoint2>()
                {
                    {"Slash", FixedPoint2.New(25)},
                };
                args.HitSoundOverride = comp.HitSound;
            }
            
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
                !EntityManager.TryGetComponent<ItemComponent?>(comp.Owner, out var item))
                return;

            if (EntityManager.TryGetComponent<ItemComponent?>(comp.Owner, out var esword))
                esword.Size = 5;

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

            if (EntityManager.TryGetComponent<ItemComponent?>(comp.Owner, out var esword))
                esword.Size = 9999;

            SoundSystem.Play(Filter.Pvs(comp.Owner), comp.ActivateSound.GetSound(), comp.Owner);

            item.EquippedPrefix = "on";
            sprite.LayerSetState(0, "e_sword_on");

            comp.Activated = true;
        }
    }
}
