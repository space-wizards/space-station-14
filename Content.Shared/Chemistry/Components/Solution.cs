using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;

namespace Content.Shared.Chemistry.Components
{
    /// <summary>
    ///     A solution of reagents.
    /// </summary>
    [Serializable, NetSerializable]
    [DataDefinition]
    public sealed partial class Solution : IEnumerable<Solution.ReagentQuantity>, ISerializationHooks
    {
        // Most objects on the station hold only 1 or 2 reagents
        [DataField("reagents")]
        public List<ReagentQuantity> Contents = new(2);

        /// <summary>
        ///     The calculated total volume of all reagents in the solution (ex. Total volume of liquid in beaker).
        /// </summary>
        [ViewVariables]
        public FixedPoint2 TotalVolume { get; set; }

        /// <summary>
        ///     The temperature of the reagents in the solution.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("temperature")]
        public float Temperature { get; set; } = 293.15f;

        public Color Color => GetColor();

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
        public Solution(string reagentId, FixedPoint2 quantity)
        {
            AddReagent(reagentId, quantity);
        }

        void ISerializationHooks.AfterDeserialization()
        {
            TotalVolume = FixedPoint2.Zero;
            Contents.ForEach(reagent => TotalVolume += reagent.Quantity);
        }

        public bool ContainsReagent(string reagentId)
        {
            return ContainsReagent(reagentId, out _);
        }

        public bool ContainsReagent(string reagentId, out FixedPoint2 quantity)
        {
            foreach (var reagent in Contents)
            {
                if (reagent.ReagentId == reagentId)
                {
                    quantity = reagent.Quantity;
                    return true;
                }
            }

            quantity = FixedPoint2.New(0);
            return false;
        }

        public string GetPrimaryReagentId()
        {
            if (Contents.Count == 0)
            {
                return "";
            }

            var majorReagent = Contents.MaxBy(reagent => reagent.Quantity);
            return majorReagent.ReagentId;
        }

        /// <summary>
        ///     Adds a given quantity of a reagent directly into the solution.
        /// </summary>
        /// <param name="reagentId">The prototype ID of the reagent to add.</param>
        /// <param name="quantity">The quantity in milli-units.</param>
        public void AddReagent(string reagentId, FixedPoint2 quantity, float? temperature = null)
        {
            if (quantity <= 0)
                return;
            if (!IoCManager.Resolve<IPrototypeManager>().TryIndex(reagentId, out ReagentPrototype? proto))
                proto = new ReagentPrototype();

            var actualTemp = temperature ?? Temperature;
            var oldThermalEnergy = Temperature * GetHeatCapacity();
            var addedThermalEnergy = (float) quantity * proto.SpecificHeat * actualTemp;
            for (var i = 0; i < Contents.Count; i++)
            {
                var reagent = Contents[i];
                if (reagent.ReagentId != reagentId)
                    continue;

                Contents[i] = new ReagentQuantity(reagentId, reagent.Quantity + quantity);

                TotalVolume += quantity;
                ThermalEnergy = oldThermalEnergy + addedThermalEnergy;
                return;
            }

            Contents.Add(new ReagentQuantity(reagentId, quantity));

            TotalVolume += quantity;
            ThermalEnergy = oldThermalEnergy + addedThermalEnergy;
        }

        /// <summary>
        ///     Scales the amount of solution.
        /// </summary>
        /// <param name="scale">The scalar to modify the solution by.</param>
        public void ScaleSolution(float scale)
        {
            if (scale.Equals(1f))
                return;

            var tempContents = new List<ReagentQuantity>(Contents);
            foreach(var current in tempContents)
            {
                if(scale > 1)
                {
                    AddReagent(current.ReagentId, current.Quantity * scale - current.Quantity);
                }
                else
                {
                    RemoveReagent(current.ReagentId, current.Quantity - current.Quantity * scale);
                }
            }
        }

