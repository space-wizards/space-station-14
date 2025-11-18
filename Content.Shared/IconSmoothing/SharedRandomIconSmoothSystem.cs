using Robust.Shared.Serialization;

namespace Content.Shared.IconSmoothing;

public abstract class SharedRandomIconSmoothSystem : EntitySystem
{
}
[Serializable, NetSerializable]
public enum RandomIconSmoothState : byte
{
    State
}
