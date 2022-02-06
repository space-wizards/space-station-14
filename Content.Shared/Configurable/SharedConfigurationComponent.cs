using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Configurable
{
    public class SharedConfigurationComponent : Component
    {
        [Serializable, NetSerializable]
        public class ConfigurationBoundUserInterfaceState : BoundUserInterfaceState
        {
            public Dictionary<string, string> Config { get; }

            public ConfigurationBoundUserInterfaceState(Dictionary<string, string> config)
            {
                Config = config;
            }
        }

        /// <summary>
        ///     Message sent to other components on this entity when DeviceNetwork configuration updated.
        /// </summary>
#pragma warning disable 618
        public class ConfigUpdatedComponentMessage : ComponentMessage
#pragma warning restore 618
        {
            public Dictionary<string, string> Config { get; }

            public ConfigUpdatedComponentMessage(Dictionary<string, string> config)
            {
                Config = config;
            }
        }

        /// <summary>
        ///     Message data sent from client to server when the device configuration is updated.
        /// </summary>
        [Serializable, NetSerializable]
        public class ConfigurationUpdatedMessage : BoundUserInterfaceMessage
        {
            public Dictionary<string, string> Config { get; }

            public ConfigurationUpdatedMessage(Dictionary<string, string> config)
            {
                Config = config;
            }
        }

        [Serializable, NetSerializable]
        public class ValidationUpdateMessage : BoundUserInterfaceMessage
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
