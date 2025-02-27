using System.Text.RegularExpressions;
using Content.Shared.Tools;
using Content.Shared.Tools.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Configurable
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class ConfigurationComponent : Component
    {
        [DataField]
        public Dictionary<string, string?> Config = new();

        [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<ToolQualityPrototype>))]
        public string QualityNeeded = SharedToolSystem.PulseQuality;

        [DataField]
        public Regex Validation = new("^[a-zA-Z0-9 ]*$", RegexOptions.Compiled);

        [Serializable, NetSerializable]
        public sealed class ConfigurationBoundUserInterfaceState(Dictionary<string, string?> config)
            : BoundUserInterfaceState
        {
            public readonly Dictionary<string, string?> Config = config;
        }

        /// <summary>
        ///     Message data sent from client to server when the device configuration is updated.
        /// </summary>
        [Serializable, NetSerializable]
        public sealed class ConfigurationUpdatedMessage(Dictionary<string, string> config) : BoundUserInterfaceMessage
        {
            public readonly Dictionary<string, string> Config = config;
        }

        [Serializable, NetSerializable]
        public sealed class ValidationUpdateMessage(string validationString) : BoundUserInterfaceMessage
        {
            public readonly string ValidationString = validationString;
        }

        [Serializable, NetSerializable]
        public enum ConfigurationUiKey
        {
            Key,
        }
    }
}
