using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Alert
{
    /// <summary>
    /// Defines the order of alerts so they show up in a consistent order.
    /// </summary>
    [Prototype("alertOrder")]
    public class AlertOrderPrototype : IPrototype, IComparer<AlertPrototype>
    {
        public string ID { get; private set; }

        private readonly Dictionary<AlertType, int> _typeToIdx = new();
        private readonly Dictionary<AlertCategory, int> _categoryToIdx = new();

        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);

            serializer.DataField(this, x => x.ID, "id", string.Empty);

            if (!mapping.TryGetNode("order", out YamlSequenceNode orderMapping)) return;

            var i = 0;
            foreach (var entryYaml in orderMapping)
            {
                var orderEntry = (YamlMappingNode) entryYaml;
                var orderSerializer = YamlObjectSerializer.NewReader(orderEntry);
                if (orderSerializer.TryReadDataField("category", out AlertCategory alertCategory))
                {
                    _categoryToIdx[alertCategory] = i++;
                }
                else if (orderSerializer.TryReadDataField("alertType", out AlertType alertType))
                {
                    _typeToIdx[alertType] = i++;
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

        public int Compare(AlertPrototype x, AlertPrototype y)
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
