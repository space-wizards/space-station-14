namespace Content.Shared.StatusIcon;

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

/// <summary>
/// This component just displays status icons. That's it.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedStatusIconSystem))]
public sealed partial class DisplayStatusIconComponent : Component
{
    /// <summary>
    /// Status icons that will be displayed
    /// </summary>
    [DataField, AutoNetworkedField, AlwaysPushInheritance]
    public List<ProtoId<StatusIconPrototype>> Icons;
}
