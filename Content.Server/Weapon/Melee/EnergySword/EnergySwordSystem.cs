using System.Collections.Generic;
using Content.Shared.Item;
using Content.Server.Tools.Components;
using Content.Shared.Interaction;
using Content.Shared.ActionBlocker;
using Content.Shared.FixedPoint;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using System;

namespace Content.Server.Weapon.Melee.Esword
{
    internal class EswordSystem : EntitySystem
    {
        [Dependency] private readonly ActionBlockerSystem _blockerSystem = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<EswordComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<EswordComponent, MeleeHitEvent>(OnMeleeHit);
            SubscribeLocalEvent<EswordComponent, UseInHandEvent>(OnUseInHand);
            SubscribeLocalEvent<EswordComponent, InteractUsingEvent>(OnInteractUsing);
        }

        private void OnComponentInit(EntityUid uid, EswordComponent comp, ComponentInit args)
        {
            string[] possibleColors = { "Tomato", "DodgerBlue", "Aqua", "MediumSpringGreen", "MediumOrchid" };
            Random random = new Random();
            comp.BladeColor = Color.FromName(possibleColors[random.Next(5)]);
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
            if (!_blockerSystem.CanUse(args.User))
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
                return;

            if (!TryComp(comp.Owner, out SpriteComponent? sprite)
                || !TryComp(comp.Owner, out SharedItemComponent? item))
                return;

            item.Size = 5;

            SoundSystem.Play(Filter.Pvs(comp.Owner), comp.DeActivateSound.GetSound(), comp.Owner);

            item.EquippedPrefix = "off";
            sprite.LayerSetVisible(1, false);

            comp.Activated = false;
        }

        private void TurnOn(EswordComponent comp, EntityUid user)
        {
            if (comp.Activated)
                return;

            if (!TryComp(comp.Owner, out SpriteComponent? sprite)
                || !TryComp(comp.Owner, out SharedItemComponent? item))
                return;

            item.Size = 9999;

            SoundSystem.Play(Filter.Pvs(comp.Owner), comp.ActivateSound.GetSound(), comp.Owner);

            if (comp.Hacked == true)
            {
                item.EquippedPrefix = "on-rainbow";
                sprite.LayerSetState(0, "e_sword_rainbow_on");
            }
            else
            {
                item.EquippedPrefix = "on";
                item.Color = comp.BladeColor;

                sprite.LayerSetColor(1, comp.BladeColor);
                sprite.LayerSetVisible(1, true);
            }
            
            comp.Activated = true;
        }

        private void OnInteractUsing(EntityUid uid, EswordComponent comp, InteractUsingEvent args)
        {
            if (!_blockerSystem.CanInteract(args.User) || comp.Hacked == true)
                return;

            if (TryComp(args.Used, out ToolComponent? tool))
            {
                if (tool.Qualities.ContainsAny("Pulsing"))
                {
                    comp.Hacked = true;

                    if (comp.Activated == true && TryComp(comp.Owner, out SpriteComponent? sprite)
                        && TryComp(comp.Owner, out SharedItemComponent? item))
                    {
                        sprite.LayerSetState(0, "e_sword_rainbow_on");
                        item.EquippedPrefix = "on-rainbow";
                    }
                }
            }
        }
    }
}
