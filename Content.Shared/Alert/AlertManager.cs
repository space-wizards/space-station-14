using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;

namespace Content.Shared.Alert
{
    /// <summary>
    /// Provides access to all configured alerts by alert type.
    /// </summary>
    public class AlertManager
    {
        [Dependency]
        private readonly IPrototypeManager _prototypeManager = default!;

        private readonly Dictionary<AlertType, AlertPrototype> _typeToAlert = new();

        public void Initialize()
        {
            foreach (var alert in _prototypeManager.EnumeratePrototypes<AlertPrototype>())
            {
                if (!_typeToAlert.TryAdd(alert.AlertType, alert))
                {
                    Logger.ErrorS("alert",
                        "Found alert with duplicate alertType {0} - all alerts must have" +
                        " a unique alerttype, this one will be skipped", alert.AlertType);
                }
            }
        }

        /// <summary>
        /// Tries to get the alert of the indicated type
        /// </summary>
        /// <returns>true if found</returns>
        public bool TryGet(AlertType alertType, [NotNullWhen(true)] out AlertPrototype? alert)
        {
            return _typeToAlert.TryGetValue(alertType, out alert);
        }
    }
}
