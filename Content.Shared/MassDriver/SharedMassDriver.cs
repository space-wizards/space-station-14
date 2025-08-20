using Robust.Shared.Serialization;

namespace Content.Shared._Starlight.MassDriver;

[Serializable, NetSerializable]
public enum MassDriverConsoleUiKey : byte
{
    Key
}

[NetSerializable]
[Serializable]
public enum MassDriverMode : byte
{
    Auto,
    Manual
}

[NetSerializable]
[Serializable]
public enum MassDriverVisuals : byte
{
    Launching
}

[NetSerializable]
[Serializable]
public sealed class MassDriverUiState : BoundUserInterfaceState
{
    public float MaxThrowSpeed = 1f;
    public float MaxThrowDistance = 1f;
    public float MinThrowSpeed = 0.0f;
    public float MinThrowDistance = 0.0f;
    public float CurrentThrowSpeed = 0.5f;
    public float CurrentThrowDistance = 0.5f;
    public MassDriverMode CurrentMassDriverMode = MassDriverMode.Auto;

    public bool MassDriverLinked;
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