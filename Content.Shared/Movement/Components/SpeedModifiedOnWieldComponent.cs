using Content.Shared.Wieldable;

namespace Content.Shared.Movement.Components;

/// <summary>
/// Modifies the speed when an entity with this component is wielded.
/// </summary>
[RegisterComponent, Access(typeof(WieldableSystem))]
public sealed partial class SpeedModifiedOnWieldComponent : Component
{
    /// <summary>
    /// How much the wielder's sprint speed is modified when the component owner is wielded.
    /// </summary>
    [DataField]
    public float SprintModifier = 1f;

    /// <summary>
    /// How much the wielder's sprint speed is modified when the component owner is wielded.
    /// </summary>
    [DataField]
    public float WalkModifier = 1f;
}
