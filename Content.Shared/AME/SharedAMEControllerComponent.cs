using Robust.Shared.Serialization;

namespace Content.Shared.AME
{
    [Virtual]
    public class SharedAMEControllerComponent : Component
    {
        [Serializable, NetSerializable]
        public sealed class AMEControllerBoundUserInterfaceState : BoundUserInterfaceState
        {
            public readonly bool HasPower;
            public readonly bool IsMaster;
            public readonly bool Injecting;
            public readonly bool HasFuelJar;
            public readonly int FuelAmount;
            public readonly int InjectionAmount;
            public readonly int CoreCount;

            public AMEControllerBoundUserInterfaceState(bool hasPower, bool isMaster, bool injecting, bool hasFuelJar, int fuelAmount, int injectionAmount, int coreCount)
            {
                HasPower = hasPower;
                IsMaster = isMaster;
                Injecting = injecting;
                HasFuelJar = hasFuelJar;
                FuelAmount = fuelAmount;
                InjectionAmount = injectionAmount;
                CoreCount = coreCount;
            }
        }

        [Serializable, NetSerializable]
        public sealed class UiButtonPressedMessage : BoundUserInterfaceMessage
        {
            public readonly UiButton Button;

            public UiButtonPressedMessage(UiButton button)
            {
                Button = button;
            }
        }

        [Serializable, NetSerializable]
        public enum AMEControllerUiKey
        {
            Key
        }

        public enum UiButton
        {
            Eject,
            ToggleInjection,
            IncreaseFuel,
            DecreaseFuel,
        }

        [Serializable, NetSerializable]
        public enum AMEControllerVisuals
        {
            DisplayState,
        }
    }
}
