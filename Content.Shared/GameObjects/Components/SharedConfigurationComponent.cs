using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;

namespace Content.Shared.GameObjects.Components
{
    public class SharedConfigurationComponent : Component
    {
        public override string Name => "Configuration";

        [Serializable, NetSerializable]
        public class ConfigurationBoundUserInterfaceState : BoundUserInterfaceState
        {
            public readonly Dictionary<string, string> Config;
            
            public ConfigurationBoundUserInterfaceState(Dictionary<string, string> config)
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
            public readonly Dictionary<string, string> Config;

            public ConfigurationUpdatedMessage(Dictionary<string, string> config)
            {
                Config = config;
            }
        }

        [Serializable, NetSerializable]
        public class ValidationUpdateMessage : BoundUserInterfaceMessage
        {
            public readonly string ValidationString;

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
