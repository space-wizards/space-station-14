using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using System.Collections;
using System.Linq;

namespace Content.Shared.Chemistry.Components
{
    /// <summary>
    ///     A solution of reagents.
    /// </summary>
    [Serializable, NetSerializable]
    [DataDefinition]
    public sealed partial class Solution : IEnumerable<ReagentQuantity>, ISerializationHooks
    {
        // This is a list because it is actually faster to add and remove reagents from
        // a list than a dictionary, though contains-reagent checks are slightly slower,
        [DataField("reagents")]
        public List<ReagentQuantity> Contents;

        public List<ReagentProprieties> ReagentCache = new();

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
        ///     If contents should boil instantly.
        ///     True when theres enough latent heat as to boil all of the LowestBoilingPointReagent stored at once.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool ShouldBoilInstantly = false;

        /// <summary>
        ///     If contents should freeze instantly.
        ///     True when theres enough latent heat as to freeze all of the HighestMeltingPointReagent stored at once.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool ShouldFreezeInstantly = false;

        /// <summary>
        ///     Latent heat of the solution stored to facilitate boiling reactions.
        ///     Only so much reagent can boil as there is extra heat.
        /// </summary
        [ViewVariables] 
        public float BoilingLatentHeat = 0;

        /// <summary>
        ///     Latent heat of the solution stored to facilitate freezing reactions.
        ///     Only so much reagent can freeze as there is heat missing.
        /// </summary
        [ViewVariables] 
        public float MeltingLatentHeat = 0;

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
        ///     The total heat capacity of all reagents in the solution.
        /// </summary>
        [ViewVariables] 
        public float HeatCapacity {get; private set;} = 0f;

        /// <summary>
        ///     Stores the reagent with the lowest boiling point for validating the temperature
        ///     and processing how much latent heat the solution should have.
        /// </summary
        [ViewVariables]
        private int? LowestBoilingPointReagent = null;

        /// <summary>
        ///     Stores the reagent with the highest melting point for validating the temperature
        ///     and processing how much latent heat the solution should have.
        /// </summary
        [ViewVariables]
        private int? HighestMeltingPointReagent = null;

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
        ///     Updates the heat capacity after adding or taking from the solution.
        /// </summary
        public void UpdateHeatCapacity()
        {
            HeatCapacity = 0;
            for(var i = 0; i < ReagentCache.Count; i++)
                HeatCapacity += (float)Contents[i].Quantity * ReagentCache[i].SpecificHeat;
        }

        /// <summary>
        ///     Returns the total thermal energy the solution holds.
        /// </summary
        public float GetThermalEnergy()
        {
            return HeatCapacity * Temperature;
        }

        /// <summary>
        ///     Sets the temperature of the solution.
        /// </summary
        public void SetAbsoluteTemperature(float temperature)
        {
            Temperature = temperature;
        }

        /// <summary>
        ///     Sets the temperature of the solution according to the amount of heat it should have.
        /// </summary
        public void SetTemperature(float thermalEnergy)
        {
            if(HeatCapacity == 0)
                return;
            Temperature = thermalEnergy / HeatCapacity; 
            ValidateTemperature();
        }

        /// <summary>
        ///     Adjusts the temperature of the solution according the the amount of heat its given or taken.
        /// </summary
        public void AdjustTemperature(float thermalEnergy)
        {
            if(HeatCapacity == 0)
                return;

            if(thermalEnergy > 0)
            {
                MeltingLatentHeat = 0;
                ShouldFreezeInstantly = false;
            }
            else
            {
                BoilingLatentHeat = 0;
                ShouldBoilInstantly = false;
            }

            Temperature += thermalEnergy / HeatCapacity;
            ValidateTemperature();
        }

