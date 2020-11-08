using System;
using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Alert
{
    /// <summary>
    /// Defines the order of alerts so they show up in a consistent order.
    /// </summary>
    [Prototype("alertOrder")]
    public class AlertOrderPrototype : IPrototype, IComparer<AlertPrototype>
    {
        private List<string> _order;
        private Dictionary<string, int> _idCategoryToIdx = new Dictionary<string, int>();

        /// <summary>
        /// List of alert Ids and alert categories, determining the order
        /// in which the corresponding alerts should be shown in the alert bar.
        /// If an alert has both its id and category in this list, the id will
        /// be used to determine the order (i.e. the id can be put in the list to "override"
        /// the order for that alert's category).
        ///
        /// If an alert's id and category is not in the list, it will go at the end of the alert list,
        /// ties broken alphabetically by alert id
        /// </summary>
        public List<string> Order => _order;

        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);
            serializer.DataField(ref _order, "order", new List<string>());
            // build index mapping dict
            int idx = 0;
            foreach (var idCategory in _order)
            {
                _idCategoryToIdx[idCategory] = idx++;
            }
        }

        private int GetOrderIndex(AlertPrototype alert)
        {
            if (_idCategoryToIdx.TryGetValue(alert.ID, out var idx))
            {
                return idx;
            }
            if (alert.Category != null &&
                _idCategoryToIdx.TryGetValue(alert.Category, out idx))
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
                return string.CompareOrdinal(x.ID, y.ID);
            }

            if (idx == -1) return 1;
            if (idy == -1) return -1;
            var result = idx - idy;
            // not strictly necessary (we don't care about ones that go at the same index)
            // but it makes the sort stable
            if (result == 0)
            {
                return string.CompareOrdinal(x.ID, y.ID);
            }
            else
            {
                return result;
            }
        }
    }
}
