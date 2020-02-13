using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Server.GameObjects;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects
{
    [RegisterComponent]
    public class SpeciesComponent : SharedSpeciesComponent, IActionBlocker, IOnDamageBehavior, IExAct
    {
        /// <summary>
        /// Damagestates are reached by reaching a certain damage threshold, they will block actions after being reached
        /// </summary>
        public IDamageState CurrentDamageState { get; private set; } = new NormalState();

        /// <summary>
        /// Damage state enum for current health, set only via change damage state //TODO: SETTER
        /// </summary>
        private ThresholdType currentstate = ThresholdType.None;

        /// <summary>
        /// Holds the damage template which controls the threshold and resistance settings for this species type
        /// </summary>
        public DamageTemplates DamageTemplate { get; private set; }

        /// <summary>
        /// Variable for serialization
        /// </summary>
        private string templatename;

        private int _heatResistance;
        public int HeatResistance => _heatResistance;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref templatename, "Template", "Human");

            var type = typeof(SpeciesComponent).Assembly.GetType("Content.Server.GameObjects." + templatename);
            DamageTemplate = (DamageTemplates) Activator.CreateInstance(type);
            serializer.DataFieldCached(ref _heatResistance, "HeatResistance", 323);
        }

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null,
            IComponent component = null)
        {
            switch (message)
            {
                case PlayerAttachedMsg _:
                    if (CanReceiveStatusEffect(Owner)) {
                        DamageableComponent damage = Owner.GetComponent<DamageableComponent>();
                        DamageTemplate.ChangeHudState(damage);
                    }
                    break;
                case PlayerDetachedMsg _:
                    break;
            }
        }

        public override void OnRemove()
        {
            base.OnRemove();
            Owner.TryGetComponent(out ServerStatusEffectsComponent statusEffectsComponent);
            statusEffectsComponent?.RemoveStatus(StatusEffect.Health);

            Owner.TryGetComponent(out ServerOverlayEffectsComponent overlayEffectsComponent);
            overlayEffectsComponent?.ChangeOverlay(ScreenEffects.None);
        }

        bool IActionBlocker.CanMove()
        {
            return CurrentDamageState.CanMove();
        }

        bool IActionBlocker.CanInteract()
        {
            return CurrentDamageState.CanInteract();
        }

        bool IActionBlocker.CanUse()
        {
            return CurrentDamageState.CanUse();
        }

        bool IActionBlocker.CanThrow()
        {
            return CurrentDamageState.CanThrow();
        }

        bool IActionBlocker.CanSpeak()
        {
            return CurrentDamageState.CanSpeak();
		}

        bool IActionBlocker.CanDrop()
        {
            return CurrentDamageState.CanDrop();
        }

        bool IActionBlocker.CanEmote()
        {
            return CurrentDamageState.CanEmote();
        }

        List<DamageThreshold> IOnDamageBehavior.GetAllDamageThresholds()
        {
            var thresholdlist = DamageTemplate.DamageThresholds;
            thresholdlist.AddRange(DamageTemplate.HealthHudThresholds);
            return thresholdlist;
        }

        void IOnDamageBehavior.OnDamageThresholdPassed(object damageable, DamageThresholdPassedEventArgs e)
        {
            DamageableComponent damage = (DamageableComponent) damageable;

            if (e.DamageThreshold.ThresholdType != ThresholdType.HUDUpdate)
            {
                ChangeDamageState(DamageTemplate.CalculateDamageState(damage));
            }

            //specifies if we have a client to update the hud for
            if (CanReceiveStatusEffect(Owner))
            {
                DamageTemplate.ChangeHudState(damage);
            }
        }

        private bool CanReceiveStatusEffect(IEntity user)
        {
            if (!user.HasComponent<ServerStatusEffectsComponent>() &&
                !user.HasComponent<ServerOverlayEffectsComponent>())
            {
                return false;
            }
            if (user.HasComponent<DamageableComponent>())
            {
                return true;
            }

            return false;
        }

        private void ChangeDamageState(ThresholdType threshold)
        {
            if (threshold == currentstate)
            {
                return;
            }

            CurrentDamageState.ExitState(Owner);
            CurrentDamageState = DamageTemplates.StateThresholdMap[threshold];
            CurrentDamageState.EnterState(Owner);

            currentstate = threshold;

            Owner.RaiseEvent(new MobDamageStateChangedMessage(this));
        }

        void IExAct.OnExplosion(ExplosionEventArgs eventArgs)
        {
            var burnDamage = 0;
            var bruteDamage = 0;
            switch(eventArgs.Severity)
            {
                case ExplosionSeverity.Destruction:
                    bruteDamage += 250;
                    burnDamage += 250;
                    break;
                case ExplosionSeverity.Heavy:
                    bruteDamage += 60;
                    burnDamage += 60;
                    break;
                case ExplosionSeverity.Light:
                    bruteDamage += 30;
                    break;
            }
            Owner.GetComponent<DamageableComponent>().TakeDamage(DamageType.Brute, bruteDamage, eventArgs.Source);
            Owner.GetComponent<DamageableComponent>().TakeDamage(DamageType.Heat, burnDamage, eventArgs.Source);
        }
    }

    /// <summary>
    ///     Fired when <see cref="SpeciesComponent.CurrentDamageState"/> changes.
    /// </summary>
    public sealed class MobDamageStateChangedMessage : EntitySystemMessage
    {
        public MobDamageStateChangedMessage(SpeciesComponent species)
        {
            Species = species;
        }

        /// <summary>
        ///     The species component that was changed.
        /// </summary>
        public SpeciesComponent Species { get; }
    }
}
