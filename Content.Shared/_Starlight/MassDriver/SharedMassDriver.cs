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
    public float MaxThrowSpeed;
    public float MaxThrowDistance;
    public float MinThrowSpeed;
    public float MinThrowDistance;
    public float CurrentThrowSpeed;
    public float CurrentThrowDistance;
    public MassDriverMode CurrentMassDriverMode;
}