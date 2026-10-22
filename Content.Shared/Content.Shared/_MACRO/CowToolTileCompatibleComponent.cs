using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._MACRO.Tools.Components;

/// <summary>
/// Used on an entity with <see cref="ToolTileCompatibleComponent"/> to override its Delay
/// if the user has  <see cref="CowToolProficiencyComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedToolSystem))]
public sealed partial class CowToolTileCompatibleComponent : Component
{
    /// <summary>
    /// The time it takes to modify the tile.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Delay = TimeSpan.FromSeconds(0.5);
}
