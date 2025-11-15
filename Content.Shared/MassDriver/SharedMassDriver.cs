using Robust.Shared.Serialization;

namespace Content.Shared.MassDriver;

[Serializable, NetSerializable]
public enum MassDriverConsoleUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public enum MassDriverMode : byte
{
    Auto,
    Manual
}

[Serializable, NetSerializable]
public enum MassDriverVisuals : byte
{
    Launching
}

[Serializable, NetSerializable]
public sealed class MassDriverComponentState : ComponentState
{
    public float MaxThrowSpeed;
    public float MaxThrowDistance;
    public float MinThrowSpeed;
    public float MinThrowDistance;
    public float CurrentThrowSpeed;
    public float CurrentThrowDistance;
    public MassDriverMode CurrentMassDriverMode = MassDriverMode.Auto;
    public NetEntity? Console = null;
    public bool Hacked;
}

[Serializable, NetSerializable]
public sealed class MassDriverUpdateUIMessage : BoundUserInterfaceMessage
{
    public MassDriverComponentState State;

    public MassDriverUpdateUIMessage(MassDriverComponentState state) => State = state;
}

[Serializable, NetSerializable]
public sealed class MassDriverModeMessage : BoundUserInterfaceMessage
{
    public MassDriverMode Mode;

    public MassDriverModeMessage(MassDriverMode mode) => Mode = mode;
}

[Serializable, NetSerializable]
public sealed class MassDriverLaunchMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class MassDriverThrowSpeedMessage : BoundUserInterfaceMessage
{
    public float Speed;

    public MassDriverThrowSpeedMessage(float speed) => Speed = speed;
}

[Serializable, NetSerializable]
public sealed class MassDriverThrowDistanceMessage : BoundUserInterfaceMessage
{
    public float Distance;

    public MassDriverThrowDistanceMessage(float distance) => Distance = distance;
}

[Serializable, NetSerializable]
public enum SecurityWireActionKey : byte
{
    Key,
    Status,
    Pulsed,
    PulseCancel
}
