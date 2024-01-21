using Content.Server.Objectives.Systems;
using Content.Shared.Ninja.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that the player is a ninja and has called in a threat.
/// </summary>
[RegisterComponent, Access(typeof(NinjaConditionsSystem), typeof(SharedSpaceNinjaSystem))]
public sealed partial class TerrorConditionComponent : Component
{
    /// <summary>
    /// Whether the comms console has been hacked
    /// </summary>
    [DataField("calledInThreat"), ViewVariables(VVAccess.ReadWrite)]
    public bool CalledInThreat;
}
