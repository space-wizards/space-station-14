using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Power;
using Content.Server.GameObjects.Components.Weapon.Melee;
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components.Power;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.GameObjects.EntitySystems.Weapon.Melee
{
    public class StunbatonSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<StunbatonComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<StunbatonComponent, MeleeHitEvent>(OnMeleeHit);
            SubscribeLocalEvent<StunbatonComponent, UseInHandEvent>(OnUseInHand);
            SubscribeLocalEvent<StunbatonComponent, ThrowCollideEvent>(OnThrowCollide);
            SubscribeLocalEvent<StunbatonComponent, PowerCellChangedEvent>(OnPowerCellChanged);
            SubscribeLocalEvent<StunbatonComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<StunbatonComponent, ExaminedEvent>(OnExamined);
        }

        private void OnAfterInteract(EntityUid uid, StunbatonComponent comp, AfterInteractEvent args)
        {
            if (!comp.Activated || args.Target == null || comp.Cell == null)
                return;

            if (!comp.Cell.TryUseCharge(comp.EnergyPerUse))
                return;

            StunEntity(args.Target, comp);
        }

        private void OnMeleeHit(EntityUid uid, StunbatonComponent comp, MeleeHitEvent args)
        {
            if (!comp.Activated || args.HitEntities.Count == 0 || comp.Cell == null)
                return;

            if (!comp.Cell.TryUseCharge(comp.EnergyPerUse))
                return;

            foreach (IEntity entity in args.HitEntities)
            {
                StunEntity(entity, comp);
            }
        }

        private void OnUseInHand(EntityUid uid, StunbatonComponent comp, UseInHandEvent args)
        {
            if (!ActionBlockerSystem.CanUse(args.User)) return;
            if (comp.Activated)
            {
                TurnOff(comp);
            }
            else
            {
                TurnOn(comp, args.User);
            }
        }

        private void OnThrowCollide(EntityUid uid, StunbatonComponent comp, ThrowCollideEvent args)
        {
            if (!comp.Activated || comp.Cell == null || !comp.Cell.TryUseCharge(comp.EnergyPerUse) || !args.Target.TryGetComponent(out StunnableComponent? stunnable))
                return;

            StunEntity(args.Target, comp);
        }

        private void OnPowerCellChanged(EntityUid uid, StunbatonComponent comp, PowerCellChangedEvent args)
        {
            if (args.Ejected)
            {
                TurnOff(comp);
            }
        }

        private void OnInteractUsing(EntityUid uid, StunbatonComponent comp, InteractUsingEvent args)
        {
            if (!ActionBlockerSystem.CanInteract(args.User)) return;
            if (!comp.CellSlot.InsertCell(args.Used)) return;
        }

        private void OnExamined(EntityUid uid, StunbatonComponent comp, ExaminedEvent args)
        {
            args.Message.AddText("\n");
            var msg = comp.Activated
                ? Loc.GetString("comp-stunbaton-examined-on")
                : Loc.GetString("comp-stun-baton-examined-off");
            args.Message.AddMarkup(msg);
        }

        private void StunEntity(IEntity entity, StunbatonComponent comp)
        {
            if (!entity.TryGetComponent(out StunnableComponent? stunnable) || comp.Cell == null || !comp.Activated) return;

            SoundSystem.Play(Filter.Pvs(comp.Owner), "/Audio/Weapons/egloves.ogg", comp.Owner.Transform.Coordinates, AudioHelpers.WithVariation(0.25f));
            if(!stunnable.SlowedDown)
            {
                if(_robustRandom.Prob(comp.ParalyzeChanceNoSlowdown))
                    stunnable.Paralyze(comp.ParalyzeTime);
                else
                    stunnable.Slowdown(comp.SlowdownTime);
            }
            else
            {
                if(_robustRandom.Prob(comp.ParalyzeChanceWithSlowdown))
                    stunnable.Paralyze(comp.ParalyzeTime);
                else
                    stunnable.Slowdown(comp.SlowdownTime);
            }


            if (!(comp.Cell.CurrentCharge < comp.EnergyPerUse)) return;

            SoundSystem.Play(Filter.Pvs(comp.Owner), AudioHelpers.GetRandomFileFromSoundCollection("sparks"), comp.Owner.Transform.Coordinates, AudioHelpers.WithVariation(0.25f));
            TurnOff(comp);
        }

        private void TurnOff(StunbatonComponent comp)
        {
            if (!comp.Activated)
            {
                return;
            }

            var sprite = comp.Owner.GetComponent<SpriteComponent>();
            var item = comp.Owner.GetComponent<ItemComponent>();

            SoundSystem.Play(Filter.Pvs(comp.Owner), AudioHelpers.GetRandomFileFromSoundCollection("sparks"), comp.Owner.Transform.Coordinates, AudioHelpers.WithVariation(0.25f));

            item.EquippedPrefix = "off";
            // TODO stunbaton visualizer
            sprite.LayerSetState(0, "stunbaton_off");
            comp.Activated = false;
        }

        private void TurnOn(StunbatonComponent comp, IEntity user)
        {
            if (comp.Activated)
            {
                return;
            }

            var sprite = comp.Owner.GetComponent<SpriteComponent>();
            var item = comp.Owner.GetComponent<ItemComponent>();

            var playerFilter = Filter.Pvs(comp.Owner);
            if (comp.Cell == null)
            {
                SoundSystem.Play(playerFilter, "/Audio/Machines/button.ogg", comp.Owner.Transform.Coordinates, AudioHelpers.WithVariation(0.25f));
                user.PopupMessage(Loc.GetString("Cell missing..."));
                return;
            }

            if (comp.Cell.CurrentCharge < comp.EnergyPerUse)
            {
                SoundSystem.Play(playerFilter, "/Audio/Machines/button.ogg", comp.Owner.Transform.Coordinates, AudioHelpers.WithVariation(0.25f));
                user.PopupMessage(Loc.GetString("Dead cell..."));
                return;
            }

            SoundSystem.Play(playerFilter, AudioHelpers.GetRandomFileFromSoundCollection("sparks"), comp.Owner.Transform.Coordinates, AudioHelpers.WithVariation(0.25f));

            item.EquippedPrefix = "on";
            sprite.LayerSetState(0, "stunbaton_on");
            comp.Activated = true;
        }
    }
}
