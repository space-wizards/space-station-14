using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.SSDIndicator;

/// <summary>
///     Shows status icon when player in SSD
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class SSDIndicatorComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public bool IsSSD = true;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public ProtoId<SsdIconPrototype> Icon = "SSDIcon";

    /// <summary>
    /// imp. used to keep track of whether or not a body has *ever* had a player attached to it, after the player has left. 
    /// used in GhostSystem.GetPlayerWarps
    /// </summary>
    [AutoNetworkedField]
    public bool HasHadPlayer;
}
