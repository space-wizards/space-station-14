namespace Content.Server.Objectives.Components;

/// <summary>
/// Sets this objective's target to the one given in <see cref="TargetOverrideComponent"/>, if the entity has it.
/// If not it will be random.
/// This component needs to be added to objective entity itself.
/// </summary>
[RegisterComponent]
public sealed partial class TargetObjectiveOverrideComponent : Component;
