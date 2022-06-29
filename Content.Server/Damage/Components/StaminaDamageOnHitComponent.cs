namespace Content.Server.Damage.Components;

[RegisterComponent]
public sealed class StaminaDamageOnHitComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("damage")]
    public float Damage = 55f;
}