        /// <summary>
        ///     Checks if the solution temperature doesn't surpass the melting and boiling points of the lowest reagent.
        ///     E.g. As long as theres water in the solution it will not surpass 373.15K
        /// </summary
        public void ValidateTemperature()
        {
            if(LowestBoilingPointReagent == null)
                return;

            var boilingReagent = ReagentCache[LowestBoilingPointReagent.Value];
            if(Temperature > boilingReagent.BoilingPoint)
            {
                // Checks how much heat past the boiling point the solution has...
                var excessThermalEnergy = Temperature * HeatCapacity - boilingReagent.BoilingPoint * HeatCapacity;
                var quantity = Contents[LowestBoilingPointReagent.Value];
                // Maximum amount of energy that can be used to boil all of the reagent the solution contains.
                // Exists in case someone pours a bucketful of lava into a glass of water
                var maxLatentHeat = ((float)quantity.Quantity) * boilingReagent.BoilingLatentHeat;
                // Stores it in latent heat
                BoilingLatentHeat += excessThermalEnergy;
                if(BoilingLatentHeat > maxLatentHeat)
                {
                    ShouldBoilInstantly = true;
                    BoilingLatentHeat = maxLatentHeat;
                }
                else
                {
                    ShouldBoilInstantly = false;
                }
                // Subtracts the excess energy from the temperature so it doesn't surpass the boiling point.
                Temperature += -excessThermalEnergy / HeatCapacity;
            }
            
            if(HighestMeltingPointReagent == null)
                return;
            
            var meltingReagent = ReagentCache[HighestMeltingPointReagent.Value];
            if(Temperature < meltingReagent.MeltingPoint)
            {
                // Exact same logic as above but for cold.
                var excessThermalEnergy = meltingReagent.MeltingPoint * HeatCapacity - Temperature * HeatCapacity;
                var quantity = Contents[HighestMeltingPointReagent.Value];
                var maxLatentHeat = ((float)quantity.Quantity) * meltingReagent.MeltingLatentHeat;
                MeltingLatentHeat += excessThermalEnergy;
                if(MeltingLatentHeat >= maxLatentHeat)
                {
                    ShouldFreezeInstantly = true;
                    MeltingLatentHeat = maxLatentHeat;
                }
                else
                {
                    ShouldFreezeInstantly = true;
                }
                Temperature += excessThermalEnergy / HeatCapacity;
            }
        }

