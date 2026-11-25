using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that a changeling has obtained the highest amount of unique devours out of all other changelings.
/// Checks against identities stored on the mind.
/// </summary>
[RegisterComponent, Access(typeof(ChangelingObjectiveSystem))]
public sealed partial class ChangelingDevourMostConditionComponent : Component;
