using Robust.Shared.Serialization;

namespace Content.Shared._Starlight.MassDriver;

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