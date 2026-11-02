using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Components;

/// <summary>
/// Adds a heat blur distortion effect anchored to an entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AnchoredBlurComponent : Component
{
    /// <summary>
    /// Size of the heat blur distortion area in tiles.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public Vector2 Size = new(0.5f, 0.5f);

    /// <summary>
    /// Offset of the heat blur area's center in tiles from the entity's position.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public Vector2 Offset = new(0f, 0f);

    /// <summary>
    /// Strength of the heat blur distortion effect (0 to 1).
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float Strength = 1f;

    /// <summary>
    /// Target strength, some systems use it to get maximum target strength of shader when entity works at full strength
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float TargetStrength = 1f;
}
