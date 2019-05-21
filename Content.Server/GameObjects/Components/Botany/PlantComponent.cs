using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Shared.Interfaces;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.GameObjects.Components.Botany
{
    class PlantComponent : Component, IAttackBy, IAttackHand, IOnDamageBehavior
    {
        public override string Name => "Plant";

        [ViewVariables(VVAccess.ReadWrite)]
        public float TimeSinceLastUpdate;

        [ViewVariables(VVAccess.ReadWrite)]
        public PlantHolderComponent Holder;
        public PlantDNA DNA => Owner.GetComponent<PlantDNAComponent>().DNA;

        [ViewVariables(VVAccess.ReadWrite)]
        public double cellularAgeInSeconds;
        [ViewVariables(VVAccess.ReadWrite)]
        public double progressInSeconds;

        [ViewVariables(VVAccess.ReadWrite)]
        public List<DamageDelta> damageDeltas;
        [ViewVariables(VVAccess.ReadWrite)]
        public bool dead;

        [ViewVariables(VVAccess.ReadWrite)]
        public string HarvestPrototype;
        [ViewVariables(VVAccess.ReadWrite)]
        public double YieldMultiplier;

        [ViewVariables(VVAccess.ReadWrite)]
        public List<BasicTransition> basicTransitions;

        [ViewVariables(VVAccess.ReadOnly)]
        public DamageThreshold DeathThreshold { get; private set; }
        [ViewVariables(VVAccess.ReadOnly)]
        public DamageThreshold DestructionThreshold { get; private set; }


        public void ApplyDelta(PlantDelta delta)
        {
            if (delta.setName != null)
            {
                Owner.GetComponent<IMetaDataComponent>().EntityName = delta.setName;
            }
            if (delta.setDescription != null)
            {
                Owner.GetComponent<IMetaDataComponent>().EntityDescription = delta.setDescription;
            }
            if (delta.setSprite != null)
            {
                if (Owner.TryGetComponent<SpriteComponent>(out var sprite))
                {
                    sprite.LayerSetSprite(0, delta.setSprite);
                }
            }

            if (delta.clearHarvests)
            {
                HarvestPrototype = null;
            }
            if (delta.setHarvestPrototype != null)
            {
                HarvestPrototype = delta.setHarvestPrototype;
            }
            if (delta.setDeath)
            {
                dead = true;
                basicTransitions.Clear(); // split this off as a transitions meta-operation
            }
            if (delta.destroy)
            {
                Owner.Delete();
            }

            foreach (var damageDelta in delta.damageDeltas)
            {
                // todo: removing, replacing, etc - could share this "meta" interface with transitions
                damageDeltas.Add(damageDelta);
            }

            if (delta.basicTransitions != null)
            {
                foreach (var transition in (List<BasicTransition>)delta.basicTransitions.Clone())
                {
                    if (transition.conditionIsAbsolute != true)
                    {
                        switch (transition.conditionType)
                        {
                            case BasicTransitionCondition.Progress:
                                transition.conditionAmount += progressInSeconds;
                                break;
                            case BasicTransitionCondition.CellularAge:
                                transition.conditionAmount += cellularAgeInSeconds;
                                break;
                        }
                    }
                    switch (transition.operation)
                    {
                        case BasicTransitionOperation.Add:
                            basicTransitions.Add(transition);
                            break;
                        case BasicTransitionOperation.Replace:
                            RemoveTransitionById(transition.transitionID);
                            basicTransitions.Add(transition);
                            break;
                        case BasicTransitionOperation.Remove:
                            RemoveTransitionById(transition.transitionID);
                            break;
                        case BasicTransitionOperation.RemoveAll:
                            basicTransitions.Clear();
                            break;
                        case BasicTransitionOperation.RemoveAllOfType:
                            basicTransitions.RemoveAll(x => x.conditionType == transition.conditionType);
                            break;

                    }
                }
            }
        }

        public void RemoveTransitionById(string Id)
        {
            var found = basicTransitions.Find(x => x.transitionID == Id);
            if (found != null)
            {
                basicTransitions.Remove(found);
            }
        }

        public void ApplyDeltas(List<PlantDelta> deltas)
        {
            foreach (var delta in deltas)
            {
                ApplyDelta(delta);
            }
        }

        public void ApplyDeltasFromIDs(List<string> deltaIDs)
        {
            var deltas = DNA.deltas.FindAll(x => deltaIDs.Contains(x.deltaID));
            foreach (var delta in deltas)
            {
                ApplyDelta(delta);
            }
        }

        public void ApplyStartingDeltas()
        {
            ApplyDeltasFromIDs(DNA.startingDeltaIDs);
        }


        /// <summary>
        /// Checks BasicTransitions that are not otherwise automatically triggered.
        /// </summary>
        public void CheckBasicTransitions()
        {
            foreach (var transition in basicTransitions.ToList())
            {
                var conditionAmount = transition.conditionAmount;
                double compareAmount = 0.0;
                switch (transition.conditionType)
                {
                    case BasicTransitionCondition.Progress:
                        compareAmount = progressInSeconds;
                        break;
                    case BasicTransitionCondition.CellularAge:
                        compareAmount = cellularAgeInSeconds;
                        break;
                    default:
                        continue;
                }
                if (compareAmount > conditionAmount)
                {
                    basicTransitions.Remove(transition);
                    ApplyDeltas(transition.Deltas);
                    ApplyDeltasFromIDs(transition.DeltaIDs);
                    return;
                }
            }
        }


        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref TimeSinceLastUpdate, "timeSinceLastUpdate", 0);
            serializer.DataField(ref cellularAgeInSeconds, "cellularAgeInSeconds", 0.0);
            serializer.DataField(ref progressInSeconds, "progressInSeconds", 0.0);

            serializer.DataField(ref damageDeltas, "damageDeltas", new List<DamageDelta>());
            serializer.DataField(ref dead, "dead", false);

            serializer.DataField(ref HarvestPrototype, "harvestPrototype", null);
            serializer.DataField(ref YieldMultiplier, "yieldMultiplier", 1.0);

            serializer.DataField(ref basicTransitions, "basicTransitions", new List<BasicTransition>());
        }

        public override void OnRemove()
        {
            base.OnRemove();
            Holder.HeldPlant = null;
        }

        public bool AttackBy(AttackByEventArgs eventArgs)
        {
            //using tools to create cuttings, harvest, heal, etc
            return false;
        }

        public bool AttackHand(AttackHandEventArgs eventArgs)
        {
            if (HarvestPrototype != null)
            {
                var entityManager = IoCManager.Resolve<IEntityManager>();

                var totalYield = YieldMultiplier;
                var rand = new Random();
                for (int i = 0; i <= totalYield; i++)
                {
                    entityManager.TrySpawnEntityAt(HarvestPrototype,
                        Owner.Transform.GridPosition.Offset(new Vector2((float)rand.NextDouble() - 0.5f, (float)rand.NextDouble() - 0.5f)),
                        out var harvest);
                    if (harvest.TryGetComponent<PlantDNAComponent>(out var dna))
                    {
                        dna.DNA = (PlantDNA)Owner.GetComponent<PlantDNAComponent>().DNA.Clone();
                    }
                    else
                    {
                        harvest.AddComponent<PlantDNAComponent>().DNA = (PlantDNA)Owner.GetComponent<PlantDNAComponent>().DNA.Clone();
                    }
                }
                foreach (var transition in basicTransitions.ToList())
                {
                    if (transition.conditionType == BasicTransitionCondition.Harvested)
                    {
                        basicTransitions.Remove(transition);
                        ApplyDeltas(transition.Deltas);
                        ApplyDeltasFromIDs(transition.DeltaIDs);
                    }
                }
                return true;
            }
            else
            {
                Owner.PopupMessage(eventArgs.User, "Nothing to harvest.");
                return false;
            }
        }

        public List<DamageThreshold> GetAllDamageThresholds()
        {
            DeathThreshold = new DamageThreshold(Shared.GameObjects.DamageType.Total, 100, ThresholdType.Death);
            DestructionThreshold = new DamageThreshold(Shared.GameObjects.DamageType.Total, 150, ThresholdType.Destruction);
            return new List<DamageThreshold>() { DeathThreshold, DestructionThreshold };
        }

        public void OnDamageThresholdPassed(object obj, DamageThresholdPassedEventArgs e)
        {
            if (e.Passed && e.DamageThreshold == DeathThreshold)
            {
                foreach (var transition in basicTransitions.ToList())
                {
                    if (transition.conditionType == BasicTransitionCondition.DeathThreshold)
                    {
                        basicTransitions.Remove(transition);
                        ApplyDeltas(transition.Deltas);
                        ApplyDeltasFromIDs(transition.DeltaIDs);
                    }
                }
            }
            if (e.Passed && e.DamageThreshold == DestructionThreshold)
            {
                Owner.Delete();
            }
        }
    }
}