        /// <summary>
        ///     Finds the reagents in the solution that contain the lowest boiling point and highest melting point.
        ///     Used to validate the temperature and for phase transition reactions.
        /// </summary>
        private void UpdatePhaseTransitionReagents()
        {
            var curLowestBoilingPoint = float.PositiveInfinity;
            var curHighestMeltingPoint = float.NegativeInfinity;

            LowestBoilingPointReagent = null;
            HighestMeltingPointReagent = null;

            for(var i = 0; i < ReagentCache.Count; i++)
            {
                if(ReagentCache[i].BoilingPoint <= curLowestBoilingPoint)
                    LowestBoilingPointReagent = i;
                if(ReagentCache[i].MeltingPoint >= curHighestMeltingPoint)
                    HighestMeltingPointReagent = i;
            }
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
        public Solution(string prototype, FixedPoint2 quantity, IPrototypeManager protoMan, ReagentData? data = null) : this()
        {
            AddReagent(new ReagentId(prototype, data), quantity, protoMan);
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
            
            UpdateHeatCapacity();
            UpdatePhaseTransitionReagents();
            ValidateSolution();
        }

        public Solution(Solution solution)
        {
            Volume = solution.Volume;
            Temperature = solution.Temperature;
            Contents = solution.Contents.ShallowClone();
            ReagentCache = solution.ReagentCache.ShallowClone();
            LowestBoilingPointReagent = solution.LowestBoilingPointReagent;
            HighestMeltingPointReagent = solution.HighestMeltingPointReagent;
            UpdateHeatCapacity();
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
            DebugTools.Assert(Contents.Count == ReagentCache.Count);
            // Correct volume
            DebugTools.Assert(Contents.Select(x => x.Quantity).Sum() == Volume);

            // All reagents have at least some reagent present.
            DebugTools.Assert(!Contents.Any(x => x.Quantity <= FixedPoint2.Zero));

            // No duplicate reagents iDs
            DebugTools.Assert(Contents.Select(x => x.Reagent).ToHashSet().Count == Contents.Count);

            // If it isn't flagged as dirty, check heat capacity is correct.
            var cur = HeatCapacity;
            UpdateHeatCapacity();
            DebugTools.Assert(MathHelper.CloseTo(HeatCapacity, cur));
#endif
        }

        void ISerializationHooks.AfterDeserialization()
        {
            // I don't know what the hell I'm doing here.
            var protoMan = IoCManager.Resolve<IPrototypeManager>();

            Volume = FixedPoint2.Zero;
            foreach (var reagent in Contents)
            {
                Volume += reagent.Quantity;
                var proto = protoMan.Index<ReagentPrototype>(reagent.Reagent.Prototype);
                ReagentCache.Add(
                    new ReagentProprieties(
                        proto.SpecificHeat, 
                        proto.BoilingPoint, 
                        proto.MeltingPoint,
                        proto.BoilingLatentHeat,
                        proto.MeltingLatentHeat));
            }

            if (MaxVolume == FixedPoint2.Zero)
                MaxVolume = Volume;

            UpdateHeatCapacity();
            UpdatePhaseTransitionReagents();
        }

        public bool ContainsPrototype(string prototype)
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

        public bool ContainsReagent(string reagentId, ReagentData? data)
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
        public FixedPoint2 GetTotalPrototypeQuantity(params string[] prototypes)
        {
            var total = FixedPoint2.Zero;
            foreach (var (reagent, quantity) in Contents)
            {
                if (prototypes.Contains(reagent.Prototype))
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
        ///     Adds a reagent to a solution.
        /// <param name="id">The reagent to add.</param>
        /// <param name="quantity">How much to add</param>
        /// <param name="protoMan">Prototype manager, optional.</param>
        /// <param name="temperature">Temperature of the reagent, optional.</param>
        /// </summary>
        public void AddReagent(ReagentId id, FixedPoint2 quantity, IPrototypeManager protoMan, float? temperature = null)
        {
            if(temperature == null)
                temperature = Temperature;

            if(quantity <= 0)
                return;

            var foundReagent = false;
            int reagentIndex = 0;
            for (var i = 0; i < Contents.Count; i++)
            {
                var (reagent, existingQuantity) = Contents[i];
                if (reagent != id)
                    continue;

                Contents[i] = new ReagentQuantity(id, existingQuantity + quantity);
                reagentIndex = i;
                foundReagent = true;
                break;
            }
            if(!foundReagent)
            {
                Contents.Add(new ReagentQuantity(id, quantity));
                var proto = protoMan.Index<ReagentPrototype>(id.Prototype);
                ReagentCache.Add(
                    new ReagentProprieties(
                        proto.SpecificHeat, 
                        proto.BoilingPoint, 
                        proto.MeltingPoint,
                        proto.BoilingLatentHeat,
                        proto.MeltingLatentHeat));
                reagentIndex = ReagentCache.Count - 1;
                UpdatePhaseTransitionReagents();
            }
            
            // Update Volume.
            Volume += quantity;
            //Adjust temperature and validate it if needed.
            if(!foundReagent && temperature != null)
            {
                float initialThermalEnergy = HeatCapacity * Temperature;
                float addedThermalEnergy = ReagentCache[reagentIndex].SpecificHeat * (float)quantity * temperature.Value;

                // Update Heat Capacity.
                HeatCapacity += ReagentCache[reagentIndex].SpecificHeat * (float)quantity;
                SetTemperature(initialThermalEnergy + addedThermalEnergy);
                ValidateSolution();
                return;
            }
            //If updating temperature was not needed:
            // Update Heat Capacity.
            HeatCapacity += ReagentCache[reagentIndex].SpecificHeat * (float)quantity;
            ValidateSolution();

        }

        /// <summary>
        ///     Adds a reagent to a solution.
        /// <param name="reagent">The reagent to add.</param>
        /// <param name="protoMan">Prototype manager, optional.</param>
        /// <param name="temperature">Temperature of the reagent, optional.</param>
        /// </summary>
        public void AddReagent(ReagentQuantity reagent, IPrototypeManager protoMan, float? temperature = null)
            => AddReagent(reagent.Reagent, reagent.Quantity, protoMan, temperature);

        /// <summary>
        ///     Adds a reagent to a solution.
        /// <param name="reagent">The reagent to add.</param>
        /// <param name="protoMan">Prototype manager, optional.</param>
        /// <param name="temperature">Temperature of the reagent, optional.</param>
        /// </summary>
        public void AddReagent(string prototype, FixedPoint2 quantity, IPrototypeManager protoMan, float? temperature = null)
            => AddReagent(new ReagentId(prototype, null), quantity, protoMan, temperature);

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

            HeatCapacity *= scale;
            ValidateSolution();
        }

        /// <summary>
        ///     Attempts to remove an amount of reagent from the solution.
        /// </summary>
        /// <param name="toRemove">The reagent to be removed.</param>
        /// <returns>How much reagent was actually removed. Zero if the reagent is not present on the solution.</returns>
        public FixedPoint2 RemoveReagent(ReagentQuantity toRemove)
        {
            if (toRemove.Quantity <= FixedPoint2.Zero)
                return FixedPoint2.Zero;

            for (var i = 0; i < Contents.Count; i++)
            {
                var (reagent, curQuantity) = Contents[i];

                if(reagent != toRemove.Reagent)
                    continue;

                var newQuantity = curQuantity - toRemove.Quantity;

                if (newQuantity <= 0)
                {
                    Volume -= curQuantity;
                    Contents.RemoveSwap(i);
                    ReagentCache.RemoveSwap(i);
                    if(LowestBoilingPointReagent == i || HighestMeltingPointReagent == i)
                        UpdatePhaseTransitionReagents();
                    UpdateHeatCapacity();
                    ValidateSolution();
                    return curQuantity;
                }
                Volume -= toRemove.Quantity;
                Contents[i] = new ReagentQuantity(reagent, newQuantity);
                UpdateHeatCapacity();
                ValidateSolution();
                return toRemove.Quantity;
            }

            // Reagent is not on the solution...
            return FixedPoint2.Zero;
        }

        /// <summary>
        ///     Attempts to remove an amount of reagent from the solution.
        /// </summary>
        /// <param name="prototype">The prototype of the reagent to be removed.</param>
        /// <param name="quantity">The amount of reagent to remove.</param>
        /// <returns>How much reagent was actually removed. Zero if the reagent is not present on the solution.</returns>
        public FixedPoint2 RemoveReagent(string prototype, FixedPoint2 quantity, ReagentData? data = null)
        {
            return RemoveReagent(new ReagentQuantity(prototype, quantity, data));
        }

        /// <summary>
        ///     Attempts to remove an amount of reagent from the solution.
        /// </summary>
        /// <param name="reagentId">The reagent to be removed.</param>
        /// <param name="quantity">The amount of reagent to remove.</param>
        /// <returns>How much reagent was actually removed. Zero if the reagent is not present on the solution.</returns>
        public FixedPoint2 RemoveReagent(ReagentId reagentId, FixedPoint2 quantity)
        {
            return RemoveReagent(new ReagentQuantity(reagentId, quantity));
        }

        public void RemoveAllSolution()
        {
            Contents.Clear();
            ReagentCache.Clear();
            Volume = FixedPoint2.Zero;
            HeatCapacity = 0;
            LowestBoilingPointReagent = null;
            HighestMeltingPointReagent = null;
            BoilingLatentHeat = 0;
            MeltingLatentHeat = 0;
        }

        /// <summary>
        /// Splits a solution without the specified reagent prototypes.
        /// </summary>
        public Solution SplitSolutionWithout(FixedPoint2 toTake, params string[] excludedPrototypes)
        {
            List<ReagentQuantity> removedQuantities = new();
            List<ReagentProprieties> removedProprieties = new();

            foreach(var id in excludedPrototypes)
            {
                for(int i  = 0; i < Contents.Count; i++)
                {
                    if(Contents[i].Reagent.Prototype == id)
                    {
                        removedQuantities.Add(Contents[i]);
                        removedProprieties.Add(ReagentCache[i]);
                        Contents.RemoveSwap(i);
                        ReagentCache.RemoveSwap(i);
                        break;
                    }
                }
            }

            Volume = 0;
            foreach(var (reagent, quantity) in Contents)
                Volume += quantity;

            var solution = SplitSolution(toTake, false);

            for(int i  = 0; i < removedQuantities.Count; i++)
            {
                Contents.Add(removedQuantities[i]);
                ReagentCache.Add(removedProprieties[i]);
            }

            Volume = 0;
            foreach(var (reagent, quantity) in Contents)
                Volume += quantity;

            UpdateHeatCapacity();
            UpdatePhaseTransitionReagents();
            ValidateSolution();

            return solution;
        }

        /// <summary>
        /// Splits a solution with only the specified reagent prototypes.
        /// </summary>
        public Solution SplitSolutionWithOnly(FixedPoint2 toTake, params string[] includedPrototypes)
        {
            List<ReagentQuantity> removedQuantities = new();
            List<ReagentProprieties> removedProprieties = new();

            foreach(var id in includedPrototypes)
            {
                for(int i  = 0; i < Contents.Count; i++)
                {
                    if(Contents[i].Reagent.Prototype != id)
                    {
                        removedQuantities.Add(Contents[i]);
                        removedProprieties.Add(ReagentCache[i]);
                        Contents.RemoveSwap(i);
                        ReagentCache.RemoveSwap(i);
                        break;
                    }
                }
            }

            Volume = 0;
            foreach(var (reagent, quantity) in Contents)
                Volume += quantity;

            var solution = SplitSolution(toTake, false);

            for(int i  = 0; i < removedQuantities.Count; i++)
            {
                Contents.Add(removedQuantities[i]);
                ReagentCache.Add(removedProprieties[i]);
            }

            Volume = 0;
            foreach(var (reagent, quantity) in Contents)
                Volume += quantity;

            UpdateHeatCapacity();
            UpdatePhaseTransitionReagents();
            ValidateSolution();

            return solution;
        }

        /// <summary>
        ///     Splits the solution taking a proportional quantity of each reagent.
        /// </summary>
        public Solution SplitSolution(FixedPoint2 toTake, bool updateCaches = true)
        {

            if(toTake <= FixedPoint2.Zero)
                return new Solution();

            Solution newSolution = new Solution();

            if (toTake >= Volume)
            {
                newSolution = Clone();
                RemoveAllSolution();
                return newSolution;
            }

            // The proportion of how much of each reagent is being taken.
            var splitRatio = toTake / Volume;
            bool reagentRemoved = false;
            var remainingToTake = toTake;

            for(var i = Contents.Count - 1; i>= 0; i--)
            {
                var(reagent, quantity) = Contents[i];
                var proprieties = ReagentCache[i];
                var removedQuantity = quantity * splitRatio;

                if(removedQuantity <= FixedPoint2.Zero)
                {
                    DebugTools.Assert(removedQuantity == 0, "Negative solution quantity while splitting? Long/int overflow?");
                    continue;
                }

                if(removedQuantity >= quantity)
                {
                    removedQuantity = quantity;
                    Contents.RemoveSwap(i);
                    ReagentCache.RemoveSwap(i);
                    reagentRemoved = true;
                }
                else
                {
                    Contents[i] = new ReagentQuantity(reagent, quantity - removedQuantity);
                }

                Volume -= removedQuantity;
                remainingToTake -= removedQuantity;

                newSolution.Contents.Add(new ReagentQuantity(reagent, removedQuantity));
                newSolution.ReagentCache.Add(proprieties);
                newSolution.Volume += removedQuantity;

            }

            DebugTools.Assert(remainingToTake >= 0 || remainingToTake == FixedPoint2.Zero);

            newSolution.Temperature = Temperature;
            
            reagentRemoved &= updateCaches;
            if(reagentRemoved)
            {
                UpdateHeatCapacity();
                UpdatePhaseTransitionReagents();
                ValidateSolution();
            }

            newSolution.UpdateHeatCapacity();
            newSolution.UpdatePhaseTransitionReagents();

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
                var (reagent, quantity) = Contents[i];

                // This is set up such that integer rounding will tend to take more reagents.
                var split = remaining * quantity.Value / effVol;

                if (split <= 0)
                {
                    effVol -= quantity.Value;
                    DebugTools.Assert(split == 0, "Negative solution quantity while splitting? Long/int overflow?");
                    continue;
                }

                var splitQuantity = FixedPoint2.FromCents((int) split);
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

            UpdateHeatCapacity();
            ValidateSolution();
        }

        public void AddSolution(Solution otherSolution, IPrototypeManager protoMan)
        {
            if (otherSolution.Volume <= FixedPoint2.Zero)
                return;

            var totalThermalEnergy = GetThermalEnergy() + otherSolution.GetThermalEnergy();

            Volume += otherSolution.Volume;

            var addedReagents = false;
            for (var i = 0; i < otherSolution.Contents.Count; i++)
            {
                var (otherReagent, otherQuantity) = otherSolution.Contents[i];
                var proprieties = otherSolution.ReagentCache[i];

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
                    addedReagents = true;
                    Contents.Add(new ReagentQuantity(otherReagent, otherQuantity));
                    ReagentCache.Add(proprieties);
                }
            }

            if(addedReagents)
                UpdatePhaseTransitionReagents();
            
            UpdateHeatCapacity();
            SetTemperature(totalThermalEnergy);
            ValidateSolution();
        }

        public Color GetColorWithout(IPrototypeManager? protoMan = null, params string[] without)
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

        public Color GetColor(IPrototypeManager? protoMan = null)
        {
            return GetColorWithout(protoMan);
        }

        public Color GetColorWithOnly(IPrototypeManager? protoMan = null, params string[] included)
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
            Contents = new(reagents);
            foreach (var reagent in Contents)
            {
                Volume += reagent.Quantity;
            }

            if (setMaxVol)
                MaxVolume = Volume;

            UpdateHeatCapacity();
            UpdatePhaseTransitionReagents();
            Temperature = 293.15f;
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
