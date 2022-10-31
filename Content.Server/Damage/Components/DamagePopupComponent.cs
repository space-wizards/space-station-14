using Content.Server.Damage.Systems;

namespace Content.Server.Damage.Components;

[RegisterComponent, Access(typeof(DamagePopupSystem))]
public sealed class DamagePopupComponent : Component
{
    /// <summary>
    /// String will be used to determine the type of damage popup displayed.
    /// </summary>
    [DataField("damagePopupTypeString")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? DamagePopupTypeString;
}
