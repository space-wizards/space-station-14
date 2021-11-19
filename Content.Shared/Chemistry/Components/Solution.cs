using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Chemistry.Components
{
    /// <summary>
    ///     A solution of reagents.
    /// </summary>
    [Serializable, NetSerializable]
    [DataDefinition]
    public partial class Solution : IEnumerable<Solution.ReagentQuantity>, ISerializationHooks
    {
        // Most objects on the station hold only 1 or 2 reagents
        [ViewVariables]
        [DataField("reagents")]
        public List<ReagentQuantity> Contents = new(2);

        /// <summary>
        ///     The calculated total volume of all reagents in the solution (ex. Total volume of liquid in beaker).
        /// </summary>
        [ViewVariables]
        public FixedPoint2 TotalVolume { get; set; }

        public Color Color => GetColor();

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

            var majorReagent = Contents.OrderByDescending(reagent => reagent.Quantity).First();
            return majorReagent.ReagentId;
        }

        /// <summary>
        ///     Adds a given quantity of a reagent directly into the solution.
        /// </summary>
        /// <param name="reagentId">The prototype ID of the reagent to add.</param>
        /// <param name="quantity">The quantity in milli-units.</param>
        public void AddReagent(string reagentId, FixedPoint2 quantity)
        {
            if (quantity <= 0)
                return;

            for (var i = 0; i < Contents.Count; i++)
            {
                var reagent = Contents[i];
                if (reagent.ReagentId != reagentId)
                    continue;

                Contents[i] = new ReagentQuantity(reagentId, reagent.Quantity + quantity);
                TotalVolume += quantity;
                return;
            }

            Contents.Add(new ReagentQuantity(reagentId, quantity));
            TotalVolume += quantity;
        }

        /// <summary>
        ///     Scales the amount of solution.
        /// </summary>
        /// <param name="scale">The scalar to modify the solution by.</param>
        public void ScaleSolution(float scale)
        {
            if (scale == 1) return;
            var tempContents = new List<ReagentQuantity>(Contents);
            foreach(ReagentQuantity current in tempContents)
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

        public void RemoveReagent(string reagentId, FixedPoint2 quantity)
        {
            if(quantity <= 0)
                return;

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
                }
                else
                {
                    Contents[i] = new ReagentQuantity(reagentId, newQuantity);
                    TotalVolume -= quantity;
                }

                return;
            }
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

            TotalVolume = TotalVolume * ratio;
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
            var remainingVolume = TotalVolume;

            for (var i = 0; i < Contents.Count; i++)
            {
                var reagent = Contents[i];
                var ratio = (remainingVolume - quantity).Double() / remainingVolume.Double();
                remainingVolume -= reagent.Quantity;

                var newQuantity = reagent.Quantity * ratio;
                var splitQuantity = reagent.Quantity - newQuantity;

                Contents[i] = new ReagentQuantity(reagent.ReagentId, newQuantity);
                newSolution.Contents.Add(new ReagentQuantity(reagent.ReagentId, splitQuantity));
                newTotalVolume += splitQuantity;
                quantity -= splitQuantity;
            }

            newSolution.TotalVolume = newTotalVolume;
            TotalVolume -= newTotalVolume;

            return newSolution;
        }

        public void AddSolution(Solution otherSolution)
        {
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
            var newSolution = new Solution();

            for (var i = 0; i < Contents.Count; i++)
            {
                var reagent = Contents[i];
                newSolution.Contents.Add(reagent);
                volume += reagent.Quantity;
            }

            newSolution.TotalVolume = volume;
            return newSolution;
        }

        public void DoEntityReaction(EntityUid uid, ReactionMethod method)
        {
            var chemistry = EntitySystem.Get<ChemistrySystem>();

            foreach (var (reagentId, quantity) in Contents.ToArray())
            {
                chemistry.ReactionEntity(uid, method, reagentId, quantity, this);
            }
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
