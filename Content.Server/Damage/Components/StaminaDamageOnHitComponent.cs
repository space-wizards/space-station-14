using Robust.Shared.Audio;

namespace Content.Server.Damage.Components;

[RegisterComponent]
public sealed class StaminaDamageOnHitComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("damage")]
    public float Damage = 30f;

    [DataField("examineGroup")]
    public string ExamineGroup = "melee";

    [DataField("examinePriority")]
    public int ExaminePriority = 0;
}
