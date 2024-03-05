using Content.Shared.Clothing;
using Robust.Shared.GameStates;

namespace Content.Shared.Item;

/// <summary>
/// This is used for items that change your speed when they are held.
/// </summary>
/// <remarks>
/// This is separate from <see cref="ClothingSpeedModifierComponent"/> because things like boots increase/decrease speed when worn, but
/// shouldn't do that when just held in hand.
/// </remarks>
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
