using Content.Client.Explosion.UI;
using Content.Client.Items;
using Content.Shared.Explosion;

namespace Content.Client.Explosion;

/// <summary>
/// Wires up item status logic for <see cref="TimerTriggerItemStatusComponent"/>.
/// </summary>
/// <seealso cref="TimerTriggerStatusControl"/>
public sealed class TimerTriggerItemStatusSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        Subs.ItemStatus<TimerTriggerItemStatusComponent>(
            entity => new TimerTriggerStatusControl(entity));
    }
}
