using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
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
    [NetSerializable, Serializable, Prototype("alert")]
    public class AlertPrototype : IPrototype
    {
        private const string SerializationCache = "alert";

        private string _id;
        private string _icon;
        private AlertSlot _alertSlot;
        private FormattedMessage _name;
        private FormattedMessage _description;
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
        /// AlertSlot this is a state of.
        /// </summary>
        [ViewVariables]
        public AlertSlot AlertSlot => _alertSlot;

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
            serializer.DataReadFunction("name", string.Empty,
                s => _name = FormattedMessage.FromMarkup(s));
            serializer.DataReadFunction("description", string.Empty,
                s => _description = FormattedMessage.FromMarkup(s));

            // TODO: kinda verbose and slightly duplicated in SharedStackComponent, refactor to a common method
            if (serializer.TryReadDataFieldCached("statusEffect", out string raw))
            {
                var refl = IoCManager.Resolve<IReflectionManager>();
                if (refl.TryParseEnumReference(raw, out var @enum))
                {
                    _alertSlot = (AlertSlot) @enum;
                }
                else
                {
                    Logger.WarningS("unable to parse StatusEffect in {0} from statusEffect: {1}", _id, raw);
                    _alertSlot = AlertSlot.Error;
                }
            }
            else
            {
                Logger.WarningS("no statusEffect defined for status effect state {0}", _id);
                _alertSlot = AlertSlot.Error;
            }
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
}
