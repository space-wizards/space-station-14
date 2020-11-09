using System.Collections.Generic;
using System.Linq;
using Content.Shared.Prototypes.Kitchen;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;

namespace Content.Shared.Alert
{
    /// <summary>
    /// Provides access to all configured alerts. Ability to encode/decode a given state
    /// to an int.
    /// </summary>
    public class AlertManager
    {
        [Dependency]
        private readonly IPrototypeManager _prototypeManager = default!;

        private AlertPrototype[] _orderedAlerts;
        private Dictionary<AlertType, int> _typeToIndex;

        public void Initialize()
        {
            // order by type value so we can map between the id and an integer index and use
            // the index for compact alert change messages
            _orderedAlerts =
                _prototypeManager.EnumeratePrototypes<AlertPrototype>()
                    .OrderBy(prototype => prototype.AlertType).ToArray();
            _typeToIndex = new Dictionary<AlertType, int>();

            for (var i = 0; i < _orderedAlerts.Length; i++)
            {
                if (!_typeToIndex.TryAdd(_orderedAlerts[i].AlertType, i))
                {
                    Logger.ErrorS("alert",
                        "Found alert with duplicate id {0}", _orderedAlerts[i].AlertType);
                }
            }
        }

        /// <summary>
        /// Tries to get the alert of the indicated type
        /// </summary>
        /// <returns>true if found</returns>
        public bool TryGet(AlertType alertType, out AlertPrototype alert)
        {
            if (_typeToIndex.TryGetValue(alertType, out var idx))
            {
                alert = _orderedAlerts[idx];
                return true;
            }

            alert = null;
            return false;
        }

        /// <summary>
        /// Tries to get the alert of the indicated type along with its encoding
        /// </summary>
        /// <returns>true if found</returns>
        public bool TryGetWithEncoded(AlertType alertType, out AlertPrototype alert, out int encoded)
        {
            if (_typeToIndex.TryGetValue(alertType, out var idx))
            {
                alert = _orderedAlerts[idx];
                encoded = idx;
                return true;
            }

            alert = null;
            encoded = -1;
            return false;
        }

        /// <summary>
        /// Tries to get the compact encoded representation of this alert
        /// </summary>
        /// <returns>true if successful</returns>
        public bool TryEncode(AlertPrototype alert, out int encoded)
        {
            return TryEncode(alert.AlertType, out encoded);
        }

        /// <summary>
        /// Tries to get the compact encoded representation of the alert with
        /// the indicated id
        /// </summary>
        /// <returns>true if successful</returns>
        public bool TryEncode(AlertType alertType, out int encoded)
        {
            if (_typeToIndex.TryGetValue(alertType, out var idx))
            {
                encoded = idx;
                return true;
            }

            encoded = -1;
            return false;
        }

        /// <summary>
        /// Tries to get the alert from the encoded representation
        /// </summary>
        /// <returns>true if successful</returns>
        public bool TryDecode(int encodedAlert, out AlertPrototype alert)
        {
            if (encodedAlert < 0 ||
                encodedAlert >= _orderedAlerts.Length)
            {
                alert = null;
                return false;
            }

            alert = _orderedAlerts[encodedAlert];
            return true;
        }
    }
}
