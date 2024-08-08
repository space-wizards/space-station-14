namespace Content.Server.Chemistry.Components;

/// <summary>
/// Used for embeddable entities that should try to inject a
/// contained solution into a target when triggered.
/// </summary>
[RegisterComponent]
public sealed partial class SolutionInjectOnTriggerComponent : BaseSolutionInjectOnEventComponent { }
