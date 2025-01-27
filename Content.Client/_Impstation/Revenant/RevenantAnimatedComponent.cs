using Robust.Client.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Client.Revenant;

[RegisterComponent, NetworkedComponent]
public sealed partial class RevenantAnimatedComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float Accumulator = 0f;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public Entity<PointLightComponent>? LightOverlay;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public Color LightColor = Color.MediumPurple;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float LightRadius = 2f;
}