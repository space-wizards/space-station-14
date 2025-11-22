using Content.Shared.Damage.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Damage.Components;

/// <summary>
/// An entity with this component will show a popup indicating the amount of damage taken.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(DamagePopupSystem))]
public sealed partial class DamagePopupComponent : Component
{
    /// <summary>
    /// Bool that will be used to determine if the popup type can be changed with a left click.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AllowTypeChange;

    /// <summary>
    /// Enum that will be used to determine the type of damage popup displayed.
    /// </summary>
    [DataField("damagePopupType"), AutoNetworkedField]
    public DamagePopupType Type = DamagePopupType.Combined;
}

[Serializable, NetSerializable]
public enum DamagePopupType : byte
{
    Combined,
    Total,
    Delta,
    Hit,
};
