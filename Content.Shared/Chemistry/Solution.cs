using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Chemistry
{
    /// <summary>
    ///     A solution of reagents.
    /// </summary>
    public class Solution : IExposeData, IEnumerable<Solution.ReagentQuantity>
    {
        // Most objects on the station hold only 1 or 2 reagents
        [ViewVariables]
        private List<ReagentQuantity> _contents = new List<ReagentQuantity>(2);

        /// <summary>
        ///     The calculated total volume of all reagents in the solution (ex. Total volume of liquid in beaker).
        /// </summary>
        [ViewVariables]
        public int TotalVolume { get; private set; }

        /// <summary>
        ///     Constructs an empty solution (ex. an empty beaker).
        /// </summary>
        public Solution() { }

        /// <summary>
        ///     Constructs a solution containing 100% of a reagent (ex. A beaker of pure water).
        /// </summary>
        /// <param name="reagentId">The prototype ID of the reagent to add.</param>
        /// <param name="quantity">The quantity in milli-units.</param>
        public Solution(string reagentId, int quantity)
        {
            AddReagent(reagentId, quantity);
        }

        /// <inheritdoc />
        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _contents, "reagents", new List<ReagentQuantity>());

            if (serializer.Reading)
            {
                TotalVolume = 0;
                foreach (var reagent in _contents)
                {
                    TotalVolume += reagent.Quantity;
                }
            }
        }

        /// <summary>
        ///     Adds a given quantity of a reagent directly into the solution. 
        /// </summary>
        /// <param name="reagentId">The prototype ID of the reagent to add.</param>
        /// <param name="quantity">The quantity in milli-units.</param>
        public void AddReagent(string reagentId, int quantity)
        {
            if(quantity <= 0)
                return;

            for (var i = 0; i < _contents.Count; i++)
            {
                var reagent = _contents[i];
                if (reagent.ReagentId != reagentId)
                    continue;

                _contents[i] = new ReagentQuantity(reagentId, reagent.Quantity + quantity);
                TotalVolume += quantity;
                return;
            }

            _contents.Add(new ReagentQuantity(reagentId, quantity));
            TotalVolume += quantity;
        }

        /// <summary>
        ///     Returns the amount of a single reagent inside the solution.
        /// </summary>
        /// <param name="reagentId">The prototype ID of the reagent to add.</param>
        /// <returns>The quantity in milli-units.</returns>
        public int GetReagentQuantity(string reagentId)
        {
            for (var i = 0; i < _contents.Count; i++)
            {
                if (_contents[i].ReagentId == reagentId)
                    return _contents[i].Quantity;
            }

            return 0;
        }

        public void RemoveReagent(string reagentId, int quantity)
        {
            if(quantity <= 0)
                return;

            for (var i = 0; i < _contents.Count; i++)
            {
                var reagent = _contents[i];
                if(reagent.ReagentId != reagentId)
                    continue;

                var curQuantity = reagent.Quantity;

                var newQuantity = curQuantity - quantity;
                if (newQuantity <= 0)
                {
                    _contents.RemoveSwap(i);
                    TotalVolume -= curQuantity;
                }
                else
                {
                    _contents[i] = new ReagentQuantity(reagentId, newQuantity);
                    TotalVolume -= quantity;
                }

                return;
            }
        }

        public void RemoveSolution(int quantity)
        {
            if(quantity <=0)
                return;

            var ratio = (float)(TotalVolume - quantity) / TotalVolume;

            if (ratio <= 0)
            {
                RemoveAllSolution();
                return;
            }

            for (var i = 0; i < _contents.Count; i++)
            {
                var reagent = _contents[i];
                var oldQuantity = reagent.Quantity;

                // quantity taken is always a little greedy, so fractional quantities get rounded up to the nearest
                // whole unit. This should prevent little bits of chemical remaining because of float rounding errors.
                var newQuantity = (int)Math.Floor(oldQuantity * ratio);

                _contents[i] = new ReagentQuantity(reagent.ReagentId, newQuantity);
            }

            TotalVolume = (int)Math.Floor(TotalVolume * ratio);
        }

        public void RemoveAllSolution()
        {
            _contents.Clear();
            TotalVolume = 0;
        }

        public Solution SplitSolution(int quantity)
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
            var newTotalVolume = 0;
            var ratio = (float)(TotalVolume - quantity) / TotalVolume;

            for (var i = 0; i < _contents.Count; i++)
            {
                var reagent = _contents[i];

                var newQuantity = (int)Math.Floor(reagent.Quantity * ratio);
                var splitQuantity = reagent.Quantity - newQuantity;

                _contents[i] = new ReagentQuantity(reagent.ReagentId, newQuantity);
                newSolution._contents.Add(new ReagentQuantity(reagent.ReagentId, splitQuantity));
                newTotalVolume += splitQuantity;
            }

            TotalVolume = (int)Math.Floor(TotalVolume * ratio);
            newSolution.TotalVolume = newTotalVolume;

            return newSolution;
        }
        
        public void AddSolution(Solution otherSolution)
        {
            for (var i = 0; i < otherSolution._contents.Count; i++)
            {
                var otherReagent = otherSolution._contents[i];

                var found = false;
                for (var j = 0; j < _contents.Count; j++)
                {
                    var reagent = _contents[j];
                    if (reagent.ReagentId == otherReagent.ReagentId)
                    {
                        found = true;
                        _contents[j] = new ReagentQuantity(reagent.ReagentId, reagent.Quantity + otherReagent.Quantity);
                        break;
                    }
                }

                if (!found)
                {
                    _contents.Add(new ReagentQuantity(otherReagent.ReagentId, otherReagent.Quantity));
                }
            }

            TotalVolume += otherSolution.TotalVolume;
        }

        public Solution Clone()
        {
            var volume = 0;
            var newSolution = new Solution();

            for (var i = 0; i < _contents.Count; i++)
            {
                var reagent = _contents[i];
                newSolution._contents.Add(reagent);
                volume += reagent.Quantity;
            }
            
            newSolution.TotalVolume = volume;
            return newSolution;
        }

        [Serializable, NetSerializable]
        public readonly struct ReagentQuantity
        {
            public readonly string ReagentId;
            public readonly int Quantity;

            public ReagentQuantity(string reagentId, int quantity)
            {
                ReagentId = reagentId;
                Quantity = quantity;
            }

            [ExcludeFromCodeCoverage]
            public override string ToString()
            {
                return $"{ReagentId}:{Quantity}";
            }
        }

        #region Enumeration
        
        public IEnumerator<ReagentQuantity> GetEnumerator()
        {
            return _contents.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
