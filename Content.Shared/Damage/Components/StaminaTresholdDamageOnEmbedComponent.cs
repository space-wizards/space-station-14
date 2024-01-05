namespace Content.Shared.Damage.Components;

/// <summary>
/// Applies stamina treshold damage when colliding with an entity.
/// </summary>
[RegisterComponent]
public sealed partial class StaminaTresholdDamageOnEmbedComponent : Component
{
    /// <summary>
    /// What to multiply crit treshold by.
    /// When added this scales crit treshold, but not stamina damage to increasing your stamcrit chance.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("modifier")]
    public float Modifier = 0.8f;
}
