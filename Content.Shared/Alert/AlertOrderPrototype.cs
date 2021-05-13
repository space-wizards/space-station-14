#nullable enable
using System;
using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Alert
{
    /// <summary>
    /// Defines the order of alerts so they show up in a consistent order.
    /// </summary>
    [Prototype("alertOrder")]
    [DataDefinition]
    public class AlertOrderPrototype : IPrototype, IComparer<AlertPrototype>, ISerializationHooks
    {
        [ViewVariables]
        [DataField("id", required: true)]
        public string ID { get; } = default!;

        [DataField("order")] private readonly List<(string type, string alert)> _order = new();

        private readonly Dictionary<AlertType, int> _typeToIdx = new();
        private readonly Dictionary<AlertCategory, int> _categoryToIdx = new();

        void ISerializationHooks.BeforeSerialization()
        {
            _order.Clear();

            var orderArray = new KeyValuePair<string, string>[_typeToIdx.Count + _categoryToIdx.Count];

            foreach (var (type, id) in _typeToIdx)
            {
                orderArray[id] = new KeyValuePair<string, string>("alertType", type.ToString());
            }

            foreach (var (category, id) in _categoryToIdx)
            {
                orderArray[id] = new KeyValuePair<string, string>("category", category.ToString());
            }

            foreach (var (type, alert) in orderArray)
            {
                _order.Add((type, alert));
            }
        }

        void ISerializationHooks.AfterDeserialization()
        {
            var i = 0;

            foreach (var (type, alert) in _order)
            {
                switch (type)
                {
                    case "alertType":
                        _typeToIdx[Enum.Parse<AlertType>(alert)] = i++;
                        break;
                    case "category":
                        _categoryToIdx[Enum.Parse<AlertCategory>(alert)] = i++;
                        break;
                    default:
                        throw new ArgumentException();
                }
            }
        }

        private int GetOrderIndex(AlertPrototype alert)
        {
            if (_typeToIdx.TryGetValue(alert.AlertType, out var idx))
            {
                return idx;
            }
            if (alert.Category != null &&
                _categoryToIdx.TryGetValue((AlertCategory) alert.Category, out idx))
            {
                return idx;
            }

            return -1;
        }

        public int Compare(AlertPrototype? x, AlertPrototype? y)
        {
            if ((x == null) && (y == null)) return 0;
            if (x == null) return 1;
            if (y == null) return -1;
            var idx = GetOrderIndex(x);
            var idy = GetOrderIndex(y);
            if (idx == -1 && idy == -1)
            {
                // break ties by type value
                return x.AlertType - y.AlertType;
            }

            if (idx == -1) return 1;
            if (idy == -1) return -1;
            var result = idx - idy;
            // not strictly necessary (we don't care about ones that go at the same index)
            // but it makes the sort stable
            if (result == 0)
            {
                // break ties by type value
                return x.AlertType - y.AlertType;
            }

            return result;
        }
    }
}
