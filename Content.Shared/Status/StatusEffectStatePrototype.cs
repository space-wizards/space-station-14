using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Status
{
    /// <summary>
    /// A possible state of a particular status effect with its associated icon, tooltip, and other data.
    /// </summary>
    [NetSerializable, Serializable, Prototype("statusEffectState")]
    public class StatusEffectStatePrototype : IPrototype
    {
        private string _id;
        private string _icon;
        private StatusEffect _statusEffect;
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
        /// Path to the icon (png) to show in status window. If severity levels are supported,
        /// this should be the path to the icon without the severity number
        /// (i.e. hot.png if there is hot1.png and hot2.png).
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
        /// StatusEffect this is a state of.
        /// </summary>
        [ViewVariables]
        public StatusEffect StatusEffect => _statusEffect;

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
            serializer.DataField(ref _statusEffect, "statusEffect", StatusEffect.Error);
            serializer.DataField(ref _icon, "icon", string.Empty);
            serializer.DataField(ref _maxSeverity, "maxSeverity", (short) -1);
            serializer.DataField(ref _minSeverity, "minSeverity", (short) 1);
            serializer.DataReadFunction("name", string.Empty,
                s => _name = FormattedMessage.FromMarkup(s));
            serializer.DataReadFunction("description", string.Empty,
                s => _description = FormattedMessage.FromMarkup(s));
        }
    }
}
