using Content.Client.Anomaly.UI;
using Content.Client.Items;
using Content.Shared.Anomaly;

namespace Content.Client.Anomaly;

/// <summary>
/// Wires up item status logic for <see cref="AnomalyItemStatusComponent"/>.
/// </summary>
/// <seealso cref="AnomalyStatusControl"/>
public sealed class AnomalyItemStatusSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        Subs.ItemStatus<AnomalyItemStatusComponent>(
            entity => new AnomalyStatusControl(entity));
    }
}
