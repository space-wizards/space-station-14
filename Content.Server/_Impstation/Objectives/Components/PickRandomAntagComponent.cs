using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Sets the target for <see cref="TargetObjectiveComponent"/> to a random antagonist.
/// If there are none it will fallback to any person.
/// </summary>
[RegisterComponent]
public sealed partial class PickRandomAntagComponent : Component;
