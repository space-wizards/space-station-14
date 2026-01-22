using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Adjusts fire stacks on trigger, optionally setting them on fire as well.
/// Requires <see cref="FlammableComponent"/> to ignite the target.
/// If TargetUser is true they will have their firestacks adjusted instead.
/// </summary>
/// <seealso cref="IgniteOnTriggerComponent"/>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FireStackOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// How many fire stacks to add or remove.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float FireStacks;

    /// <summary>
    /// If true, the target will be set on fire if it isn't already.
    /// If false does nothing.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool DoIgnite = true;
}
