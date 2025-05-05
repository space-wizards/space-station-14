using Content.Shared.Destructible;
using Content.Shared.Examine;
using Content.Shared.NameModifier.EntitySystems;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Delivery;

/// <summary>
/// System responsible for managing multipliers and logic for different delivery modifiers.
/// </summary>
public sealed partial class DeliveryModifierSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly NameModifierSystem _nameModifier = default!;
    [Dependency] private readonly SharedDeliverySystem _delivery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeliveryRandomMultiplierComponent, MapInitEvent>(OnRandomMultiplierMapInit);
        SubscribeLocalEvent<DeliveryRandomMultiplierComponent, GetDeliveryMultiplierEvent>(OnGetRandomMultiplier);

        SubscribeLocalEvent<DeliveryPriorityComponent, MapInitEvent>(OnPriorityMapInit);
        SubscribeLocalEvent<DeliveryPriorityComponent, DeliveryUnlockedEvent>(OnPriorityDelivered);
        SubscribeLocalEvent<DeliveryPriorityComponent, ExaminedEvent>(OnPriorityExamine);
        SubscribeLocalEvent<DeliveryPriorityComponent, GetDeliveryMultiplierEvent>(OnGetPriorityMultiplier);

        SubscribeLocalEvent<DeliveryFragileComponent, MapInitEvent>(OnFragileMapInit);
        SubscribeLocalEvent<DeliveryFragileComponent, BreakageEventArgs>(OnFragileBreakage);
        SubscribeLocalEvent<DeliveryFragileComponent, ExaminedEvent>(OnFragileExamine);
        SubscribeLocalEvent<DeliveryFragileComponent, GetDeliveryMultiplierEvent>(OnGetFragileMultiplier);
    }

    #region Random
    private void OnRandomMultiplierMapInit(Entity<DeliveryRandomMultiplierComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.CurrentMultiplierOffset = _random.NextFloat(ent.Comp.MinMultiplierOffset, ent.Comp.MaxMultiplierOffset);
        Dirty(ent);
    }

    private void OnGetRandomMultiplier(Entity<DeliveryRandomMultiplierComponent> ent, ref GetDeliveryMultiplierEvent args)
    {
        args.AdditiveMultiplier += ent.Comp.CurrentMultiplierOffset;
    }
    #endregion

    #region Priority
    private void OnPriorityMapInit(Entity<DeliveryPriorityComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.DeliverUntilTime = _timing.CurTime + ent.Comp.DeliveryTime;
        _delivery.UpdatePriorityVisuals(ent);
        Dirty(ent);
    }

    private void OnPriorityDelivered(Entity<DeliveryPriorityComponent> ent, ref DeliveryUnlockedEvent args)
    {
        if (ent.Comp.Expired)
            return;

        ent.Comp.Delivered = true;
        Dirty(ent);
    }

    private void OnPriorityExamine(Entity<DeliveryPriorityComponent> ent, ref ExaminedEvent args)
    {
        var trueName = _nameModifier.GetBaseName(ent.Owner);
        var timeLeft = ent.Comp.DeliverUntilTime - _timing.CurTime;

        if (ent.Comp.Delivered)
            args.PushMarkup(Loc.GetString("delivery-priority-delivered-examine", ("type", trueName)));
        else if (_timing.CurTime < ent.Comp.DeliverUntilTime)
            args.PushMarkup(Loc.GetString("delivery-priority-examine", ("type", trueName), ("time", timeLeft.ToString("mm\\:ss"))));
        else
            args.PushMarkup(Loc.GetString("delivery-priority-expired-examine", ("type", trueName)));
    }

    private void OnGetPriorityMultiplier(Entity<DeliveryPriorityComponent> ent, ref GetDeliveryMultiplierEvent args)
    {
        if (_timing.CurTime < ent.Comp.DeliverUntilTime)
            args.AdditiveMultiplier += ent.Comp.InTimeMultiplierOffset;
        else
            args.AdditiveMultiplier += ent.Comp.ExpiredMultiplierOffset;
    }
    #endregion

    #region Fragile
    private void OnFragileMapInit(Entity<DeliveryFragileComponent> ent, ref MapInitEvent args)
    {
        _delivery.UpdateBrokenVisuals(ent, true);
    }

    private void OnFragileBreakage(Entity<DeliveryFragileComponent> ent, ref BreakageEventArgs args)
    {
        ent.Comp.Broken = true;
        _delivery.UpdateBrokenVisuals(ent, true);
        Dirty(ent);
    }

    private void OnFragileExamine(Entity<DeliveryFragileComponent> ent, ref ExaminedEvent args)
    {
        var trueName = _nameModifier.GetBaseName(ent.Owner);

        if (ent.Comp.Broken)
            args.PushMarkup(Loc.GetString("delivery-fragile-broken-examine", ("type", trueName)));
        else
            args.PushMarkup(Loc.GetString("delivery-fragile-examine", ("type", trueName)));
    }

    private void OnGetFragileMultiplier(Entity<DeliveryFragileComponent> ent, ref GetDeliveryMultiplierEvent args)
    {
        if (ent.Comp.Broken)
            args.AdditiveMultiplier += ent.Comp.BrokenMultiplierOffset;
        else
            args.AdditiveMultiplier += ent.Comp.IntactMultiplierOffset;
    }
    #endregion

    #region Update Loops
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdatePriorty(frameTime);
    }

    private void UpdatePriorty(float frameTime)
    {
        var priorityQuery = EntityQueryEnumerator<DeliveryPriorityComponent>();
        var curTime = _timing.CurTime;

        while (priorityQuery.MoveNext(out var uid, out var priorityData))
        {
            if (priorityData.Expired || priorityData.Delivered)
                continue;

            if (priorityData.DeliverUntilTime < curTime)
            {
                priorityData.Expired = true;
                _delivery.UpdatePriorityVisuals((uid, priorityData));
                Dirty(uid, priorityData);

                var ev = new DeliveryPriorityExpiredEvent();
                RaiseLocalEvent(uid, ev);
            }
        }
    }
    #endregion
}

/// <summary>
/// Gets raised on a priority delivery when it's timer expires.
/// </summary>
[Serializable, NetSerializable]
public readonly record struct DeliveryPriorityExpiredEvent;
