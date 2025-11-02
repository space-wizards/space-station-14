using Robust.Shared.Audio;

namespace Content.Shared.Damage.Components;

[RegisterComponent]
public sealed partial class StaminaDamageOnHitComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("damage")]
    public float Damage = 30f;

    [DataField("sound")]
    public SoundSpecifier? Sound;

    /// <summary>
    /// LocId for message that will be shown on detailed examine.
    /// </summary>
    [DataField]
    public LocId ExamineMessage = "stamina-damage-examine-hit";
}