        /// <summary>
        ///     Returns the amount of a single reagent inside the solution.
        /// </summary>
        /// <param name="reagentId">The prototype ID of the reagent to add.</param>
        /// <returns>The quantity in milli-units.</returns>
        public FixedPoint2 GetReagentQuantity(string reagentId)
        {
            for (var i = 0; i < Contents.Count; i++)
            {
                if (Contents[i].ReagentId == reagentId)
                    return Contents[i].Quantity;
            }

            return FixedPoint2.New(0);
        }

        /// <summary>
        ///     Attempts to remove an amount of reagent from the solution.
        /// </summary>
        /// <param name="reagentId">The reagent to be removed.</param>
        /// <param name="quantity">The amount of reagent to remove.</param>
        /// <returns>How much reagent was actually removed. Zero if the reagent is not present on the solution.</returns>
        public FixedPoint2 RemoveReagent(string reagentId, FixedPoint2 quantity)
        {
            if(quantity <= 0)
                return FixedPoint2.Zero;

            for (var i = 0; i < Contents.Count; i++)
            {
                var reagent = Contents[i];

                if(reagent.ReagentId != reagentId)
                    continue;

                var curQuantity = reagent.Quantity;
                var newQuantity = curQuantity - quantity;

                if (newQuantity <= 0)
                {
                    Contents.RemoveSwap(i);
                    TotalVolume -= curQuantity;
                    return curQuantity;
                }

                Contents[i] = new ReagentQuantity(reagentId, newQuantity);
                TotalVolume -= quantity;
                return quantity;
            }

            // Reagent is not on the solution...
            return FixedPoint2.Zero;
        }

        /// <summary>
        /// Remove the specified quantity from this solution.
        /// </summary>
        /// <param name="quantity">The quantity of this solution to remove</param>
        public void RemoveSolution(FixedPoint2 quantity)
        {
            if(quantity <= 0)
                return;

            var ratio = (TotalVolume - quantity).Double() / TotalVolume.Double();

            if (ratio <= 0)
            {
                RemoveAllSolution();
                return;
            }

            for (var i = 0; i < Contents.Count; i++)
            {
                var reagent = Contents[i];
                var oldQuantity = reagent.Quantity;

                // quantity taken is always a little greedy, so fractional quantities get rounded up to the nearest
                // whole unit. This should prevent little bits of chemical remaining because of float rounding errors.
                var newQuantity = oldQuantity * ratio;

                Contents[i] = new ReagentQuantity(reagent.ReagentId, newQuantity);
            }

            TotalVolume *= ratio;
        }

        public void RemoveAllSolution()
        {
            Contents.Clear();
            TotalVolume = FixedPoint2.New(0);
        }

        public Solution SplitSolution(FixedPoint2 quantity)
        {
            if (quantity <= 0)
                return new Solution();

            Solution newSolution;

            if (quantity >= TotalVolume)
            {
                newSolution = Clone();
                RemoveAllSolution();
                return newSolution;
            }

            newSolution = new Solution();
            var newTotalVolume = FixedPoint2.New(0);
            var newHeatCapacity = 0.0d;
            var remainingVolume = TotalVolume;
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            for (var i = Contents.Count - 1; i >= 0; i--)
            {
                if (remainingVolume == FixedPoint2.Zero)
                    // shouldn't happen, but it can if someone, somehow has a reagent with 0-quantity in a solution.
                    break;

                var reagent = Contents[i];
                var ratio = (remainingVolume - quantity).Double() / remainingVolume.Double();
                if(!prototypeManager.TryIndex(reagent.ReagentId, out ReagentPrototype? proto))
                    proto = new ReagentPrototype();

                remainingVolume -= reagent.Quantity;

                var newQuantity = reagent.Quantity * ratio;
                var splitQuantity = reagent.Quantity - newQuantity;

                if (newQuantity > 0)
                    Contents[i] = new ReagentQuantity(reagent.ReagentId, newQuantity);
                else
                    Contents.RemoveAt(i);

                if (splitQuantity > 0)
                    newSolution.Contents.Add(new ReagentQuantity(reagent.ReagentId, splitQuantity));

                newTotalVolume += splitQuantity;
                newHeatCapacity += (float) splitQuantity * proto.SpecificHeat;
                quantity -= splitQuantity;
            }

            newSolution.TotalVolume = newTotalVolume;
            newSolution.Temperature = Temperature;
            TotalVolume -= newTotalVolume;

            return newSolution;
        }

