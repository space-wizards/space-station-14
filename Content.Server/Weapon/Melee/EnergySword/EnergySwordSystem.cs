using Content.Shared.Item;
using Content.Server.Tools.Components;
using Content.Shared.Interaction;
using Content.Shared.ActionBlocker;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Random;
using Robust.Shared.Log;

namespace Content.Server.Weapon.Melee.Esword
{
    internal class EnergySwordSystem : EntitySystem
    {
        [Dependency] private readonly ActionBlockerSystem _blockerSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<EnergySwordComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<EnergySwordComponent, MeleeHitEvent>(OnMeleeHit);
            SubscribeLocalEvent<EnergySwordComponent, UseInHandEvent>(OnUseInHand);
            SubscribeLocalEvent<EnergySwordComponent, InteractUsingEvent>(OnInteractUsing);
        }

        private void OnComponentInit(EntityUid uid, EnergySwordComponent comp, ComponentInit args)
        {
            if (comp.ColorOptions.Count != 0)
                comp.BladeColor = _random.Pick(comp.ColorOptions);
        }

        private void OnMeleeHit(EntityUid uid, EnergySwordComponent comp, MeleeHitEvent args)
        {
            
            if (comp.Activated == true)
            {
                // Overrides basic blunt damage with burn+slash as set in yaml
                args.BonusDamage = comp.LitDamageBonus;
                args.HitSoundOverride = comp.HitSound;
            }
        }

        private void OnUseInHand(EntityUid uid, EnergySwordComponent comp, UseInHandEvent args)
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

        private void TurnOff(EnergySwordComponent comp)
        {
            if (!comp.Activated)
                return;

            if (!TryComp(comp.Owner, out SpriteComponent? sprite)
                || !TryComp(comp.Owner, out SharedItemComponent? item)
                || !TryComp(comp.Owner, out SharedPointLightComponent? light))
                return;

            item.Size = 5;

            SoundSystem.Play(Filter.Pvs(comp.Owner), comp.DeActivateSound.GetSound(), comp.Owner);

            item.EquippedPrefix = "off";

            if (comp.Hacked == true)
            {
                sprite.LayerSetState(0, "e_sword");
            }
            else
            {
                sprite.LayerSetVisible(1, false);
            }

            light.Enabled = false;
            comp.Activated = false;
        }

        private void TurnOn(EnergySwordComponent comp, EntityUid user)
        {
            if (comp.Activated)
                return;

            if (!TryComp(comp.Owner, out SpriteComponent? sprite)
                || !TryComp(comp.Owner, out SharedItemComponent? item)
                || !TryComp(comp.Owner, out SharedPointLightComponent? light))
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

                light.Color = comp.BladeColor;
                light.Enabled = true;
            }
            
            comp.Activated = true;
        }

        private void OnInteractUsing(EntityUid uid, EnergySwordComponent comp, InteractUsingEvent args)
        {
            if (comp.Hacked == true || !_blockerSystem.CanInteract(args.User))
                return;

            if (TryComp(args.Used, out ToolComponent? tool) && tool.Qualities.ContainsAny("Pulsing"))
            {
                comp.Hacked = true;

                if (comp.Activated == true && TryComp(comp.Owner, out SpriteComponent? sprite)
                    && TryComp(comp.Owner, out SharedItemComponent? item))
                {
                    sprite.LayerSetVisible(1, false);
                    sprite.LayerSetColor(1, Color.White);
                    //todo: figure out how to use the RGBLightControllerSystem to phase out the rainbow sprite AND add lights.
                    sprite.LayerSetState(0, "e_sword_rainbow_on");

                    item.Color = Color.White;
                    item.EquippedPrefix = "on-rainbow";
                }
            }
        }
    }
}
