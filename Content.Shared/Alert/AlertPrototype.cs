using System;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Alert
{
    /// <summary>
    /// An alert popup with associated icon, tooltip, and other data.
    /// </summary>
    [Prototype("alert")]
    public class AlertPrototype : IPrototype
    {
        private string _id;
        private string _icon;
        private FormattedMessage _name;
        private FormattedMessage _description;
        private string _category;
        private AlertKey _alertKey;
        private short _maxSeverity;
        private short _minSeverity;

        /// <summary>
        /// Unique identifier used to reference this state in code
        /// </summary>
        [ViewVariables]
        public string ID => _id;

        /// <summary>
        /// Path to the icon (png) or  to show in alert bar. If severity levels are supported,
        /// this should be the path to the icon without the severity number
        /// (i.e. hot.png if there is hot1.png and hot2.png). Use <see cref="GetSpriteSpecifier"/>
        /// to get the correct icon path for a particular severity level.
        /// </summary>
        [ViewVariables]
        public string IconPath => _icon;

        /// <summary>
        /// Name to show in tooltip window. Accepts formatting.
        /// </summary>
        public FormattedMessage Name => _name;

        /// <summary>
        /// Description to show in tooltip window. Accepts formatting.
        /// </summary>
        public FormattedMessage Description => _description;

        /// <summary>
        /// Category the alert belongs to. Only one alert of a given category
        /// can be shown at a time. If one is shown while another is already being shown,
        /// it will be replaced. This can be useful for categories of alerts which should naturally
        /// replace each other and are mutually exclusive, for example lowpressure / highpressure,
        /// hot / cold. If left unspecified, the alert will not replace or be replaced by any other alerts.
        /// </summary>
        public string Category => _category;

        /// <summary>
        /// Key which is unique w.r.t category semantics (alerts with same category have equal keys,
        /// alerts with no category have different keys).
        /// </summary>
        public AlertKey AlertKey => _alertKey;

        /// <summary>
        /// -1 (no effect) unless MaxSeverity is specified. Defaults to 1. Minimum severity level supported by this state.
        /// </summary>
        public short MinSeverity => _maxSeverity == -1 ? (short) -1 : _minSeverity;

        /// <summary>
        /// Maximum severity level supported by this state. -1 (default) indicates
        /// no severity levels are supported by the state.
        /// </summary>
        public short MaxSeverity => _maxSeverity;

        /// <summary>
        /// Indicates whether this state support severity levels
        /// </summary>
        public bool SupportsSeverity => MaxSeverity != -1;

        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);

            serializer.DataField(ref _id, "id", string.Empty);
            serializer.DataField(ref _icon, "icon", string.Empty);
            serializer.DataField(ref _maxSeverity, "maxSeverity", (short) -1);
            serializer.DataField(ref _minSeverity, "minSeverity", (short) 1);
            serializer.DataField(ref _category, "category", null);
            _alertKey = new AlertKey(_category, _id);
            serializer.DataReadFunction("name", string.Empty,
                s => _name = FormattedMessage.FromMarkup(s));
            serializer.DataReadFunction("description", string.Empty,
                s => _description = FormattedMessage.FromMarkup(s));
        }

        /// <param name="severity">severity level, if supported by this alert</param>
        /// <returns>the icon path to the texture for the provided severity level</returns>
        public string GetIconPath(short? severity = null)
        {
            if (!SupportsSeverity && severity != null)
            {
                Logger.WarningS("alert", "attempted to get icon path for severity level for alert {0}, but" +
                                          " this alert does not support severity levels", _id);
            }
            if (!SupportsSeverity) return _icon;
            if (severity == null)
            {
                Logger.WarningS("alert", "attempted to get icon path without severity level for alert {0}," +
                                " but this alert requires a severity level. Using lowest" +
                                " valid severity level instead...", _id);
                severity = MinSeverity;
            }

            if (severity < MinSeverity)
            {
                Logger.WarningS("alert", "attempted to get icon path with severity level {0} for alert {1}," +
                                          " but the minimum severity level for this alert is {2}. Using" +
                                          " lowest valid severity level instead...", severity, _id, MinSeverity);
                severity = MinSeverity;
            }
            if (severity > MaxSeverity)
            {
                Logger.WarningS("alert", "attempted to get icon path with severity level {0} for alert {1}," +
                                          " but the max severity level for this alert is {2}. Using" +
                                          " highest valid severity level instead...", severity, _id, MaxSeverity);
                severity = MaxSeverity;
            }

            // split and add the severity number to the path
            var ext = _icon.LastIndexOf('.');
            return _icon.Substring(0, ext) + severity + _icon.Substring(ext, _icon.Length - ext);
        }
    }

    /// <summary>
    /// Key for an alert which is unique (for equality and hashcode purposes) w.r.t category semantics.
    /// I.e., the category, if a category was specified, otherwise
    /// cat- + the id (prefixed to avoid accidental overlap between category and id).
    /// </summary>
    [Serializable]
    public struct AlertKey
    {
        private readonly string _key;

        public AlertKey(string category, string id) : this()
        {
            _key = category ?? "cat-" + id;
        }

        public bool Equals(AlertKey other)
        {
            return _key == other._key;
        }

        public override bool Equals(object obj)
        {
            return obj is AlertKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (_key != null ? _key.GetHashCode() : 0);
        }

        /// <param name="category">alert category, must not be null</param>
        /// <returns>An alert key for the provided alert category</returns>
        public static AlertKey ForCategory(string category)
        {
            if (category == null)
            {
                Logger.ErrorS("alert", "tried to create AlertKey with null category, this is not allowed");
                return new AlertKey();
            }
            return new AlertKey(category, null);
        }
    }
}
