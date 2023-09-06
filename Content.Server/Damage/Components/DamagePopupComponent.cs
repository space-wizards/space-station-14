using Content.Server.Damage.Systems;

namespace Content.Server.Damage.Components;

[RegisterComponent, Access(typeof(DamagePopupSystem))]
public sealed partial class DamagePopupComponent : Component
{
    /// <summary>
    /// Enum that will be used to determine the type of damage popup displayed.
    /// </summary>
    [DataField("damagePopupType")] [ViewVariables(VVAccess.ReadWrite)]
    public DamagePopupType Type = DamagePopupType.Combined;
}
public enum DamagePopupType
{
    Combined,
    Total,
    Delta,
    Hit,
};
