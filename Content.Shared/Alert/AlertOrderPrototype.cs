using System.Linq;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Alert
{
    /// <summary>
    /// Defines the order of alerts so they show up in a consistent order.
    /// </summary>
    [Prototype("alertOrder")]
    [DataDefinition]
    public sealed partial class AlertOrderPrototype : IPrototype, IComparer<AlertPrototype>
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; private set; } = default!;

        [DataField("order")]
        private (string type, string alert)[] Order
        {
            // why would paul do this to me.
            get
            {
                var res = new (string, string)[_typeToIdx.Count + _categoryToIdx.Count];

                foreach (var (type, id) in _typeToIdx)
                {
                    res[id] = ("alertType", type.ToString());
                }

                foreach (var (category, id) in _categoryToIdx)
                {
                    res[id] = ("category", category.ToString());
                }

                DebugTools.Assert(res.All(x => x != default));

                return res;
            }
            set
            {
                var i = 0;

                foreach (var (type, alert) in value)
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
        }

        private readonly Dictionary<AlertType, int> _typeToIdx = new();
        private readonly Dictionary<AlertCategory, int> _categoryToIdx = new();

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
                // Must cast to int to avoid integer overflow when subtracting (enum's unsigned)
                return (int)x.AlertType - (int)y.AlertType;
            }

            if (idx == -1) return 1;
            if (idy == -1) return -1;
            var result = idx - idy;
            // not strictly necessary (we don't care about ones that go at the same index)
            // but it makes the sort stable
            if (result == 0)
            {
                // break ties by type value
                // Must cast to int to avoid integer overflow when subtracting (enum's unsigned)
                return (int)x.AlertType - (int)y.AlertType;
            }

            return result;
        }
    }
}
