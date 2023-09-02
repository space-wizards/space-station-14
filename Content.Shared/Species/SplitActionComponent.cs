using Content.Shared.Damage;
using Robust.Shared.Serialization;

namespace Content.Shared.SplitAction;

[RegisterComponent]
public sealed partial class SplitActionComponent : Component
{
    /// <summary>
    /// What will be spawned at the end of the action.
    /// </summary>
    [DataField("entityProduced", required: true)]
    public string EntityProduced = "";

    [DataField("actionPrototype", required: true)]
    public string ActionPrototype = "";

    /// <summary>
    /// How long will it take to split off.
    /// </summary>
    [DataField("splitTime", required: true), ViewVariables(VVAccess.ReadWrite)]
    public float splitTime = 0;

    /// <summary>
    /// How much damage the entity will take on splitting.
    /// </summary>
    [DataField("damage"), ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier Damage = new();

    /// <summary>
    /// The text that appears when attempting to split.
    /// </summary>
    [DataField("popupText")]
    public string PopupText = "diona-split-attempt";

    /// <summary>
    /// How long the entity will be stunned at the start and end of the doafter.
    /// </summary>
    [DataField("stunTime"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan StunTime = TimeSpan.Zero;
}
