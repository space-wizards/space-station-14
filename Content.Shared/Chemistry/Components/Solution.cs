using System.Collections;
using System.Linq;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Chemistry.Components
{
    /// <summary>
    ///     A solution of reagents.
    /// </summary>
    [Serializable, NetSerializable]
    [DataDefinition]
    public sealed partial class Solution : IEnumerable<ReagentQuantity>, ISerializationHooks, IRobustCloneable<Solution>
    {
        // This is a list because it is actually faster to add and remove reagents from
        // a list than a dictionary, though contains-reagent checks are slightly slower,
        [DataField("reagents")]
        public List<ReagentQuantity> Contents;

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
        public FixedPoint2 MaxVolume { get; set; } = FixedPoint2.Zero;

        public float FillFraction => MaxVolume == 0 ? 1 : Volume.Float() / MaxVolume.Float();

        /// <summary>
        ///     If reactions will be checked for when adding reagents to the container.
        /// </summary>
        [DataField]
        public bool CanReact { get; set; } = true;

        /// <summary>
        ///     Volume needed to fill this container.
        /// </summary>
        [ViewVariables]
        public FixedPoint2 AvailableVolume => MaxVolume - Volume;

        /// <summary>
        ///     The temperature of the reagents in the solution.
        /// </summary>
        [DataField]
        public float Temperature { get; set; } = 293.15f;

        /// <summary>
        ///     The name of this solution, if it is contained in some <see cref="SolutionContainerManagerComponent"/>
        /// </summary>
        [DataField]
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
        [ViewVariables] private float _heatCapacity;

        /// <summary>
        ///     If true, then <see cref="_heatCapacity"/> needs to be recomputed.
        /// </summary>
        [ViewVariables] private bool _heatCapacityDirty = true;

        [ViewVariables(VVAccess.ReadWrite)]
        private int _heatCapacityUpdateCounter;

        // This value is arbitrary btw.
        private const int HeatCapacityUpdateInterval = 15;

        public void UpdateHeatCapacity(IPrototypeManager? protoMan)
        {
            IoCManager.Resolve(ref protoMan);
            DebugTools.Assert(_heatCapacityDirty);
            _heatCapacityDirty = false;
            _heatCapacity = 0;
            foreach (var (reagent, quantity) in Contents)
            {
                _heatCapacity += (float)quantity *
                                    protoMan.Index<ReagentPrototype>(reagent.Prototype).SpecificHeat;
            }

            _heatCapacityUpdateCounter = 0;
        }

        public float GetHeatCapacity(IPrototypeManager? protoMan)
        {
            if (_heatCapacityDirty)
                UpdateHeatCapacity(protoMan);
            return _heatCapacity;
        }

        public void CheckRecalculateHeatCapacity()
        {
            // For performance, we have a few ways for heat capacity to get modified without a full recalculation.
            // To avoid these drifting too much due to float error, we mark it as dirty after N such operations,
            // so it will be recalculated.
            if (++_heatCapacityUpdateCounter >= HeatCapacityUpdateInterval)
                _heatCapacityDirty = true;
        }

        public float GetThermalEnergy(IPrototypeManager? protoMan)
        {
            return GetHeatCapacity(protoMan) * Temperature;
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
        /// <param name="prototype">The prototype ID of the reagent to add.</param>
        /// <param name="quantity">The quantity in milli-units.</param>
        public Solution([ForbidLiteral] string prototype, FixedPoint2 quantity, List<ReagentData>? data = null) : this()
        {
            AddReagent(new ReagentId(prototype, data), quantity);
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
            Contents = solution.Contents.ShallowClone();
            Volume = solution.Volume;
            MaxVolume = solution.MaxVolume;
            Temperature = solution.Temperature;
            CanReact = solution.CanReact;
            _heatCapacity = solution._heatCapacity;
            _heatCapacityDirty = solution._heatCapacityDirty;
            _heatCapacityUpdateCounter = solution._heatCapacityUpdateCounter;
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

            // No duplicate reagents iDs
            DebugTools.Assert(Contents.Select(x => x.Reagent).ToHashSet().Count == Contents.Count);

            // If it isn't flagged as dirty, check heat capacity is correct.
            if (!_heatCapacityDirty)
            {
                var cur = _heatCapacity;
                _heatCapacityDirty = true;
                UpdateHeatCapacity(null);
                DebugTools.Assert(MathHelper.CloseTo(_heatCapacity, cur, tolerance: 0.01));
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

        public bool ContainsPrototype([ForbidLiteral] string prototype)
        {
            foreach (var (reagent, _) in Contents)
            {
                if (reagent.Prototype == prototype)
                    return true;
            }

            return false;
        }

        public bool ContainsReagent(ReagentId id)
        {
            foreach (var (reagent, _) in Contents)
            {
                if (reagent == id)
                    return true;
            }

            return false;
        }

        public bool ContainsReagent([ForbidLiteral] string reagentId, List<ReagentData>? data)
            => ContainsReagent(new(reagentId, data));

        public bool TryGetReagent(ReagentId id, out ReagentQuantity quantity)
        {
            foreach (var tuple in Contents)
            {
                if (tuple.Reagent != id)
                    continue;

                DebugTools.Assert(tuple.Quantity > FixedPoint2.Zero);
                quantity = tuple;
                return true;
            }

            quantity = new ReagentQuantity(id, FixedPoint2.Zero);
            return false;
        }

        public bool TryGetReagentQuantity(ReagentId id, out FixedPoint2 volume)
        {
            volume = FixedPoint2.Zero;
            if (!TryGetReagent(id, out var quant))
                return false;

            volume = quant.Quantity;
            return true;
        }

        [Pure]
        public ReagentQuantity GetReagent(ReagentId id)
        {
            TryGetReagent(id, out var quantity);
            return quantity;
        }

        public ReagentQuantity this[ReagentId id]
        {
            get
            {
                if (!TryGetReagent(id, out var quantity))
                    throw new KeyNotFoundException(id.ToString());
                return quantity;
            }
        }

        /// <summary>
        /// Get the volume/quantity of a single reagent in the solution.
        /// </summary>
        [Pure]
        public FixedPoint2 GetReagentQuantity(ReagentId id)
        {
            return GetReagent(id).Quantity;
        }

        /// <summary>
        /// Gets the total volume of all reagents in the solution with the given prototype Id.
        /// If you only want the volume of a single reagent, use <see cref="GetReagentQuantity"/>
        /// </summary>
        [Pure]
        public FixedPoint2 GetTotalPrototypeQuantity(params ProtoId<ReagentPrototype>[] prototypes)
        {
            var total = FixedPoint2.Zero;
            foreach (var (reagent, quantity) in Contents)
            {
                if (prototypes.Contains(reagent.Prototype))
                    total += quantity;
            }

            return total;
        }

        public FixedPoint2 GetTotalPrototypeQuantity(ProtoId<ReagentPrototype> id)
        {
            var total = FixedPoint2.Zero;
            foreach (var (reagent, quantity) in Contents)
            {
                if (id == reagent.Prototype)
                    total += quantity;
            }

            return total;
        }

        public ReagentId? GetPrimaryReagentId()
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

            return max.Reagent;
        }

        /// <summary>
        ///     Adds a given quantity of a reagent directly into the solution.
        /// </summary>
        /// <param name="prototype">The prototype ID of the reagent to add.</param>
        /// <param name="quantity">The quantity in milli-units.</param>
        public void AddReagent([ForbidLiteral] string prototype, FixedPoint2 quantity, bool dirtyHeatCap = true)
            => AddReagent(new ReagentId(prototype, null), quantity, dirtyHeatCap);

        /// <summary>
        ///     Adds a given quantity of a reagent directly into the solution.
        /// </summary>
        /// <param name="id">The reagent to add.</param>
        /// <param name="quantity">The quantity in milli-units.</param>
        public void AddReagent(ReagentId id, FixedPoint2 quantity, bool dirtyHeatCap = true)
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
                var (reagent, existingQuantity) = Contents[i];
                if (reagent != id)
                    continue;

                Contents[i] = new ReagentQuantity(id, existingQuantity + quantity);
                ValidateSolution();
                return;
            }

            Contents.Add(new ReagentQuantity(id, quantity));
            ValidateSolution();
        }

        /// <summary>
        ///     Adds a given quantity of a reagent directly into the solution.
        /// </summary>
        /// <param name="reagentId">The reagent to add.</param>
        /// <param name="quantity">The quantity in milli-units.</param>
        public void AddReagent(ReagentPrototype proto, ReagentId reagentId, FixedPoint2 quantity)
        {
            AddReagent(reagentId, quantity, false);

            _heatCapacity += quantity.Float() * proto.SpecificHeat;
            CheckRecalculateHeatCapacity();
        }

        public void AddReagent(ReagentQuantity reagentQuantity)
            => AddReagent(reagentQuantity.Reagent, reagentQuantity.Quantity);

        /// <summary>
        ///     Adds a given quantity of a reagent directly into the solution.
        /// </summary>
        /// <param name="proto">The prototype of the reagent to add.</param>
        /// <param name="quantity">The quantity in milli-units.</param>
        public void AddReagent(ReagentPrototype proto, FixedPoint2 quantity, float temperature, IPrototypeManager? protoMan, List<ReagentData>? data = null)
        {
            if (_heatCapacityDirty)
                UpdateHeatCapacity(protoMan);

            var totalThermalEnergy = Temperature * _heatCapacity + temperature * proto.SpecificHeat;
            AddReagent(new ReagentId(proto.ID, data), quantity);
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
            CheckRecalculateHeatCapacity();

            for (int i = 0; i < Contents.Count; i++)
            {
                var old = Contents[i];
                Contents[i] = new ReagentQuantity(old.Reagent, old.Quantity * scale);
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
                    Contents[i] = new ReagentQuantity(old.Reagent, newQuantity);
                    Volume += newQuantity;
                }
            }

            _heatCapacityDirty = true;
            ValidateSolution();
        }

        /// <summary>
        ///     Attempts to remove an amount of reagent from the solution.
        /// </summary>
        /// <param name="toRemove">The reagent to be removed.</param>
        /// <returns>How much reagent was actually removed. Zero if the reagent is not present on the solution.</returns>
        public FixedPoint2 RemoveReagent(ReagentQuantity toRemove, bool preserveOrder = false, bool ignoreReagentData = false)
        {
            if (toRemove.Quantity <= FixedPoint2.Zero)
                return FixedPoint2.Zero;

            List<int> reagentIndices = new List<int>();
            int totalRemoveVolume = 0;

            for (var i = 0; i < Contents.Count; i++)
            {
                var (reagent, quantity) = Contents[i];

                if (ignoreReagentData)
                {
                    if (reagent.Prototype != toRemove.Reagent.Prototype)
                        continue;
                }
                else
                {
                    if (reagent != toRemove.Reagent)
                        continue;
                }
                //We prepend instead of add to handle the Contents list back-to-front later down.
                //It makes RemoveSwap safe to use.
                totalRemoveVolume += quantity.Value;
                reagentIndices.Insert(0, i);
            }

            if (totalRemoveVolume <= 0)
            {
                // Reagent is not on the solution...
                return FixedPoint2.Zero;
            }

            FixedPoint2 removedQuantity = 0;
            for (var i = 0; i < reagentIndices.Count; i++)
            {
                var (reagent, curQuantity) = Contents[reagentIndices[i]];

                // This is set up such that integer rounding will tend to take more reagents.
                var split = ((long)toRemove.Quantity.Value) * curQuantity.Value / totalRemoveVolume;

                var splitQuantity = FixedPoint2.FromCents((int)split);

                var newQuantity = curQuantity - splitQuantity;
                _heatCapacityDirty = true;

                if (newQuantity <= 0)
                {
                    if (!preserveOrder)
                        Contents.RemoveSwap(reagentIndices[i]);
                    else
                        Contents.RemoveAt(reagentIndices[i]);

                    Volume -= curQuantity;
                    removedQuantity += curQuantity;
                    continue;
                }

                Contents[reagentIndices[i]] = new ReagentQuantity(reagent, newQuantity);
                Volume -= splitQuantity;
                removedQuantity += splitQuantity;
            }
            ValidateSolution();

            return removedQuantity;
        }

        /// <summary>
        ///     Attempts to remove an amount of reagent from the solution.
        /// </summary>
        /// <param name="prototype">The prototype of the reagent to be removed.</param>
        /// <param name="quantity">The amount of reagent to remove.</param>
        /// <returns>How much reagent was actually removed. Zero if the reagent is not present on the solution.</returns>
        public FixedPoint2 RemoveReagent(string prototype, FixedPoint2 quantity, List<ReagentData>? data = null, bool ignoreReagentData = false)
        {
            return RemoveReagent(new ReagentQuantity(prototype, quantity, data), ignoreReagentData: ignoreReagentData);
        }

        /// <summary>
        ///     Attempts to remove an amount of reagent from the solution.
        /// </summary>
        /// <param name="reagentId">The reagent to be removed.</param>
        /// <param name="quantity">The amount of reagent to remove.</param>
        /// <returns>How much reagent was actually removed. Zero if the reagent is not present on the solution.</returns>
        public FixedPoint2 RemoveReagent(ReagentId reagentId, FixedPoint2 quantity, bool preserveOrder = false, bool ignoreReagentData = false)
        {
            return RemoveReagent(new ReagentQuantity(reagentId, quantity), preserveOrder, ignoreReagentData);
        }

        public void RemoveAllSolution()
        {
            Contents.Clear();
            Volume = FixedPoint2.Zero;
            _heatCapacityDirty = false;
            _heatCapacity = 0;
        }

        /// <summary>
        /// Splits a solution without the specified reagent prototypes.
        /// </summary>
        [Obsolete("Use SplitSolutionWithout with params ProtoId<ReagentPrototype>")]
        public Solution SplitSolutionWithout(FixedPoint2 toTake, params string[] excludedPrototypes)
        {
            // First remove the blacklisted prototypes
            List<ReagentQuantity> excluded = new();
            foreach (var id in excludedPrototypes)
            {
                foreach (var tuple in Contents)
                {
                    if (tuple.Reagent.Prototype != id)
                        continue;

                    excluded.Add(tuple);
                    RemoveReagent(tuple);
                    break;
                }
            }

            // Then split the solution
            var sol = SplitSolution(toTake);

            // Then re-add the excluded reagents to the original solution.
            foreach (var reagent in excluded)
            {
                AddReagent(reagent);
            }

            return sol;
        }

        /// <summary>
        /// Splits a solution without the specified reagent prototypes.
        /// </summary>
        public Solution SplitSolutionWithout(FixedPoint2 toTake, params ProtoId<ReagentPrototype>[] excludedPrototypes)
        {
            // First remove the blacklisted prototypes
            List<ReagentQuantity> excluded = new();
            foreach (var id in excludedPrototypes)
            {
                foreach (var tuple in Contents)
                {
                    if (tuple.Reagent.Prototype != id)
                        continue;

                    excluded.Add(tuple);
                    RemoveReagent(tuple);
                    break;
                }
            }

            // Then split the solution
            var sol = SplitSolution(toTake);

            // Then re-add the excluded reagents to the original solution.
            foreach (var reagent in excluded)
            {
                AddReagent(reagent);
            }

            return sol;
        }

        /// <summary>
        /// Splits a solution with only the specified reagent prototypes.
        /// </summary>
        public Solution SplitSolutionWithOnly(FixedPoint2 toTake, params ProtoId<ReagentPrototype>[] includedPrototypes)
        {
            // First remove the non-included prototypes
            List<ReagentQuantity> excluded = new();
            for (var i = Contents.Count - 1; i >= 0; i--)
            {
                if (includedPrototypes.Contains(Contents[i].Reagent.Prototype))
                    continue;

                excluded.Add(Contents[i]);
                RemoveReagent(Contents[i]);
            }

            // Then split the solution
            var sol = SplitSolution(toTake);

            // Then re-add the excluded reagents to the original solution.
            foreach (var reagent in excluded)
            {
                AddReagent(reagent);
            }

            return sol;
        }

        /// <summary>
        /// Splits a solution into two by moving reagents from the given solution into a new one.
        /// This modifies the original solution.
        /// </summary>
        /// <param name="toTake">The quantity of this solution to remove.</param>
        /// <returns>A new solution containing the removed reagents.</returns>
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
            var remaining = (long)toTake.Value;

            for (var i = Contents.Count - 1; i >= 0; i--) // iterate backwards because of remove swap.
            {
                var (reagent, quantity) = Contents[i];

                // This is set up such that integer rounding will tend to take more reagents.
                var split = remaining * quantity.Value / effVol;

                if (split <= 0)
                {
                    effVol -= quantity.Value;
                    DebugTools.Assert(split == 0, "Negative solution quantity while splitting? Long/int overflow?");
                    continue;
                }

                var splitQuantity = FixedPoint2.FromCents((int)split);
                var newQuantity = quantity - splitQuantity;

                DebugTools.Assert(newQuantity >= 0);

                if (newQuantity > FixedPoint2.Zero)
                    Contents[i] = new ReagentQuantity(reagent, newQuantity);
                else
                    Contents.RemoveSwap(i);

                newSolution.Contents.Add(new ReagentQuantity(reagent, splitQuantity));
                Volume -= splitQuantity;
                remaining -= split;
                effVol -= quantity.Value;
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
            var remaining = (long)toTake.Value;
            for (var i = Contents.Count - 1; i >= 0; i--)// iterate backwards because of remove swap.
            {
                var (reagent, quantity) = Contents[i];

                // This is set up such that integer rounding will tend to take more reagents.
                var split = remaining * quantity.Value / effVol;

                if (split <= 0)
                {
                    effVol -= quantity.Value;
                    DebugTools.Assert(split == 0, "Negative solution quantity while splitting? Long/int overflow?");
                    continue;
                }

                var splitQuantity = FixedPoint2.FromCents((int)split);
                var newQuantity = quantity - splitQuantity;

                if (newQuantity > FixedPoint2.Zero)
                    Contents[i] = new ReagentQuantity(reagent, newQuantity);
                else
                    Contents.RemoveSwap(i);

                remaining -= split;
                effVol -= quantity.Value;
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
                var (otherReagent, otherQuantity) = otherSolution.Contents[i];

                var found = false;
                for (var j = 0; j < Contents.Count; j++)
                {
                    var (reagent, quantity) = Contents[j];
                    if (reagent == otherReagent)
                    {
                        found = true;
                        Contents[j] = new ReagentQuantity(reagent, quantity + otherQuantity);
                        break;
                    }
                }

                if (!found)
                {
                    Contents.Add(new ReagentQuantity(otherReagent, otherQuantity));
                }
            }

            _heatCapacity += otherSolution._heatCapacity;
            CheckRecalculateHeatCapacity();
            if (closeTemps)
                _heatCapacityDirty |= otherSolution._heatCapacityDirty;
            else
                Temperature = _heatCapacity == 0 ? 0 : totalThermalEnergy / _heatCapacity;

            ValidateSolution();
        }

        public Color GetColorWithout(IPrototypeManager? protoMan, params ProtoId<ReagentPrototype>[] without)
        {
            if (Volume == FixedPoint2.Zero)
            {
                return Color.Transparent;
            }

            IoCManager.Resolve(ref protoMan);

            Color mixColor = default;
            var runningTotalQuantity = FixedPoint2.New(0);
            bool first = true;

            foreach (var (reagent, quantity) in Contents)
            {
                if (without.Contains(reagent.Prototype))
                    continue;

                runningTotalQuantity += quantity;

                if (!protoMan.TryIndex(reagent.Prototype, out ReagentPrototype? proto))
                {
                    continue;
                }

                if (first)
                {
                    first = false;
                    mixColor = proto.SubstanceColor;
                    continue;
                }

                var interpolateValue = quantity.Float() / runningTotalQuantity.Float();
                mixColor = Color.InterpolateBetween(mixColor, proto.SubstanceColor, interpolateValue);
            }
            return mixColor;
        }

        public Color GetColor(IPrototypeManager? protoMan)
        {
            return GetColorWithout(protoMan);
        }

        public Color GetColorWithOnly(IPrototypeManager? protoMan, params ProtoId<ReagentPrototype>[] included)
        {
            if (Volume == FixedPoint2.Zero)
            {
                return Color.Transparent;
            }

            IoCManager.Resolve(ref protoMan);

            Color mixColor = default;
            var runningTotalQuantity = FixedPoint2.New(0);
            bool first = true;

            foreach (var (reagent, quantity) in Contents)
            {
                if (!included.Contains(reagent.Prototype))
                    continue;

                runningTotalQuantity += quantity;

                if (!protoMan.TryIndex(reagent.Prototype, out ReagentPrototype? proto))
                {
                    continue;
                }

                if (first)
                {
                    first = false;
                    mixColor = proto.SubstanceColor;
                    continue;
                }

                var interpolateValue = quantity.Float() / runningTotalQuantity.Float();
                mixColor = Color.InterpolateBetween(mixColor, proto.SubstanceColor, interpolateValue);
            }
            return mixColor;
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
            Volume = 0;
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

        public Dictionary<ReagentPrototype, FixedPoint2> GetReagentPrototypes(IPrototypeManager protoMan)
        {
            var dict = new Dictionary<ReagentPrototype, FixedPoint2>(Contents.Count);
            foreach (var (reagent, quantity) in Contents)
            {
                var proto = protoMan.Index<ReagentPrototype>(reagent.Prototype);
                dict[proto] = quantity + dict.GetValueOrDefault(proto);
            }
            return dict;
        }
    }
}
