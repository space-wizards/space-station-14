using Content.Server.Atmos.Components;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
///     Adjusts fire stacks on trigger, optionally setting them on fire as well.
///     Requires <see cref="FlammableComponent"/> to ignite the target.
/// </summary>
/// <seealso cref="IgniteOnTriggerComponent"/>
[RegisterComponent]
public sealed partial class CombustOnTriggerComponent : BaseXOnTriggerComponent
{
    [DataField]
    public float FireStacks;
}
