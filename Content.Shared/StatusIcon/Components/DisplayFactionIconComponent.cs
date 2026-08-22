namespace Content.Shared.StatusIcon;

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

/// <summary>
/// This component just displays faction icons. That's it.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedStatusIconSystem))]
public sealed partial class DisplayFactionIconComponent : Component
{
    /// <summary>
    /// Factions icons that will be displayed
    /// </summary>
    [DataField(required: true), AutoNetworkedField, AlwaysPushInheritance]
    public List<ProtoId<FactionIconPrototype>> Icons = [];
}
