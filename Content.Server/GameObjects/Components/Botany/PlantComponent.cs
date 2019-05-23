using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Shared.Interfaces;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
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
    class PlantComponent : Component, IAttackBy, IAttackHand, IOnDamageBehavior, EntitySystems.IExamine
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
        public string flavorText;

        [ViewVariables(VVAccess.ReadWrite)]
        public double mutationProbability;
        [ViewVariables(VVAccess.ReadWrite)]
        public List<SpeciationDelta> speciationDeltas;

        [ViewVariables(VVAccess.ReadWrite)]
        public List<HarvestDelta> harvestDeltas;
        [ViewVariables(VVAccess.ReadWrite)]
        public List<DamageDelta> damageDeltas;
        [ViewVariables(VVAccess.ReadWrite)]
        public bool dead;


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
                flavorText = delta.setDescription;
            }
            if (delta.setSprite != null)
            {
                if (Owner.TryGetComponent<SpriteComponent>(out var sprite))
                {
                    sprite.LayerSetSprite(0, delta.setSprite);
                }
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

            if (delta.mutationDelta != null)
            {
                var mut = delta.mutationDelta;
                switch (mut.operation)
                {
                    case NumericOperation.Add:
                        mutationProbability = Math.Max(0.0, Math.Min(1.0, mutationProbability + mut.amount));
                        break;
                    case NumericOperation.Set:
                        mutationProbability = Math.Max(0.0, Math.Min(1.0, mut.amount));
                        break;
                }
            }

            foreach (var speciationDelta in delta.speciationDeltas)
            {
                // todo: implement IListDelta
                speciationDeltas.Add(speciationDelta);
            }

            foreach (var harvestDelta in delta.harvestDeltas)
            {
                ApplyListDelta(harvestDelta, harvestDeltas, x => x.toolRequired == harvestDelta.toolRequired);
            }

            foreach (var damageDelta in delta.damageDeltas)
            {
                ApplyListDelta(damageDelta, damageDeltas, x => x.type == damageDelta.type);
            }

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
                ApplyListDelta(transition, basicTransitions, x => x.conditionType == transition.conditionType);
            }
        }

        public void ApplyListDelta<T>(T entry, List<T> list, Predicate<T> pred) where T : IListDelta
        {
            switch (entry.GetOperation())
            {
                case ListDeltaOperation.Add:
                    list.Add(entry);
                    break;
                case ListDeltaOperation.Replace:
                    var foundForReplacement = list.Find(x => x.GetID() == entry.GetID());
                    if (foundForReplacement != null)
                    {
                        list.Remove(foundForReplacement);
                    }
                    list.Add(entry);
                    break;
                case ListDeltaOperation.Remove:
                    var foundForRemoval = list.Find(x => x.GetID() == entry.GetID());
                    if (foundForRemoval != null)
                    {
                        list.Remove(foundForRemoval);
                    }
                    break;
                case ListDeltaOperation.RemoveAll:
                    list.Clear();
                    break;
                case ListDeltaOperation.RemoveAllOfType: // I feel uneasy about this one, it could be tightened/removed
                    list.RemoveAll(pred);
                    break;
            }
        }

        public void ApplyDeltas(List<PlantDelta> deltas)
        {
            foreach (var delta in deltas)
            {
                ApplyDelta(delta);
            }
        }

        public void ApplyDeltaFromID(string deltaID)
        {
            var shadowingDelta = DNA.deltas.Find(x => x.ID == deltaID);
            if (shadowingDelta != null)
            {
                ApplyDelta(shadowingDelta);
            }
            else
            {
                var prototypeMan = IoCManager.Resolve<IPrototypeManager>();
                try
                {
                    var delta = prototypeMan.Index<PlantDelta>(deltaID);
                    ApplyDelta(delta);
                }
                catch (KeyNotFoundException e)
                {
                    Logger.GetSawmill("Plant").Error("PlantComponent " + this + " failed to index PlantDelta: " + deltaID + ". Exception details: " + e.Message);
                }
            }
        }

        public void ApplyDeltasFromIDs(List<string> deltaIDs)
        {
            foreach (var ID in deltaIDs)
            {
                ApplyDeltaFromID(ID);
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

            serializer.DataField(ref mutationProbability, "mutationProbability", 0.0);
            serializer.DataField(ref speciationDeltas, "speciationDeltas", new List<SpeciationDelta>());

            serializer.DataField(ref harvestDeltas, "harvestDeltas", new List<HarvestDelta>());
            serializer.DataField(ref damageDeltas, "damageDeltas", new List<DamageDelta>());
            serializer.DataField(ref dead, "dead", false);

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
            foreach (var harvestDelta in harvestDeltas)
            {
                if (harvestDelta.toolRequired != HarvestTool.None)
                {
                    continue;
                }
                var entityManager = IoCManager.Resolve<IEntityManager>();
                var yield = harvestDelta.yield;
                var rand = new Random();

                for (int i = 0; i <= yield; i++)
                {
                    entityManager.TrySpawnEntityAt(harvestDelta.prototype,
                        Owner.Transform.GridPosition.Offset(new Vector2((float)rand.NextDouble() - 0.5f, (float)rand.NextDouble() - 0.5f)),
                        out var harvest);
                    if (!harvestDelta.sterile)
                    {
                        if (harvest.TryGetComponent<PlantDNAComponent>(out var dna))
                        {
                            dna.DNA = (PlantDNA)Owner.GetComponent<PlantDNAComponent>().DNA.Clone();
                        }
                        else
                        {
                            harvest.AddComponent<PlantDNAComponent>().DNA = (PlantDNA)Owner.GetComponent<PlantDNAComponent>().DNA.Clone();
                        }
                    }
                }
                // We're potentially removing the harvestDelta after this, so let's copy its ID
                var harvestID = harvestDelta.GetID();
                foreach (var transition in basicTransitions.ToList())
                {
                    if (transition.conditionType == BasicTransitionCondition.Harvested &&
                        (transition.conditionID == null ||
                        transition.conditionID == harvestID))
                    {
                        basicTransitions.Remove(transition);
                        ApplyDeltas(transition.Deltas);
                        ApplyDeltasFromIDs(transition.DeltaIDs);
                    }
                }
                return true;
            }
            Owner.PopupMessage(eventArgs.User, "Nothing to harvest.");
            return false;
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

        public void Examine(FormattedMessage message)
        {
            if (flavorText != null)
            {
                message.PushColor(Color.Gray);
                message.AddText(flavorText);
                message.Pop();
            }
        }
    }
}
