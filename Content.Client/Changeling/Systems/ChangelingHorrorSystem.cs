using Content.Shared.Alert.Components;
using Content.Shared.Changeling.Components;
using Content.Shared.Changeling.Systems;
using Robust.Shared.Timing;

namespace Content.Client.Changeling.Systems;
/// <summary>
/// On the client side, we only handle the remaining time alert.
/// </summary>
public sealed partial class ChangelingHorrorSystem : SharedChangelingHorrorSystem
{
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChangelingHorrorComponent, GetGenericAlertCounterAmountEvent>(OnGetCounterAmount);
    }

    private void OnGetCounterAmount(Entity<ChangelingHorrorComponent> ent, ref GetGenericAlertCounterAmountEvent args)
    {
        if (ent.Comp.TimeAlert != args.Alert)
        {
            return;
        }

        // do maths
        var time = Math.Max((ent.Comp.TimeBudget - (_timing.CurTime - ent.Comp.InitialTime)).TotalSeconds, 0d);
        args.Amount = (int)time;
    }
}
