using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Popups;

/// <summary>
/// Shows a popup to the entity after receiving certain damage types.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(PopupOnDamageSystem))]
public sealed partial class PopupOnDamageComponent : Component
{
    /// <summary>
    /// Key - damage type. Value - damage popup data..
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<DamageTypePrototype>, DamagePopupData> Popups;

    /// <summary>
    /// Cooldown between popups.
    /// </summary>
    [DataField]
    public TimeSpan PopupsCooldown = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Next time we can show the entity a popup.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextPopupTime = TimeSpan.Zero;
}

/// <summary>
/// Damage popup data.
/// </summary>
[DataDefinition, Serializable]
public partial struct DamagePopupData
{
    /// <summary>
    /// Text of the popup.
    /// </summary>
    [DataField(required: true)]
    public string Popup;

    /// <summary>
    /// Type of the popup.
    /// </summary>
    [DataField(required: true)]
    public PopupType Type;

    /// <summary>
    /// Damage threshold of the popup.
    /// </summary>
    [DataField]
    public FixedPoint2? Threshold;
}
