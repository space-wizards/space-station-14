namespace Content.Shared.Atmos.Rotting;

/// <summary>
/// Entities inside this container will rot at a faster pace, e.g. a grave
/// </summary>
[RegisterComponent]
public sealed partial class ProRottingContainerComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float DecayModifier = 3f;
}

