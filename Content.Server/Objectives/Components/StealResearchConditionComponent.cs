using Content.Server.Objectives.Systems;
using Content.Shared.Ninja.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Objective condition that requires the player to be a ninja and have stolen at least a random number of technologies.
/// Requires <see cref="NumberObjectiveComponent"/> to function.
/// </summary>
[RegisterComponent, Access(typeof(NinjaConditionsSystem), typeof(SharedSpaceNinjaSystem))]
public sealed partial class StealResearchConditionComponent : Component
{
    [DataField("downloadedNodes"), ViewVariables(VVAccess.ReadWrite)]
    public HashSet<string> DownloadedNodes = new();
}
