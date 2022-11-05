using Content.Shared.Atmos;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;
using Robust.Shared.Utility;
using System.Diagnostics;
using System.Linq;

namespace Content.Shared.Chemistry.Components
{
    /// <summary>
    ///     A solution of reagents.
    /// </summary>
    [Serializable, NetSerializable]
    [DataDefinition]
    public sealed partial class Solution : ISerializationHooks
    {
        // Most objects on the station hold only 1 or 2 reagents
        [ViewVariables]
        [DataField("reagents", customTypeSerializer:typeof(PrototypeIdDictionarySerializer<FixedPoint2, ReagentPrototype>))]
        public Dictionary<string, FixedPoint2> Contents = new(2);

        public FixedPoint2 this[string id] => Contents[id];

        /// <summary>
        ///     The calculated total volume of all reagents in the solution (ex. Total volume of liquid in beaker).
        /// </summary>
        [ViewVariables]
        public FixedPoint2 CurrentVolume { get; set; }

        /// <summary>
        ///     The temperature of the reagents in the solution.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("temperature")]
        public float Temperature = Atmospherics.T20C;

        /// <summary>
        ///     The name of this solution, if it is contained in some <see cref="SolutionContainerManagerComponent"/>
        /// </summary>
        public string? Name;

        /// <summary>
        ///     Constructs an empty solution (ex. an empty beaker).
        /// </summary>
        public Solution() { }

        /// <summary>
        ///     Constructs a solution containing 100% of a reagent (ex. A beaker of pure water).
        /// </summary>
        /// <param name="reagentId">The prototype ID of the reagent to add.</param>
        /// <param name="quantity">The quantity in milli-units.</param>
        public Solution(string reagentId, FixedPoint2 quantity, IPrototypeManager? proto)
        {
            IoCManager.Resolve(ref proto);
            AddReagent(proto.Index<ReagentPrototype>(reagentId), quantity);
        }

        /// <summary>
        ///     Constructs a solution containing 100% of a reagent (ex. A beaker of pure water).
        /// </summary>
        /// <param name="proto">The prototype ID of the reagent to add.</param>
        /// <param name="quantity">The quantity in milli-units.</param>
        public Solution(ReagentPrototype proto, FixedPoint2 quantity)
        {
            AddReagent(proto, quantity);
        }

        void ISerializationHooks.AfterDeserialization()
        {
            // TODO solution serializer
            CurrentVolume = Contents.Values.Sum();
            if (MaxVolume == FixedPoint2.Zero)
                MaxVolume = CurrentVolume;
            ValidateSolution();
        }

        public bool ContainsReagent(string reagentId) => Contents.ContainsKey(reagentId);

        public bool TryGetReagent(string reagentId, out FixedPoint2 quantity) => Contents.TryGetValue(reagentId, out quantity);

        public string GetPrimaryReagentId()
        {
            if (Contents.Count == 0)
                throw new InvalidOperationException("Empty solution has no primary reagent");

            var max = FixedPoint2.Zero;
            string maxId = "";
            foreach (var (id, quantity) in Contents)
            {
                if (quantity > max)
                {
                    max = quantity;
                    maxId = id;
                }
            }
            return maxId;
        }

        /// <summary>
        ///     Adds a quantity of a reagent at some specific temperature directly into the solution.
        /// </summary>
        public FixedPoint2 AddReagent(string reagentId, FixedPoint2 quantity, float? temperature = null, IPrototypeManager? protoMan)
        {
            IoCManager.Resolve(ref protoMan);
            return AddReagent(protoMan.Index<ReagentPrototype>(reagentId), quantity, temperature);
        }

        /// <summary>
        ///     Adds a quantity of a reagent at some specific temperature directly into the solution.
        /// </summary>
        public FixedPoint2 AddReagent(ReagentPrototype proto, FixedPoint2 quantity, float? temperature = null)
        {
            quantity = FixedPoint2.Min(quantity, AvailableVolume);
            if (quantity <= 0)
                return FixedPoint2.Zero;

            Contents[proto.ID] = Contents.TryGetValue(proto.ID, out var existing)
                ? quantity + existing
                : quantity;

            CurrentVolume += quantity;

            var addedHeatCap = (float) quantity * proto.SpecificHeat;
            var termalEnergy = ThermalEnergy + addedHeatCap * (temperature ?? Temperature);
            HeatCapacity += addedHeatCap;
            Temperature = termalEnergy / HeatCapacity;

            ValidateSolution();
            return quantity;

        }

