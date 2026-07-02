using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Objective condition that requires every member of command ("dungeon boss") to be dead.
/// Progress is the fraction of living command staff that have been killed.
/// </summary>
[RegisterComponent, Access(typeof(KillAllHeadsConditionSystem))]
public sealed partial class KillAllHeadsConditionComponent : Component;
