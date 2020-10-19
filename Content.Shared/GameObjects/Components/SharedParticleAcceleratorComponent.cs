using System;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components
{
    [Serializable, NetSerializable]
    public enum ParticleAcceleratorWireStatus
    {
        KeyboardIndicator,
        LimiterIndicator,
    }

    [NetSerializable, Serializable]
    public enum ParticleAcceleratorVisuals
    {
        VisualState
    }

    [NetSerializable, Serializable]
    public enum ParticleAcceleratorVisualState
    {
        //Open, //no prefix
        //Wired, //w prefix
        Unpowered, //c prefix
        Powered, //p prefix
        Level0, //0 prefix
        Level1, //1 prefix
        Level2, //2 prefix
        Level3 //3 prefix
    }

    [NetSerializable, Serializable]
    public enum ParticleAcceleratorPowerState
    {
        Standby = ParticleAcceleratorVisualState.Powered,
        Level0 = ParticleAcceleratorVisualState.Level0,
        Level1 = ParticleAcceleratorVisualState.Level1,
        Level2 = ParticleAcceleratorVisualState.Level2,
        Level3 = ParticleAcceleratorVisualState.Level3
    }

    public enum ParticleAcceleratorVisualLayers
    {
        Base,
        Unlit
    }

    [NetSerializable, Serializable]
    public class ParticleAcceleratorDataUpdateMessage : BoundUserInterfaceMessage
    {
        public bool Assembled;
        public bool Enabled;
        public ParticleAcceleratorPowerState State;
        public int PowerDraw;

        //dont need a bool for the controlbox because... this is sent to the controlbox :D
        public bool EmitterLeftExists;
        public bool EmitterCenterExists;
        public bool EmitterRightExists;
        public bool PowerBoxExists;
        public bool FuelChamberExists;
        public bool EndCapExists;

        public bool InterfaceBlock;
        public ParticleAcceleratorPowerState MaxLevel;
        public bool WirePowerBlock;

        public ParticleAcceleratorDataUpdateMessage(bool assembled, bool enabled, ParticleAcceleratorPowerState state, int powerDraw, bool emitterLeftExists, bool emitterCenterExists, bool emitterRightExists, bool powerBoxExists, bool fuelChamberExists, bool endCapExists, bool interfaceBlock, ParticleAcceleratorPowerState maxLevel, bool wirePowerBlock)
        {
            Assembled = assembled;
            Enabled = enabled;
            State = state;
            PowerDraw = powerDraw;
            EmitterLeftExists = emitterLeftExists;
            EmitterCenterExists = emitterCenterExists;
            EmitterRightExists = emitterRightExists;
            PowerBoxExists = powerBoxExists;
            FuelChamberExists = fuelChamberExists;
            EndCapExists = endCapExists;
            InterfaceBlock = interfaceBlock;
            MaxLevel = maxLevel;
            WirePowerBlock = wirePowerBlock;
        }
    }

    [NetSerializable, Serializable]
    public class ParticleAcceleratorSetEnableMessage : BoundUserInterfaceMessage
    {
        public readonly bool Enabled;
        public ParticleAcceleratorSetEnableMessage(bool enabled)
        {
            Enabled = enabled;
        }
    }

    [NetSerializable, Serializable]
    public class ParticleAcceleratorSetPowerStateMessage : BoundUserInterfaceMessage
    {
        public readonly ParticleAcceleratorPowerState State;

        public ParticleAcceleratorSetPowerStateMessage(ParticleAcceleratorPowerState state)
        {
            State = state;
        }
    }

    [NetSerializable, Serializable]
    public enum ParticleAcceleratorControlBoxUiKey
    {
        Key
    }
}
