using Content.Shared.Clothing;
using Robust.Shared.GameStates;

namespace Content.Shared.Item;

/// <summary>
/// This is used for items that change your speed when they are held.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(HeldSpeedModifierSystem))]
public sealed partial class HeldSpeedModifierComponent : Component
{
    /// <summary>
    /// A multiplier applied to the walk speed.
    /// </summary>
    [DataField] [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float WalkModifier = 1.0f;

    /// <summary>
    /// A multiplier applied to the sprint speed.
    /// </summary>
    [DataField] [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float SprintModifier = 1.0f;

    /// <summary>
    /// If true, values from <see cref="ClothingSpeedModifierComponent"/> will attempted to be used before the ones in this component.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool MirrorClothingModifier = true;
}
