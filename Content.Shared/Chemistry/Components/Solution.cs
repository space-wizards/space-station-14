using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Shared.Chemistry.Components
{
    /// <summary>
    ///     A solution of reagents.
    /// </summary>
    [Serializable, NetSerializable]
    [DataDefinition]
    public sealed partial class Solution : IEnumerable<Solution.ReagentQuantity>, ISerializationHooks
    {
        // This is a list because it is actually faster to add and remove reagents from
        // a list than a dictionary, though contains-reagent checks are slightly slower,
        [DataField("reagents")]
        public List<ReagentQuantity> Contents = new(2);

        /// <summary>
        ///     The calculated total volume of all reagents in the solution (ex. Total volume of liquid in beaker).
        /// </summary>
        [ViewVariables]
        public FixedPoint2 Volume { get; set; }

        /// <summary>
        ///     Maximum volume this solution supports.
        /// </summary>
        /// <remarks>
        ///     A value of zero means the maximum will automatically be set equal to the current volume during
        ///     initialization. Note that most solution methods ignore max volume altogether, but various solution
        ///     systems use this.
        /// </remarks>
        [DataField("maxVol")]
        [ViewVariables(VVAccess.ReadWrite)]
        public FixedPoint2 MaxVolume { get; set; } = FixedPoint2.Zero;

        public float FillFraction => MaxVolume == 0 ? 1 : Volume.Float() / MaxVolume.Float();

        /// <summary>
        ///     If reactions will be checked for when adding reagents to the container.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("canReact")]
        public bool CanReact { get; set; } = true;

        /// <summary>
        ///     If reactions can occur via mixing.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("canMix")]
        public bool CanMix { get; set; } = false;

        /// <summary>
        ///     Volume needed to fill this container.
        /// </summary>
        [ViewVariables]
        public FixedPoint2 AvailableVolume => MaxVolume - Volume;

        /// <summary>
        ///     The temperature of the reagents in the solution.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("temperature")]
        public float Temperature { get; set; } = 293.15f;

        /// <summary>
        ///     The name of this solution, if it is contained in some <see cref="SolutionContainerManagerComponent"/>
        /// </summary>
        public string? Name;

        /// <summary>
        ///     Checks if a solution can fit into the container.
        /// </summary>
        public bool CanAddSolution(Solution solution)
        {
            return solution.Volume <= AvailableVolume;
        }

        /// <summary>
        ///     The total heat capacity of all reagents in the solution.
        /// </summary>
        [ViewVariables]
        private float _heatCapacity;

        /// <summary>
        ///     If true, then <see cref="_heatCapacity"/> needs to be recomputed.
        /// </summary>
        [ViewVariables]
        private bool _heatCapacityDirty = true;

        public void UpdateHeatCapacity(IPrototypeManager? protoMan)
        {
            IoCManager.Resolve(ref protoMan);
            DebugTools.Assert(_heatCapacityDirty);
            _heatCapacityDirty = false;
            _heatCapacity = 0;
            foreach (var reagent in Contents)
            {
                _heatCapacity += (float) reagent.Quantity * protoMan.Index<ReagentPrototype>(reagent.ReagentId).SpecificHeat;
            }
        }

        public float GetHeatCapacity(IPrototypeManager? protoMan)
        {
            if (_heatCapacityDirty)
                UpdateHeatCapacity(protoMan);
            return _heatCapacity;
        }


        /// <summary>
        ///     Constructs an empty solution (ex. an empty beaker).
        /// </summary>
        public Solution() : this(2) // Most objects on the station hold only 1 or 2 reagents.
        {
        }

        /// <summary>
        ///     Constructs an empty solution (ex. an empty beaker).
        /// </summary>
        public Solution(int capacity)
        {
            Contents = new(capacity);
        }

        /// <summary>
        ///     Constructs a solution containing 100% of a reagent (ex. A beaker of pure water).
        /// </summary>
        /// <param name="reagentId">The prototype ID of the reagent to add.</param>
        /// <param name="quantity">The quantity in milli-units.</param>
        public Solution(string reagentId, FixedPoint2 quantity) : this()
        {
            AddReagent(reagentId, quantity);
        }

        public Solution(IEnumerable<ReagentQuantity> reagents, bool setMaxVol = true)
        {
            Contents = new(reagents);
            Volume = FixedPoint2.Zero;
            foreach (var reagent in Contents)
            {
                Volume += reagent.Quantity;
            }

            if (setMaxVol)
                MaxVolume = Volume;

            ValidateSolution();
        }

        public Solution(Solution solution)
        {
            Volume = solution.Volume;
            _heatCapacity = solution._heatCapacity;
            _heatCapacityDirty = solution._heatCapacityDirty;
            Contents = solution.Contents.ShallowClone();
            ValidateSolution();
        }

        public Solution Clone()
        {
            return new Solution(this);
        }


        [AssertionMethod]
        public void ValidateSolution()
        {
            // sandbox forbids: [Conditional("DEBUG")]
#if DEBUG
            // Correct volume
            DebugTools.Assert(Contents.Select(x => x.Quantity).Sum() == Volume);

            // All reagents have at least some reagent present.
            DebugTools.Assert(!Contents.Any(x => x.Quantity <= FixedPoint2.Zero));

            // No duplicate reagent iDs
            DebugTools.Assert(Contents.Select(x => x.ReagentId).ToHashSet().Count() == Contents.Count);

            // If it isn't flagged as dirty, check heat capacity is correct.
            if (!_heatCapacityDirty)
            {
                var cur = _heatCapacity;
                _heatCapacityDirty = true;
                UpdateHeatCapacity(null);
                DebugTools.Assert(MathHelper.CloseTo(_heatCapacity, cur));
            }
#endif
        }

        void ISerializationHooks.AfterDeserialization()
        {
            Volume = FixedPoint2.Zero;
            foreach (var reagent in Contents)
            {
                Volume += reagent.Quantity;
            }

            if (MaxVolume == FixedPoint2.Zero)
                MaxVolume = Volume;
        }

        public bool ContainsReagent(string reagentId)
        {
            foreach (var reagent in Contents)
            {
                if (reagent.ReagentId == reagentId)
                    return true;
            }

            return false;
        }

        public bool TryGetReagent(string reagentId, out FixedPoint2 quantity)
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

        public string? GetPrimaryReagentId()
        {
            if (Contents.Count == 0)
                return null;

            ReagentQuantity max = default;

            foreach (var reagent in Contents)
            {
                if (reagent.Quantity >= max.Quantity)
                {
                    max = reagent;
                }
            }

            return max.ReagentId!;
        }

        /// <summary>
        ///     Adds a given quantity of a reagent directly into the solution.
        /// </summary>
        /// <param name="reagentId">The prototype ID of the reagent to add.</param>
        /// <param name="quantity">The quantity in milli-units.</param>
        public void AddReagent(string reagentId, FixedPoint2 quantity, bool dirtyHeatCap = true)
        {
            if (quantity <= 0)
            {
                DebugTools.Assert(quantity == 0, "Attempted to add negative reagent quantity");
                return;
            }

            Volume += quantity;
            _heatCapacityDirty |= dirtyHeatCap;
            for (var i = 0; i < Contents.Count; i++)
            {
                var reagent = Contents[i];
                if (reagent.ReagentId != reagentId)
                    continue;

                Contents[i] = new ReagentQuantity(reagentId, reagent.Quantity + quantity);
                ValidateSolution();
                return;
            }

            Contents.Add(new ReagentQuantity(reagentId, quantity));
            ValidateSolution();
        }

        /// <summary>
        ///     Adds a given quantity of a reagent directly into the solution.
        /// </summary>
        /// <param name="proto">The prototype of the reagent to add.</param>
        /// <param name="quantity">The quantity in milli-units.</param>
        public void AddReagent(ReagentPrototype proto, FixedPoint2 quantity)
        {
            AddReagent(proto.ID, quantity, false);
            _heatCapacity += quantity.Float() * proto.SpecificHeat;
        }

        /// <summary>
        ///     Adds a given quantity of a reagent directly into the solution.
        /// </summary>
        /// <param name="proto">The prototype of the reagent to add.</param>
        /// <param name="quantity">The quantity in milli-units.</param>
        public void AddReagent(ReagentPrototype proto, FixedPoint2 quantity, float temperature, IPrototypeManager? protoMan)
        {
            if (_heatCapacityDirty)
                UpdateHeatCapacity(protoMan);

            var totalThermalEnergy = Temperature * _heatCapacity + temperature * proto.SpecificHeat;
            AddReagent(proto, quantity);
            Temperature = _heatCapacity == 0 ? 0 : totalThermalEnergy / _heatCapacity;
        }


        /// <summary>
        ///     Scales the amount of solution by some integer quantity.
        /// </summary>
        /// <param name="scale">The scalar to modify the solution by.</param>
        public void ScaleSolution(int scale)
        {
            if (scale == 1)
                return;

            if (scale <= 0)
            {
                RemoveAllSolution();
                return;
            }

            _heatCapacity *= scale;
            Volume *= scale;

            for (int i = 0; i < Contents.Count; i++)
            {
                var old = Contents[i];
                Contents[i] = new ReagentQuantity(old.ReagentId, old.Quantity * scale);
            }
            ValidateSolution();
        }

        /// <summary>
        ///     Scales the amount of solution.
        /// </summary>
        /// <param name="scale">The scalar to modify the solution by.</param>
        public void ScaleSolution(float scale)
        {
            if (scale == 1)
                return;

            if (scale == 0)
            {
                RemoveAllSolution();
                return;
            }

            Volume = FixedPoint2.Zero;
            for (int i = Contents.Count - 1; i >= 0; i--)
            {
                var old = Contents[i];
                var newQuantity = old.Quantity * scale;
                if (newQuantity == FixedPoint2.Zero)
                    Contents.RemoveSwap(i);
                else
                {
                    Contents[i] = new ReagentQuantity(old.ReagentId, newQuantity);
                    Volume += newQuantity;
                }
            }

            _heatCapacityDirty = true;
            ValidateSolution();
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
                var reagent = Contents[i];
                if (reagent.ReagentId == reagentId)
                    return reagent.Quantity;
            }

            return FixedPoint2.Zero;
        }

        /// <summary>
        ///     Attempts to remove an amount of reagent from the solution.
        /// </summary>
        /// <param name="reagentId">The reagent to be removed.</param>
        /// <param name="quantity">The amount of reagent to remove.</param>
        /// <returns>How much reagent was actually removed. Zero if the reagent is not present on the solution.</returns>
        public FixedPoint2 RemoveReagent(string reagentId, FixedPoint2 quantity)
        {
            if (quantity <= FixedPoint2.Zero)
                return FixedPoint2.Zero;

            for (var i = 0; i < Contents.Count; i++)
            {
                var reagent = Contents[i];

                if(reagent.ReagentId != reagentId)
                    continue;

                var curQuantity = reagent.Quantity;
                var newQuantity = curQuantity - quantity;
                _heatCapacityDirty = true;

                if (newQuantity <= 0)
                {
                    Contents.RemoveSwap(i);
                    Volume -= curQuantity;
                    ValidateSolution();
                    return curQuantity;
                }

                Contents[i] = new ReagentQuantity(reagentId, newQuantity);
                Volume -= quantity;
                ValidateSolution();
                return quantity;
            }

            // Reagent is not on the solution...
            return FixedPoint2.Zero;
        }

        public void RemoveAllSolution()
        {
            Contents.Clear();
            Volume = FixedPoint2.Zero;
            _heatCapacityDirty = false;
            _heatCapacity = 0;
        }

        public Solution SplitSolution(FixedPoint2 toTake)
        {
            if (toTake <= FixedPoint2.Zero)
                return new Solution();

            Solution newSolution;

            if (toTake >= Volume)
            {
                newSolution = Clone();
                RemoveAllSolution();
                return newSolution;
            }

            var origVol = Volume;
            var effVol = Volume.Value;
            newSolution = new Solution(Contents.Count) { Temperature = Temperature };
            var remaining = (long) toTake.Value;

            for (var i = Contents.Count - 1; i >= 0; i--) // iterate backwards because of remove swap.
            {
                var reagent = Contents[i];

                // This is set up such that integer rounding will tend to take more reagents.
                var split = remaining * reagent.Quantity.Value / effVol;

                if (split <= 0)
                {
                    effVol -= reagent.Quantity.Value;
                    DebugTools.Assert(split == 0, "Negative solution quantity while splitting? Long/int overflow?");
                    continue;
                }

                var splitQuantity = FixedPoint2.FromCents((int) split);
                var newQuantity = reagent.Quantity - splitQuantity;

                DebugTools.Assert(newQuantity >= 0);

                if (newQuantity > FixedPoint2.Zero)
                    Contents[i] = new ReagentQuantity(reagent.ReagentId, newQuantity);
                else
                    Contents.RemoveSwap(i);

                newSolution.Contents.Add(new ReagentQuantity(reagent.ReagentId, splitQuantity));
                Volume -= splitQuantity;
                remaining -= split;
                effVol -= reagent.Quantity.Value;
            }

            newSolution.Volume = origVol - Volume;

            DebugTools.Assert(remaining >= 0);
            DebugTools.Assert(remaining == 0 || Volume == FixedPoint2.Zero);

            _heatCapacityDirty = true;
            newSolution._heatCapacityDirty = true;

            ValidateSolution();
            newSolution.ValidateSolution();

            return newSolution;
        }

        /// <summary>
        /// Variant of <see cref="SplitSolution(FixedPoint2)"/> that doesn't return a new solution containing the removed reagents.
        /// </summary>
        /// <param name="toTake">The quantity of this solution to remove</param>
        public void RemoveSolution(FixedPoint2 toTake)
        {
            if (toTake <= FixedPoint2.Zero)
                return;

            if (toTake >= Volume)
            {
                RemoveAllSolution();
                return;
            }

            var effVol = Volume.Value;
            Volume -= toTake;
            var remaining = (long) toTake.Value;
            for (var i = Contents.Count - 1; i >= 0; i--)// iterate backwards because of remove swap.
            {
                var reagent = Contents[i];

                // This is set up such that integer rounding will tend to take more reagents.
                var split = remaining * reagent.Quantity.Value / effVol;

                if (split <= 0)
                {
                    effVol -= reagent.Quantity.Value;
                    DebugTools.Assert(split == 0, "Negative solution quantity while splitting? Long/int overflow?");
                    continue;
                }

                var splitQuantity = FixedPoint2.FromCents((int) split);
                var newQuantity = reagent.Quantity - splitQuantity;

                if (newQuantity > FixedPoint2.Zero)
                    Contents[i] = new ReagentQuantity(reagent.ReagentId, newQuantity);
                else
                    Contents.RemoveSwap(i);

                remaining -= split;
                effVol -= reagent.Quantity.Value;
            }

            DebugTools.Assert(remaining >= 0);
            DebugTools.Assert(remaining == 0 || Volume == FixedPoint2.Zero);

            _heatCapacityDirty = true;
            ValidateSolution();
        }

        public void AddSolution(Solution otherSolution, IPrototypeManager? protoMan)
        {
            if (otherSolution.Volume <= FixedPoint2.Zero)
                return;

            Volume += otherSolution.Volume;

            var closeTemps = MathHelper.CloseTo(otherSolution.Temperature, Temperature);
            float totalThermalEnergy = 0;
            if (!closeTemps)
            {
                IoCManager.Resolve(ref protoMan);

                if (_heatCapacityDirty)
                    UpdateHeatCapacity(protoMan);

                if (otherSolution._heatCapacityDirty)
                    otherSolution.UpdateHeatCapacity(protoMan);

                totalThermalEnergy = _heatCapacity * Temperature + otherSolution._heatCapacity * otherSolution.Temperature;
            }

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

            _heatCapacity += otherSolution._heatCapacity;
            if (closeTemps)
                _heatCapacityDirty |= otherSolution._heatCapacityDirty;
            else
                Temperature = _heatCapacity == 0 ? 0 : totalThermalEnergy / _heatCapacity;

            ValidateSolution();
        }

        public Color GetColor(IPrototypeManager? protoMan)
        {
            if (Volume == FixedPoint2.Zero)
            {
                return Color.Transparent;
            }

            IoCManager.Resolve(ref protoMan);

            Color mixColor = default;
            var runningTotalQuantity = FixedPoint2.New(0);
            bool first = true;

            foreach (var reagent in Contents)
            {
                runningTotalQuantity += reagent.Quantity;

                if (!protoMan.TryIndex(reagent.ReagentId, out ReagentPrototype? proto))
                {
                    continue;
                }

                if (first)
                {
                    first = false;
                    mixColor = proto.SubstanceColor;
                    continue;
                }

                var interpolateValue = reagent.Quantity.Float() / runningTotalQuantity.Float();
                mixColor = Color.InterpolateBetween(mixColor, proto.SubstanceColor, interpolateValue);
            }
            return mixColor;
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

        public void SetContents(IEnumerable<ReagentQuantity> reagents, bool setMaxVol = false)
        {
            RemoveAllSolution();
            _heatCapacityDirty = true;
            Contents = new(reagents);
            foreach (var reagent in Contents)
            {
                Volume += reagent.Quantity;
            }

            if (setMaxVol)
                MaxVolume = Volume;

            ValidateSolution();
        }

    }
}
