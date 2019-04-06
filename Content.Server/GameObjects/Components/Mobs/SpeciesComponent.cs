using System;
using System.Collections.Generic;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Shared.GameObjects.Components.Mobs;
using SS14.Server.GameObjects;
using SS14.Shared.ContentPack;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Serialization;

namespace Content.Server.GameObjects
{
    public class SpeciesComponent : SharedSpeciesComponent, IActionBlocker, IOnDamageBehavior
    {
        /// <summary>
        /// Damagestates are reached by reaching a certain damage threshold, they will block actions after being reached
        /// </summary>
        public DamageState CurrentDamageState { get; private set; } = new NormalState();

        /// <summary>
        /// Damage state enum for current health, set only via change damage state //TODO: SETTER
        /// </summary>
        private ThresholdType currentstate = ThresholdType.None;

        /// <summary>
        /// Holds the damage template which controls the threshold and resistance settings for this species type
        /// </summary>
        private DamageTemplates DamageTemplate;

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

            Type type = AppDomain.CurrentDomain.GetAssemblyByName("Content.Server")
                .GetType("Content.Server.GameObjects." + templatename);
            DamageTemplate = (DamageTemplates) Activator.CreateInstance(type);
            serializer.DataFieldCached(ref _heatResistance, "HeatResistance", 323);
        }

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null,
            IComponent component = null)
        {
            switch (message)
            {
                case PlayerAttachedMsg _:
                    var hudstatechange = DamageTemplate.ChangeHudState(Owner.GetComponent<DamageableComponent>());
                    SendNetworkMessage(hudstatechange);
                    break;
            }
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

            if (Owner.TryGetComponent(out BasicActorComponent actor)
            ) //specifies if we have a client to update the hud for
            {
                var hudstatechange = DamageTemplate.ChangeHudState(damage);
                SendNetworkMessage(hudstatechange);
            }
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
        }
    }
}
