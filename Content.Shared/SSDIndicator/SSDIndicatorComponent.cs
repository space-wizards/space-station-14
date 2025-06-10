using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.SSDIndicator;

/// <summary>
///     Shows status icon when player in SSD
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class SSDIndicatorComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public bool IsSSD = true;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public ProtoId<SsdIconPrototype> Icon = "SSDIcon";

    /// <summary>
    ///     When the entity should fall asleep
    /// </summary>
    [DataField, AutoPausedField, Access(typeof(SSDIndicatorSystem))]
    public TimeSpan FallAsleepTime = TimeSpan.Zero;

    /// <summary>
    ///     Required to don't remove forced sleep from other sources
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public bool ForcedSleepAdded = false;
}
