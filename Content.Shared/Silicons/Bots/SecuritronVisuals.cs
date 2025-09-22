using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.Bots;

[Serializable, NetSerializable]
public enum SecuritronVisuals
{
    State,
}

[Serializable, NetSerializable]
public enum SecuritronVisualState
{
    Online,
    Combat,
}
