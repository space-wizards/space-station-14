using System;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components
{
    [NetSerializable, Serializable]
    public enum ParticleAcceleratorVisuals
    {
        VisualState
    }

    [NetSerializable, Serializable]
    public enum ParticleAcceleratorVisualState
    {
        Open, //no prefix
        Wired, //w prefix
        Closed, //c prefix
        Powered, //p prefix
        Level0, //0 prefix
        Level1, //1 prefix
        Level2, //2 prefix
        Level3 //3 prefix
    }

    [NetSerializable, Serializable]
    public enum ParticleAcceleratorPowerState
    {
        Off = ParticleAcceleratorVisualState.Closed,
        Powered = ParticleAcceleratorVisualState.Powered,
        Level0 = ParticleAcceleratorVisualState.Level0,
        Level1 = ParticleAcceleratorVisualState.Level1,
        Level2 = ParticleAcceleratorVisualState.Level2,
        Level3 = ParticleAcceleratorVisualState.Level3
    }

    [NetSerializable, Serializable]
    public class ParticleAcceleratorDataUpdateMessage : BoundUserInterfaceMessage
    {
        public bool Assembled;
        public ParticleAcceleratorPowerState State;

        public ParticleAcceleratorDataUpdateMessage(bool assembled, ParticleAcceleratorPowerState state)
        {
            Assembled = assembled;
            this.State = state;
        }
    }

    [NetSerializable, Serializable]
    public class ParticleAcceleratorTogglePowerMessage : BoundUserInterfaceMessage{}

    [NetSerializable, Serializable]
    public class ParticleAcceleratorIncreasePowerMessage : BoundUserInterfaceMessage{}

    [NetSerializable, Serializable]
    public class ParticleAcceleratorDecreasePowerMessage : BoundUserInterfaceMessage{}

    [NetSerializable, Serializable]
    public enum ParticleAcceleratorControlBoxUiKey
    {
        Key
    }
}
