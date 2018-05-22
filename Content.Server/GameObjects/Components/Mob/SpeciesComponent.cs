using System;
using System.Collections.Generic;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using SS14.Server.GameObjects;
using SS14.Shared.ContentPack;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Serialization;

namespace Content.Server.GameObjects
{
    public class SpeciesComponent : Component, IActionBlocker, IOnDamageBehavior
    {
        public override string Name => "Species";

        /// <summary>
        /// Damagestates are reached by reaching a certain damage threshold, they will block actions after being reached
        /// </summary>
        public DamageState CurrentDamageState { get; private set; } = new NormalState();

        /// <summary>
        /// The threshold that was used to set the current damage state value
        /// </summary>
        private ThresholdTypes currentstate = ThresholdTypes.None;

        /// <summary>
        /// Holds the damage template which controls the threshold and resistance settings for this species type
        /// </summary>
        private DamageTemplates DamageTemplate;

        /// <summary>
        /// Variable for serialization
        /// </summary>
        private string templatename;

        public override void ExposeData(EntitySerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref templatename, "Template", "Human");

            Type type = AppDomain.CurrentDomain.GetAssemblyByName("Content.Server").GetType("Content.Server.GameObjects." + templatename);
            DamageTemplate = (DamageTemplates)Activator.CreateInstance(type);
            currentstate = DamageTemplate.AllowedStates[0]; //Set the current state to the healthiest state
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

        void IOnDamageBehavior.OnDamageThresholdPassed(object obj, DamageThresholdPassedEventArgs e)
        {
            if(e.DamageThreshold.ThresholdType != ThresholdTypes.HUDUpdate)
            {
                ChangeDamageState(e.DamageThreshold.ThresholdType, e.Passed);
            }

            if (Owner.TryGetComponent(out BasicActorComponent actor)) //specifies if we have a client to update the hud for
            {
                var hudstatechange = DamageTemplate.ChangeHudState(e.DamageThreshold.ThresholdType, Owner.GetComponent<DamageableComponent>());
                SendNetworkMessage(hudstatechange);
            }
        }

        private void ChangeDamageState(ThresholdTypes threshold, bool passed)
        {
            //Above threshold, try to increase damagestate to this new more damaged state
            if (passed == true)
            {
                //We want to increase our damage state to the new value, if the new value is a lower priority than the current one return
                if (DamageTemplate.AllowedStates.IndexOf(currentstate) > DamageTemplate.AllowedStates.IndexOf(threshold))
                {
                    return;
                }

                CurrentDamageState.ExitState(Owner);
                CurrentDamageState = DamageTemplates.StateThresholdMap[threshold];
                CurrentDamageState.EnterState(Owner);

                currentstate = threshold;
            }
            else
            //Returned below threshold, try to decrease damagestate to the next highest damage state
            {
                var thresholdindex = DamageTemplate.AllowedStates.IndexOf(threshold);
                var currentindex = DamageTemplate.AllowedStates.IndexOf(currentstate);
                //If we are already at a lower damage state of the threshold we just went below, or if we're already at the healthiest state return
                if (currentindex < thresholdindex || currentindex == 0)
                {
                    return;
                }

                //TODO: store which states we CAN be, so that thresholds do not need to be strictly above each other but instead take the highest
                //of all possible thresholds that we have reached
                currentstate = DamageTemplate.AllowedStates[thresholdindex - 1];

                CurrentDamageState.ExitState(Owner);
                CurrentDamageState = DamageTemplates.StateThresholdMap[currentstate];
                CurrentDamageState.EnterState(Owner);
            }
        }
    }
}
