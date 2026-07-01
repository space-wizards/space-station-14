using Robust.Shared.Serialization;

namespace Content.Shared.Ame;

[Serializable, NetSerializable]
public enum AmeControllerVisuals
{
    DisplayState,
}

[Serializable, NetSerializable]
public enum AmeControllerState
{
    On,
    Warning,
    Critical,
    Fuck,
    Off,
}

[Serializable, NetSerializable]
public enum AmeShieldVisuals
{
    Core,
    CoreState
}

[Serializable, NetSerializable]
public enum AmeCoreState
{
    Off,
    Weak,
    Strong
}