        /// <summary>
        ///     Returns the total heat capacity of the reagents in this solution.
        /// </summary>
        /// <returns>The total heat capacity of the reagents in this solution.</returns>
        private void UpdateHeatCapacity(IPrototypeManager? protoMan)
        {
            IoCManager.Resolve(ref protoMan);
            HeatCapacity = 0;
            foreach (var (id, quantity) in Contents)
            {
                HeatCapacity += (float) quantity * protoMan.Index<ReagentPrototype>(id).SpecificHeat;
            }
        }

        /// <summary>
        ///     Scales the amount of solution. Currently only supports solutions that have no maximum volume.
        /// </summary>
        /// <param name="scale">The scalar to modify the solution by.</param>
        public void ScaleSolution(float scale, IPrototypeManager? protoMan)
        {
            DebugTools.AssertNull(MaxVolume);

            if (scale.Equals(1f))
                return;

            CurrentVolume = 0;

            foreach(var (id, quantity) in Contents)
            {
                var newQuant = quantity * scale;
                Contents[id] = newQuant;
                CurrentVolume += newQuant;
            }
            UpdateHeatCapacity(protoMan);
        }

        /// <summary>
        ///     Returns the amount of a single reagent inside the solution.
        /// </summary>
        /// <param name="reagentId">The prototype ID of the reagent to add.</param>
        /// <returns>The quantity in milli-units.</returns>
        public FixedPoint2 GetReagentQuantity(string reagentId) => Contents.GetValueOrDefault(reagentId);

        /// <summary>
        ///     Attempts to remove an amount of reagent from the solution.
        /// </summary>
        /// <param name="reagentId">The reagent to be removed.</param>
        /// <param name="quantity">The amount of reagent to remove.</param>
        /// <returns>How much reagent was actually removed. Zero if the reagent is not present on the solution.</returns>
        public FixedPoint2 RemoveReagent(string reagentId, FixedPoint2 quantity, IPrototypeManager? protoMan)
        {
            IoCManager.Resolve(ref protoMan);
            return RemoveReagent(protoMan.Index<ReagentPrototype>(reagentId), quantity);
        }

        /// <summary>
        ///     Attempts to remove an amount of reagent from the solution.
        /// </summary>
        /// <param name="proto">The reagent to be removed.</param>
        /// <param name="quantity">The amount of reagent to remove.</param>
        /// <returns>How much reagent was actually removed. Zero if the reagent is not present on the solution.</returns>
        public FixedPoint2 RemoveReagent(ReagentPrototype proto, FixedPoint2 quantity)
        {
            if (quantity <= 0 || !Contents.TryGetValue(proto.ID, out var existing))
                return FixedPoint2.Zero;

            if (quantity >= existing)
            {
                Contents.Remove(proto.ID);
                CurrentVolume -= existing;
                HeatCapacity -= proto.SpecificHeat * existing.Float();
                ValidateSolution();
                return existing;
            }

            Contents[proto.ID] = existing - quantity; ;
            CurrentVolume -= quantity;
            HeatCapacity -= proto.SpecificHeat * quantity.Float();
            ValidateSolution();
            return quantity;

        }

        [Conditional("DEBUG")]
        [AssertionMethod]
        private void ValidateSolution(IPrototypeManager? protoMan = null)
        {
            DebugTools.Assert(CurrentVolume == Contents.Values.Sum());
            DebugTools.Assert(AvailableVolume >= FixedPoint2.Zero);
            DebugTools.Assert(!Contents.Values.Any(x => x == FixedPoint2.Zero));

            var oldCap = HeatCapacity;
            UpdateHeatCapacity(protoMan);
            DebugTools.Assert(oldCap == HeatCapacity);
        }

        /// <summary>
        /// Remove the specified quantity from this solution.
        /// </summary>
        /// <param name="toTake">The quantity of this solution to remove</param>
        public void RemoveSolution(FixedPoint2 toTake, IPrototypeManager? protoMan)
        {
            if(toTake >= CurrentVolume)
            {
                RemoveAllSolution();
                return;
            }

            foreach (var (id, quantity) in Contents)
            {
                var taken = quantity * toTake / CurrentVolume;
                if (taken == FixedPoint2.Zero)
                    continue;

                CurrentVolume -= taken;
                if (quantity == taken)
                    Contents.Remove(id);
                else
                    Contents[id] = quantity - taken;
            }

            UpdateHeatCapacity(protoMan);
            ValidateSolution(protoMan);
        }

        public void RemoveAllSolution()
        {
            Contents.Clear();
            CurrentVolume = FixedPoint2.New(0);
            HeatCapacity = 0;
            ValidateSolution();
        }

