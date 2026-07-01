using Robust.Shared.Serialization;

namespace Content.Shared.Ame;

[Serializable, NetSerializable]
public sealed class AmeControllerBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly float CurrentPowerSupply;

    public AmeControllerBoundUserInterfaceState(float currentPowerSupply)
    {
        CurrentPowerSupply = currentPowerSupply;
    }
}

[Serializable, NetSerializable]
public sealed class AmeControllerEjectMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class AmeControllerToggleInjectionMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class AmeControllerIncreaseFuelMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class AmeControllerDecreaseFuelMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public enum AmeControllerUiKey
{
    Key
}
