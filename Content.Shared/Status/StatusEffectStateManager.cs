using System.Collections.Generic;
using System.Linq;
using Content.Shared.Prototypes.Kitchen;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;

namespace Content.Shared.Status
{
    /// <summary>
    /// Provides access to all configured status effect states. Ability to encode/decode a given state
    /// to an int.
    /// </summary>
    public class StatusEffectStateManager
    {
        [Dependency]
        private readonly IPrototypeManager _prototypeManager = default!;

        private StatusEffectStatePrototype[] _orderedStatusEffectStates;
        private Dictionary<string, int> _idToIndex;

        public void Initialize()
        {
            // order by id so we can map between the id and an integer index and use
            // the index for compact status change messages
            _orderedStatusEffectStates =
                _prototypeManager.EnumeratePrototypes<StatusEffectStatePrototype>()
                    .OrderBy(prototype => prototype.ID).ToArray();

            for (var i = 0; i < _orderedStatusEffectStates.Length; i++)
            {
                if (!_idToIndex.TryAdd(_orderedStatusEffectStates[i].ID, i))
                {
                    Logger.ErrorS("status",
                        "Found statusState with duplicate id {0}", _orderedStatusEffectStates[i].ID);
                }
            }
        }

        /// <summary>
        /// Tries to get the status effect state with the indicated id
        /// </summary>
        /// <returns>true if found</returns>
        public bool TryGet(string statusEffectStateId, out StatusEffectStatePrototype statusEffectState)
        {
            if (_idToIndex.TryGetValue(statusEffectStateId, out var idx))
            {
                statusEffectState = _orderedStatusEffectStates[idx];
                return true;
            }

            statusEffectState = null;
            return false;
        }

        /// <summary>
        /// Tries to get the compact encoded representation of this status effect state
        /// </summary>
        /// <returns>true if successful</returns>
        public bool TryEncode(StatusEffectStatePrototype statusEffectState, out int encoded)
        {
            if (_idToIndex.TryGetValue(statusEffectState.ID, out var idx))
            {
                encoded = idx;
                return true;
            }

            encoded = -1;
            return false;
        }

        /// <summary>
        /// Tries to get the compact encoded representation of the status effect state with
        /// the indicated id
        /// </summary>
        /// <returns>true if successful</returns>
        public bool TryEncode(string statusEffectStateId, out int encoded)
        {
            if (_idToIndex.TryGetValue(statusEffectStateId, out var idx))
            {
                encoded = idx;
                return true;
            }

            encoded = -1;
            return false;
        }
    }
}
