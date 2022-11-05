using Content.Shared.Atmos;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;
using Robust.Shared.Utility;
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
        public Solution(string reagentId, FixedPoint2 quantity)
        {
            AddReagent(reagentId, quantity);
        }

        void ISerializationHooks.AfterDeserialization()
        {
            // TODO solution serializer
            CurrentVolume = Contents.Values.Sum();
            if (MaxVolume == FixedPoint2.Zero)
                MaxVolume = CurrentVolume;
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
        ///     Adds a given quantity of a reagent directly into the solution. Does not modify the temperature of the
        ///     solution.
        /// </summary>
        public FixedPoint2 AddReagent(ReagentPrototype proto, FixedPoint2 quantity) => AddReagent(proto.ID, quantity);

        /// <summary>
        ///     Adds a given quantity of a reagent directly into the solution. Does not modify the temperature of the
        ///     solution.
        /// </summary>
        public FixedPoint2 AddReagent(string reagentId, FixedPoint2 quantity)
        {
            if (quantity <= 0)
                return;

            quantity = FixedPoint2.Min(quantity, AvailableVolume);

            DebugTools.Assert(IoCManager.Resolve<IPrototypeManager>().HasIndex<ReagentPrototype>(reagentId));

            Contents[reagentId] = Contents.TryGetValue(reagentId, out var existing)
                ? quantity + existing
                : quantity;

            CurrentVolume += quantity;
            _heatCapacityDirty = true;
            DebugTools.Assert(CurrentVolume == Contents.Values.Sum());
            return quantity;
        }

        /// <summary>
        ///     Adds a quantity of a reagent at some specific temperature directly into the solution.
        /// </summary>
        public FixedPoint2 AddReagent(string reagentId, FixedPoint2 quantity, float temperature, IPrototypeManager? protoMan)
        {
            IoCManager.Resolve(ref protoMan);
            return AddReagent(protoMan.Index<ReagentPrototype>(reagentId), quantity, temperature, protoMan);
        }

        /// <summary>
        ///     Adds a quantity of a reagent at some specific temperature directly into the solution.
        /// </summary>
        public FixedPoint2 AddReagent(ReagentPrototype proto, FixedPoint2 quantity, float temperature, IPrototypeManager? protoMan)
        {
            if (quantity <= 0)
                return FixedPoint2.Zero;

            IoCManager.Resolve(ref protoMan);

            var termalEnergy = Temperature * GetHeatCapacity(protoMan);
            quantity = AddReagent(proto.ID, quantity);

            var addedHeatCap = (float) quantity * proto.SpecificHeat;
            termalEnergy += addedHeatCap * temperature;
            _heatCapacity += addedHeatCap;
            _heatCapacityDirty = false;
            Temperature = termalEnergy / _heatCapacity;
            return quantity;

        }

        /// <summary>
        ///     Scales the amount of solution. Currently only supports solutions that have no maximum volume.
        /// </summary>
        /// <param name="scale">The scalar to modify the solution by.</param>
        public void ScaleSolution(float scale)
        {
            DebugTools.AssertNull(MaxVolume);

            if (scale.Equals(1f))
                return;

            _heatCapacityDirty = true;
            CurrentVolume = 0;

            foreach(var (id, quantity) in Contents)
            {
                var newQuant = quantity * scale;
                Contents[id] = newQuant;
                CurrentVolume += newQuant;
            }
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
        public FixedPoint2 RemoveReagent(string reagentId, FixedPoint2 quantity)
        {
            if(quantity <= 0 || !Contents.TryGetValue(reagentId, out var existing))
                return FixedPoint2.Zero;

            if (quantity >= existing)
            {
                Contents.Remove(reagentId);
                CurrentVolume -= existing;
                return existing;
            }

            Contents[reagentId] = existing - quantity; ;
            CurrentVolume -= quantity;
            _heatCapacityDirty = true;
            return quantity;
        }

        /// <summary>
        ///     Attempts to remove an amount of reagent from the solution.
        /// </summary>
        /// <param name="reagentId">The reagent to be removed.</param>
        /// <param name="quantity">The amount of reagent to remove.</param>
        /// <returns>How much reagent was actually removed. Zero if the reagent is not present on the solution.</returns>
        public FixedPoint2 RemoveReagent(ReagentPrototype proto, FixedPoint2 quantity)
        {
            var rem = RemoveReagent(proto.ID, quantity);
            if (rem > 0)
            {
                _heatCapacityDirty = false;
                _heatCapacity -= rem.Float() * proto.SpecificHeat;
            }

            return rem;
        }

        /// <summary>
        /// Remove the specified quantity from this solution.
        /// </summary>
        /// <param name="toTake">The quantity of this solution to remove</param>
        public void RemoveSolution(FixedPoint2 toTake, IPrototypeManager? protoMan = null)
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

            if (protoMan != null)
                GetHeatCapacity(protoMan);
            else
                _heatCapacityDirty = true;
        }

        public void RemoveAllSolution()
        {
            Contents.Clear();
            CurrentVolume = FixedPoint2.New(0);
            _heatCapacityDirty = false;
            _heatCapacity = 0;
        }

        public Solution SplitSolution(FixedPoint2 toTake, IPrototypeManager? protoMan = null)
        {
            if (toTake <= 0)
                return new Solution();

            Solution newSolution;
            if (toTake >= CurrentVolume)
            {
                newSolution = Clone();
                RemoveAllSolution();
                return newSolution;
            }

            newSolution = new()
            {
                Contents = new(Contents.Count),
                _heatCapacityDirty = true,
                Temperature = Temperature
            };

            _heatCapacityDirty = true;
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

            if (protoMan != null)
            {
                GetHeatCapacity(protoMan);
                newSolution.GetHeatCapacity(protoMan);
            }

            return newSolution;
        }

        /// <summary>
        ///     Add another solution to this one. This ignores max-volume.
        /// </summary>
        public void AddSolution(Solution otherSolution, IPrototypeManager? protoMan = null)
        {
            if (otherSolution.CurrentVolume <= FixedPoint2.Zero)
                return;

            var totalThermalEnergy = GetThermalEnergy(protoMan) + otherSolution.GetThermalEnergy(protoMan);
            _heatCapacity = _heatCapacity + otherSolution._heatCapacity;

            CurrentVolume += otherSolution.CurrentVolume;

            foreach (var (id, quantity) in otherSolution.Contents)
            {
                Contents[id] = Contents.TryGetValue(id, out var existing)
                    ? quantity + existing
                    : quantity;
            }

            CurrentVolume += otherSolution.CurrentVolume;
            Temperature = totalThermalEnergy / _heatCapacity;
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
                _heatCapacity = _heatCapacity,
                _heatCapacityDirty = _heatCapacityDirty, // CBF making this require IPrototypeManager;
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
