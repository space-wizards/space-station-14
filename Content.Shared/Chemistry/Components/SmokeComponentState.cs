using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Components;

[Serializable, NetSerializable]
public sealed class SmokeComponentState : ComponentState
{
    public Color Color;
}
