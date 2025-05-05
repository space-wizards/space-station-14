using System.Text.RegularExpressions;
using Content.Shared.Tools;
using Content.Shared.Tools.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Configurable
{
    /// <summary>
    /// Configuration for mailing units.
    /// </summary>
    /// <remarks>
    /// If you want a more detailed description ask the original coder.
    /// </remarks>
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class ConfigurationComponent : Component
    {
        /// <summary>
        /// Tags for mail unit routing.
        /// </summary>
        [DataField, AutoNetworkedField]
        public Dictionary<string, string?> Config = new();

        /// <summary>
        /// Quality to open up the configuration UI.
        /// </summary>
        [DataField]
        public ProtoId<ToolQualityPrototype> QualityNeeded = SharedToolSystem.PulseQuality;

        /// <summary>
        /// Validate tags in <see cref="Config"/>.
        /// </summary>
        [DataField]
        public Regex Validation = new("^[a-zA-Z0-9 ]*$", RegexOptions.Compiled);

        /// <summary>
        ///     Message data sent from client to server when the device configuration is updated.
        /// </summary>
        [Serializable, NetSerializable]
        public sealed class ConfigurationUpdatedMessage : BoundUserInterfaceMessage
        {
            public Dictionary<string, string> Config { get; }

            public ConfigurationUpdatedMessage(Dictionary<string, string> config)
            {
                Config = config;
            }
        }

        [Serializable, NetSerializable]
        public sealed class ValidationUpdateMessage : BoundUserInterfaceMessage
        {
            public string ValidationString { get; }

            public ValidationUpdateMessage(string validationString)
            {
                ValidationString = validationString;
            }
        }

        [Serializable, NetSerializable]
        public enum ConfigurationUiKey
        {
            Key
        }
    }
}