        public void AddSolution(Solution otherSolution)
        {
            var oldThermalEnergy = Temperature * GetHeatCapacity();
            var addedThermalEnergy = otherSolution.Temperature * otherSolution.GetHeatCapacity();
            for (var i = 0; i < otherSolution.Contents.Count; i++)
            {
                var otherReagent = otherSolution.Contents[i];

                var found = false;
                for (var j = 0; j < Contents.Count; j++)
                {
                    var reagent = Contents[j];
                    if (reagent.ReagentId == otherReagent.ReagentId)
                    {
                        found = true;
                        Contents[j] = new ReagentQuantity(reagent.ReagentId, reagent.Quantity + otherReagent.Quantity);
                        break;
                    }
                }

                if (!found)
                {
                    Contents.Add(new ReagentQuantity(otherReagent.ReagentId, otherReagent.Quantity));
                }
            }

            TotalVolume += otherSolution.TotalVolume;
            ThermalEnergy = oldThermalEnergy + addedThermalEnergy;
        }

        private Color GetColor()
        {
            if (TotalVolume == 0)
            {
                return Color.Transparent;
            }

            Color mixColor = default;
            var runningTotalQuantity = FixedPoint2.New(0);
            var protoManager = IoCManager.Resolve<IPrototypeManager>();

            foreach (var reagent in Contents)
            {
                runningTotalQuantity += reagent.Quantity;

                if (!protoManager.TryIndex(reagent.ReagentId, out ReagentPrototype? proto))
                {
                    continue;
                }

                if (mixColor == default)
                {
                    mixColor = proto.SubstanceColor;
                    continue;
                }

                var interpolateValue = (1 / runningTotalQuantity.Float()) * reagent.Quantity.Float();
                mixColor = Color.InterpolateBetween(mixColor, proto.SubstanceColor, interpolateValue);
            }
            return mixColor;
        }

        public Solution Clone()
        {
            var volume = FixedPoint2.New(0);
            var heatCapacity = 0.0d;
            var newSolution = new Solution();
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            for (var i = 0; i < Contents.Count; i++)
            {
                var reagent = Contents[i];
                if (!prototypeManager.TryIndex(reagent.ReagentId, out ReagentPrototype? proto))
                    proto = new ReagentPrototype();

                newSolution.Contents.Add(reagent);
                volume += reagent.Quantity;
                heatCapacity += (float) reagent.Quantity * proto.SpecificHeat;
            }

            newSolution.TotalVolume = volume;
            newSolution.Temperature = Temperature;
            return newSolution;
        }

        [Obsolete("Use ReactiveSystem.DoEntityReaction")]
        public void DoEntityReaction(EntityUid uid, ReactionMethod method)
        {
            IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ReactiveSystem>().DoEntityReaction(uid, this, method);
        }

        [Serializable, NetSerializable]
        [DataDefinition]
        public readonly struct ReagentQuantity: IComparable<ReagentQuantity>
        {
            [DataField("ReagentId", customTypeSerializer:typeof(PrototypeIdSerializer<ReagentPrototype>))]
            public readonly string ReagentId;
            [DataField("Quantity")]
            public readonly FixedPoint2 Quantity;

            public ReagentQuantity(string reagentId, FixedPoint2 quantity)
            {
                ReagentId = reagentId;
                Quantity = quantity;
            }

            [ExcludeFromCodeCoverage]
            public override string ToString()
            {
                return $"{ReagentId}:{Quantity}";
            }

            public int CompareTo(ReagentQuantity other) { return Quantity.Float().CompareTo(other.Quantity.Float()); }

            public void Deconstruct(out string reagentId, out FixedPoint2 quantity)
            {
                reagentId = ReagentId;
                quantity = Quantity;
            }
        }

        #region Enumeration

        public IEnumerator<ReagentQuantity> GetEnumerator()
        {
            return Contents.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
