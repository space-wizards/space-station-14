using Content.Client.Items;
using Content.Client.Trigger.UI;
using Content.Shared.Item;
using Content.Shared.Trigger.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Trigger.Systems;

/// <summary>
/// Wires up item status logic for timer triggers using <see cref="TimerTriggerComponent"/> state.
/// </summary>
/// <seealso cref="TimerTriggerStatusControl"/>
public sealed partial class TimerTriggerItemStatusSystem : EntitySystem
{
    public ProtoId<ItemStatusPrototype> TimerTriggerItemStatus = "TimerTrigger";

    public override void Initialize()
    {
        base.Initialize();
        Subs.ItemStatus<TimerTriggerComponent>(entity => new TimerTriggerStatusControl(entity), TimerTriggerItemStatus);
    }
}
