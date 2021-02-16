using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Gravity
{
    public class SharedGravityGeneratorComponent : Component
    {
        public override string Name => "GravityGenerator";

        public override uint? NetID => ContentNetIDs.GRAVITY_GENERATOR;

        /// <summary>
        ///     Sent to the server to set whether the generator should be on or off
        /// </summary>
        [Serializable, NetSerializable]
        public class SwitchGeneratorMessage : BoundUserInterfaceMessage
        {
            public bool On;

            public SwitchGeneratorMessage(bool on)
            {
                On = on;
            }
        }

        /// <summary>
        ///     Sent to the server when requesting the status of the generator
        /// </summary>
        [Serializable, NetSerializable]
        public class GeneratorStatusRequestMessage : BoundUserInterfaceMessage
        {
            public GeneratorStatusRequestMessage()
            {

            }
        }

        [Serializable, NetSerializable]
        public class GeneratorState : BoundUserInterfaceState
        {
            public bool On;

            public GeneratorState(bool on)
            {
                On = on;
            }
        }

        [Serializable, NetSerializable]
        public enum GravityGeneratorUiKey
        {
            Key
        }
    }

    [Serializable, NetSerializable]
    public enum GravityGeneratorVisuals
    {
        State,
        CoreVisible
    }

    [Serializable, NetSerializable]
    public enum GravityGeneratorStatus
    {
        Broken,
        Unpowered,
        Off,
        On
    }
}
