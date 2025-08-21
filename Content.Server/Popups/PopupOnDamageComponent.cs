using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Popups;

/// <summary>
///
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(PopupOnDamageSystem))]
public sealed partial class PopupOnDamageComponent : Component
{
    /// <summary>
    ///
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<DamageTypePrototype>, DamagePopupData> Popups;

    /// <summary>
    ///
    /// </summary>
    [DataField]
    public TimeSpan PopupsCooldown = TimeSpan.FromMinutes(1);

    /// <summary>
    ///
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextPopupTime = TimeSpan.Zero;
}

/// <summary>
///
/// </summary>
[DataDefinition, Serializable]
public partial struct DamagePopupData
{
    /// <summary>
    ///
    /// </summary>
    [DataField(required: true)]
    public string Popup;

    /// <summary>
    ///
    /// </summary>
    [DataField(required: true)]
    public PopupType Type;

    /// <summary>
    ///
    /// </summary>
    [DataField]
    public FixedPoint2? Threshold;
}
