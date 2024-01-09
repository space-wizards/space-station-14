namespace Content.Shared.Damage.Components;

/// <summary>
/// Applies stamina damage when embeds in an entity.
/// </summary>
[RegisterComponent]
public sealed partial class StaminaDamageOnEmbedComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("damage")]
    public float Damage = 10f;
}
