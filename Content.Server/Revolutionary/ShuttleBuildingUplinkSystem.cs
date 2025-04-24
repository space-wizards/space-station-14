using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Revolutionary
{
    public sealed class ShuttleBuildingUplinkSystem : EntitySystem
    {
        private readonly Dictionary<EntityUid, int> _uplinkCurrency = new();

        /// <summary>
        /// Tries to get the uplink entity associated with the given entity uid.
        /// </summary>
        public bool TryGetUplinkEntity(EntityUid uid, out EntityUid uplinkEntity)
        {
            // For simplicity, assume uplink entity is the same as uid if currency exists
            if (_uplinkCurrency.ContainsKey(uid))
            {
                uplinkEntity = uid;
                return true;
            }

            uplinkEntity = default;
            return false;
        }

        /// <summary>
        /// Gets the current currency count for the given entity uid.
        /// </summary>
        public int GetCurrency(EntityUid uid)
        {
            return _uplinkCurrency.TryGetValue(uid, out var currency) ? currency : 0;
        }

        /// <summary>
        /// Increments the currency count for the given entity uid by the specified amount.
        /// </summary>
        public void IncrementCurrency(EntityUid uid, int amount = 1)
        {
            if (_uplinkCurrency.ContainsKey(uid))
            {
                _uplinkCurrency[uid] += amount;
            }
            else
            {
                _uplinkCurrency[uid] = amount;
            }
        }
    }
}
