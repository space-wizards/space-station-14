using Robust.Shared.GameStates;

namespace Content.Shared.Silicons.Laws.Components;

/// <summary>
/// This is used for an entity that grants a special "obey" law when emagge.d
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSiliconLawSystem))]
[AutoGenerateComponentState]
public sealed partial class EmagSiliconLawComponent : Component
{
    /// <summary>
    /// The name of the person who emagged this law provider.
    /// </summary>
    [DataField("ownerName"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public string? OwnerName;

    /// <summary>
    /// Does the panel need to be open to EMAG this law provider.
    /// </summary>
    [DataField("requireOpenPanel"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public bool RequireOpenPanel = true;
}
