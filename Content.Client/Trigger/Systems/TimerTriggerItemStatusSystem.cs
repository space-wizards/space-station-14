using Content.Client.Items;
using Content.Client.Trigger.UI;
using Content.Shared.Trigger.Components;

namespace Content.Client.Trigger.Systems;

/// <summary>
/// Wires up item status logic for timer triggers using <see cref="TimerTriggerComponent"/> state.
/// </summary>
/// <seealso cref="TimerTriggerStatusControl"/>
public sealed class TimerTriggerItemStatusSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        Subs.ItemStatus<TimerTriggerComponent>(
            entity => new TimerTriggerStatusControl(entity));
    }
}
