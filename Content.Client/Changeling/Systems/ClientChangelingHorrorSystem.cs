using Content.Shared.Alert.Components;
using Content.Shared.Changeling.Components;
using Content.Shared.Changeling.Systems;
using Robust.Shared.Timing;

namespace Content.Client.Changeling.Systems;
/// <summary>
/// On the client side, we only handle the remaining time alert.
/// </summary>
public sealed partial class ClientChangelingHorrorSystem : ChangelingHorrorSystem
{
    [Dependency] private IGameTiming _timing = default!;

    [SubscribeLocalEvent]
    private void OnGetCounterAmount(Entity<ChangelingHorrorComponent> ent, ref GetGenericAlertCounterAmountEvent args)
    {
        if (ent.Comp.TimeAlert != args.Alert)
        {
            return;
        }

        // do maths
        var time = Math.Max((ent.Comp.EndTime - _timing.CurTime).TotalSeconds, 0d);
        args.Amount = (int)time;
    }
}
