using Content.Shared.Actions;
using Content.Shared.Chemistry.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Changeling.Components;

/// <summary>
/// Allows the changeling to silently inject a solution into a nearby target.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ChangelingStingAbilityComponent : Component
{
    /// <summary>
    /// Whether the sting can only be used on living targets.
    /// </summary>
    [DataField]
    public bool RequireAlive = true;

    /// <summary>
    /// The reagents to inject into the target.
    /// </summary>
    [DataField(required: true)]
    public Solution InjectSolution = new();
}

/// <summary>
/// Action event for changeling stings, raised on the ability when used.
/// </summary>
public sealed partial class ChangelingStingActionEvent : EntityTargetActionEvent;
