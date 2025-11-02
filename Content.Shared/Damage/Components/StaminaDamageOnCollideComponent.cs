using Robust.Shared.Audio;

namespace Content.Shared.Damage.Components;

/// <summary>
/// Applies stamina damage when colliding with an entity.
/// </summary>
[RegisterComponent]
public sealed partial class StaminaDamageOnCollideComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("damage")]
    public float Damage = 55f;

    [DataField("sound")]
    public SoundSpecifier? Sound;

    /// <summary>
    /// LocId for message that will be shown on detailed examine.
    /// </summary>
    [DataField]
    public LocId ExamineMessage = "stamina-damage-examine-collide";
}
