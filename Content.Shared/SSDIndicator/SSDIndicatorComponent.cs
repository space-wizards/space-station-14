using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.SSDIndicator;

/// <summary>
///     Shows status icon when player in SSD
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class SSDIndicatorComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    [AutoNetworkedField]
    public bool IsSSD = true;

    [DataField]
    public ProtoId<SsdIconPrototype> Icon = "SSDIcon";

    /// <summary>
    ///     When the entity should fall asleep
    /// </summary>
    [AutoNetworkedField, AutoPausedField]
    [Access(typeof(SSDIndicatorSystem))]
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan FallAsleepTime = TimeSpan.Zero;
}
