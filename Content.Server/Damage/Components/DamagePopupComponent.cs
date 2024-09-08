using Content.Server.Damage.Systems;

namespace Content.Server.Damage.Components;

[RegisterComponent, Access(typeof(DamagePopupSystem))]
public sealed partial class DamagePopupComponent : Component
{
    /// <summary>
    /// Bool that will be used to determine if the popup type can be changed with a left click.
    /// </summary>
    [DataField("allowTypeChange")] [ViewVariables(VVAccess.ReadWrite)]
    public bool AllowTypeChange = false;
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
