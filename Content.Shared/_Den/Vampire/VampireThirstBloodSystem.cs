using Content.Shared.Alert;
using Content.Shared.Rejuvenate;
using Robust.Shared.Timing;
using Content.Shared._Den.Vampire.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Network;

namespace Content.Shared._Den.Vampire;

public sealed class VampireThirstBloodSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly HungerSystem _hungerSystem = default!;
    [Dependency] private readonly ThirstSystem _thirstSystem = default!;
    [Dependency] private readonly INetManager _netMan = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampireThirstBloodComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<VampireThirstBloodComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<VampireThirstBloodComponent, RejuvenateEvent>(OnRejuvenate);
    }

    private void OnMapInit(EntityUid uid, VampireThirstBloodComponent component, MapInitEvent args)
    {
        _alerts.ShowAlert(uid, component.ThirstBloodAlert);

        component.NextUpdate = _timing.CurTime + component.UpdateInterval;
    }

    private void OnShutdown(EntityUid uid, VampireThirstBloodComponent component, ComponentShutdown args)
    {
        _alerts.ClearAlert(uid, component.ThirstBloodAlert);
    }

    private void OnRejuvenate(EntityUid uid, VampireThirstBloodComponent component, RejuvenateEvent args)
    {
        component.CurrentThirstBlood = component.MaxThirstBlood;
        Dirty(uid, component);
    }

    public void ModifyThirstBlood(Entity<VampireThirstBloodComponent?> ent, float value)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (_netMan.IsClient && !IsClientSide(ent))
            return; // не помогло, реши

        ent.Comp.CurrentThirstBlood = Math.Clamp(ent.Comp.CurrentThirstBlood + value,
            ent.Comp.MinThirstBlood,
            ent.Comp.SoftCapMaximum ? Int32.MaxValue : ent.Comp.MaxThirstBlood);
    }

    private void UpdateHungerThirst(EntityUid uid, VampireThirstBloodComponent thirstblood)
    {

        var bloodPercent = Math.Clamp(thirstblood.CurrentThirstBlood / thirstblood.MaxThirstBlood, 0f, 1f);
        var modifier = bloodPercent - thirstblood.NeutralBloodPercent;
        var ticksToZero = thirstblood.MaxThirstBlood / thirstblood.ThirstBloodDecay;


        Logger.Info($"bloodPercent {bloodPercent}.");


        if (TryComp<HungerComponent>(uid, out var hunger))
        {
            var hungerDelta =  modifier * hunger.Thresholds[HungerThreshold.Overfed] / ticksToZero * thirstblood.HungerRateMultiplier;
            _hungerSystem.ModifyHunger(uid, hungerDelta, hunger);

            var currentHunger = _hungerSystem.GetHunger(hunger);
            Logger.Info($"Entity {uid}: Hunger modified by {hungerDelta}, current hunger = {currentHunger}.");
        }

        if (TryComp<ThirstComponent>(uid, out var thirst))
        {
            var thirstDelta = modifier * thirst.ThirstThresholds[ThirstThreshold.OverHydrated] / ticksToZero * thirstblood.ThirstRateMultiplier;
            _thirstSystem.ModifyThirst(uid, thirst, thirstDelta);
            Logger.Info($"Entity {uid}: Thirst modified by {thirstDelta}, current hunger = {thirst.CurrentThirst}.");
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<VampireThirstBloodComponent>();
        while (query.MoveNext(out var uid, out var thirstblood))
        {
            if (_timing.CurTime < thirstblood.NextUpdate)
                continue;

            thirstblood.NextUpdate = _timing.CurTime + thirstblood.UpdateInterval;
            ModifyThirstBlood(uid, -thirstblood.ThirstBloodDecay);
            UpdateHungerThirst(uid, thirstblood);
        }
    }
}
