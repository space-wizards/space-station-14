using Content.Client.Charges.UI;
using Content.Client.Items;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;

namespace Content.Client.Charges;

public sealed partial class ChargesSystem : SharedChargesSystem
{
    private Dictionary<EntityUid, int> _lastCharges = new();
    private Dictionary<EntityUid, int> _tempLastCharges = new();

    public override void Initialize()
    {
        base.Initialize();
        Subs.ItemStatus<LimitedChargesComponent>(entity => new ChargeStatusControl(entity));
    }

    public override void Update(float frameTime)
    {
        // Technically this should probably be in frameupdate but no one will ever notice a tick of delay on this.
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        // Update recharging actions. Server doesn't actually care about this and it's a waste of performance, actions are immediate.
        var query = AllEntityQuery<AutoRechargeComponent, LimitedChargesComponent>();

        while (query.MoveNext(out var uid, out var recharge, out var charges))
        {
            var current = GetCurrentCharges((uid, charges, recharge));

            if (!_lastCharges.TryGetValue(uid, out var last) || current != last)
            {
                UpdateChargeVisuals((uid, charges, recharge));
            }

            _tempLastCharges[uid] = current;
        }

        _lastCharges.Clear();

        foreach (var (uid, value) in _tempLastCharges)
        {
            _lastCharges[uid] = value;
        }

        _tempLastCharges.Clear();
    }
}