        public Solution SplitSolution(FixedPoint2 toTake, IPrototypeManager? protoMan)
        {
            if (toTake <= 0)
                return new Solution();

            Solution newSolution;
            if (toTake >= CurrentVolume)
            {
                newSolution = Clone();
                RemoveAllSolution();
                newSolution.ValidateSolution(protoMan);
                return newSolution;
            }

            newSolution = new()
            {
                Contents = new(Contents.Count),
                Temperature = Temperature
            };

            foreach (var (id, quantity) in Contents)
            {
                var taken = quantity * toTake / CurrentVolume;
                if (taken == FixedPoint2.Zero)
                    continue;

                CurrentVolume -= taken;
                newSolution.CurrentVolume += taken;
                newSolution.Contents[id] = taken;
                if (quantity == taken)
                    Contents.Remove(id);
                else
                    Contents[id] = quantity - taken;
            }

            IoCManager.Resolve(ref protoMan);
            UpdateHeatCapacity(protoMan);
            newSolution.UpdateHeatCapacity(protoMan);

            ValidateSolution(protoMan);
            newSolution.ValidateSolution(protoMan);

            return newSolution;
        }

        /// <summary>
        ///     Removes an amount from all reagents in a solution, adding it to a new solution.
        /// </summary>
        /// <param name="uid">The entity containing the solution.</param>
        /// <param name="solution">The solution to remove reagents from.</param>
        /// <param name="quantity">The amount to remove from every reagent in the solution.</param>
        /// <returns>A new solution containing every removed reagent from the original solution.</returns>
        public Solution RemoveEachReagent(FixedPoint2 toTake, IPrototypeManager? protoMan)
        {
            if (toTake <= 0)
                return new Solution();

            Solution newSolution;

            newSolution = new()
            {
                Contents = new(Contents.Count),
                Temperature = Temperature
            };

            foreach (var (id, quantity) in Contents)
            {
                if (toTake >= quantity)
                {
                    CurrentVolume -= quantity;
                    Contents.Remove(id);
                    newSolution.CurrentVolume += quantity;
                    newSolution.Contents[id] = quantity;
                }
                else
                {
                    CurrentVolume -= toTake;
                    newSolution.CurrentVolume += toTake;
                    newSolution.Contents[id] = toTake;
                    Contents[id] = quantity - toTake;
                }
            }

            IoCManager.Resolve(ref protoMan);
            UpdateHeatCapacity(protoMan);
            newSolution.UpdateHeatCapacity(protoMan);

            ValidateSolution(protoMan);
            newSolution.ValidateSolution(protoMan);

            return newSolution;
        }

        /// <summary>
        ///     Add another solution to this one. This ignores max-volume. Does not modify the other solution
        /// </summary>
        public void AddSolution(Solution otherSolution)
        {
            if (otherSolution.CurrentVolume <= FixedPoint2.Zero)
                return;

            var totalThermalEnergy = ThermalEnergy + otherSolution.ThermalEnergy;
            HeatCapacity = HeatCapacity + otherSolution.HeatCapacity;

            CurrentVolume += otherSolution.CurrentVolume;

            foreach (var (id, quantity) in otherSolution.Contents)
            {
                Contents[id] = Contents.TryGetValue(id, out var existing)
                    ? quantity + existing
                    : quantity;
            }

            CurrentVolume += otherSolution.CurrentVolume;
            Temperature = totalThermalEnergy / HeatCapacity;
            ValidateSolution();
        }

        private Color GetColor(IPrototypeManager? protoMan)
        {
            if (CurrentVolume == 0)
            {
                return Color.Transparent;
            }

            IoCManager.Resolve(ref protoMan);

            Color mixColor = default;
            var runningTotalQuantity = FixedPoint2.New(0);
            var first = true;

            foreach (var (id, quantity) in Contents)
            {
                runningTotalQuantity += quantity;

                if (first)
                {
                    mixColor = protoMan.Index<ReagentPrototype>(id).SubstanceColor;
                    first = false;
                    continue;
                }

                var interpolateValue = quantity.Float() / runningTotalQuantity.Float();
                mixColor = Color.InterpolateBetween(mixColor, protoMan.Index<ReagentPrototype>(id).SubstanceColor, interpolateValue);
            }
            return mixColor;
        }

        public Solution Clone()
        {
            return new Solution()
            {
                Contents = Contents.ShallowClone(),
                HeatCapacity = HeatCapacity,
                CurrentVolume = CurrentVolume,
                Temperature = Temperature
            };
        }

        [Obsolete("Use ReactiveSystem.DoEntityReaction")]
        public void DoEntityReaction(EntityUid uid, ReactionMethod method)
        {
            IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ReactiveSystem>().DoEntityReaction(uid, this, method);
        }
    }
}
