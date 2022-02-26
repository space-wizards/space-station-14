using System;
using System.Globalization;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Alert
{
    /// <summary>
    /// An alert popup with associated icon, tooltip, and other data.
    /// </summary>
    [Prototype("alert")]
    public sealed class AlertPrototype : IPrototype, ISerializationHooks
    {
        [ViewVariables]
        string IPrototype.ID => AlertType.ToString();

        /// <summary>
        /// Type of alert, no 2 alert prototypes should have the same one.
        /// </summary>
        [DataField("alertType")]
        public AlertType AlertType { get; private set; }

        /// <summary>
        /// Path to the icon (png) to show in alert bar. If severity levels are supported,
        /// this should be the path to the icon without the severity number
        /// (i.e. hot.png if there is hot1.png and hot2.png). Use <see cref="GetIconPath"/>
        /// to get the correct icon path for a particular severity level.
        /// </summary>
        [ViewVariables]
        [DataField("icon")]
        public SpriteSpecifier Icon { get; private set; } = SpriteSpecifier.Invalid;

        /// <summary>
        /// Name to show in tooltip window. Accepts formatting.
        /// </summary>
        [DataField("name")]
        public FormattedMessage Name { get; private set; } = new();

        /// <summary>
        /// Description to show in tooltip window. Accepts formatting.
        /// </summary>
        [DataField("description")]
        public FormattedMessage Description { get; private set; } = new();

        /// <summary>
        /// Category the alert belongs to. Only one alert of a given category
        /// can be shown at a time. If one is shown while another is already being shown,
        /// it will be replaced. This can be useful for categories of alerts which should naturally
        /// replace each other and are mutually exclusive, for example lowpressure / highpressure,
        /// hot / cold. If left unspecified, the alert will not replace or be replaced by any other alerts.
        /// </summary>
        [DataField("category")]
        public AlertCategory? Category { get; private set; }

        /// <summary>
        /// Key which is unique w.r.t category semantics (alerts with same category have equal keys,
        /// alerts with no category have different keys).
        /// </summary>
        public AlertKey AlertKey { get; private set; }

        /// <summary>
        /// -1 (no effect) unless MaxSeverity is specified. Defaults to 1. Minimum severity level supported by this state.
        /// </summary>
        public short MinSeverity => MaxSeverity == -1 ? (short) -1 : _minSeverity;

        [DataField("minSeverity")] private short _minSeverity = 1;

        /// <summary>
        /// Maximum severity level supported by this state. -1 (default) indicates
        /// no severity levels are supported by the state.
        /// </summary>
        [DataField("maxSeverity")]
        public short MaxSeverity = -1;

        /// <summary>
        /// Indicates whether this state support severity levels
        /// </summary>
        public bool SupportsSeverity => MaxSeverity != -1;

        /// <summary>
        /// Defines what to do when the alert is clicked.
        /// This will always be null on clientside.
        /// </summary>
        [DataField("onClick", serverOnly: true)]
        public IAlertClick? OnClick { get; private set; }

        void ISerializationHooks.AfterDeserialization()
        {
            if (AlertType == AlertType.Error)
            {
                Logger.ErrorS("alert", "missing or invalid alertType for alert with name {0}", Name);
            }

            AlertKey = new AlertKey(AlertType, Category);
        }

        /// <param name="severity">severity level, if supported by this alert</param>
        /// <returns>the icon path to the texture for the provided severity level</returns>
        public SpriteSpecifier GetIcon(short? severity = null)
        {
            if (!SupportsSeverity && severity != null)
            {
                throw new InvalidOperationException($"This alert ({AlertKey}) does not support severity");
            }

            if (!SupportsSeverity)
                return Icon;

            if (severity == null)
            {
                throw new ArgumentException($"No severity specified but this alert ({AlertKey}) has severity.", nameof(severity));
            }

            if (severity < MinSeverity)
            {
                throw new ArgumentOutOfRangeException(nameof(severity), $"Severity below minimum severity in {AlertKey}.");
            }

            if (severity > MaxSeverity)
            {
                throw new ArgumentOutOfRangeException(nameof(severity), $"Severity above maximum severity in {AlertKey}.");
            }

            var severityText = severity.Value.ToString(CultureInfo.InvariantCulture);
            switch (Icon)
            {
                case SpriteSpecifier.EntityPrototype entityPrototype:
                    throw new InvalidOperationException($"Severity not supported for EntityPrototype icon in {AlertKey}");
                case SpriteSpecifier.Rsi rsi:
                    return new SpriteSpecifier.Rsi(rsi.RsiPath, rsi.RsiState + severityText);
                case SpriteSpecifier.Texture texture:
                    var newName = texture.TexturePath.FilenameWithoutExtension + severityText;
                    return new SpriteSpecifier.Texture(
                        texture.TexturePath.WithName(newName + "." + texture.TexturePath.Extension));
                default:
                    throw new ArgumentOutOfRangeException(nameof(Icon));
            }
        }
    }
}
