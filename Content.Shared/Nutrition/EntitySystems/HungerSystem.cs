using System.Diagnostics.CodeAnalysis;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Rejuvenate;
using Content.Shared.StatusIcon;
using Robust.Shared.Timing;
using Content.Shared.Alert;

namespace Content.Shared.Nutrition.EntitySystems;

public sealed class HungerSystem : SatiationSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;


    public override void Initialize()
    {
         Icons = new (string, StatusIconPrototype?)[] {
            ("HungerIconOverfed", null),
            ("HungerIconPeckish", null),
            ("HungerIconStarving", null)
        };
        AlertThresholds = new()
        {
            { SatiationThreashold.Concerned, AlertType.Peckish },
            { SatiationThreashold.Desperate, AlertType.Starving },
            { SatiationThreashold.Dead, AlertType.Starving }
        };
        AlertCategory = Alert.AlertCategory.Hunger;

        base.Initialize();

        SubscribeLocalEvent<HungerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<HungerComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<HungerComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
        SubscribeLocalEvent<HungerComponent, RejuvenateEvent>(OnRejuvenate);
    }

    private void OnMapInit(EntityUid uid, HungerComponent component, MapInitEvent args)
    {
        component.NextUpdateTime = _timing.CurTime;
        base.OnMapInit(uid, component.Satiation, args);
    }

    private void OnShutdown(EntityUid uid, HungerComponent component, ComponentShutdown args)
    {
        base.OnShutdown(uid, component.Satiation, args);
    }

    private void OnRefreshMovespeed(EntityUid uid, HungerComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        base.OnRefreshMovespeed(uid, component.Satiation, args);
    }

    private void OnRejuvenate(EntityUid uid, HungerComponent component, RejuvenateEvent args)
    {
        base.OnRejuvenate(uid, component.Satiation, args);
    }

    /// <summary>
    /// Adds to the current hunger of an entity by the specified value
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="amount"></param>
    /// <param name="component"></param>
    public void ModifyHunger(EntityUid uid, float amount, HungerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;
        base.ModifyNutrition(uid, amount, component.Satiation);
    }

    /// <summary>
    /// Sets the current hunger of an entity to the specified value
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="amount"></param>
    /// <param name="component"></param>
    public void SetHunger(EntityUid uid, float amount, HungerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;
        base.SetNutrition(uid, amount, component.Satiation);
        Dirty(uid, component);
    }

    private void UpdateCurrentThreshold(EntityUid uid, HungerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        base.UpdateCurrentThreshold(uid, component.Satiation);
        Dirty(uid, component);
    }

    private void DoNutritionThresholdEffects(EntityUid uid, HungerComponent? component = null, bool force = false)
    {
        if (!Resolve(uid, ref component))
            return;

        base.DoNutritionThresholdEffects(uid, component.Satiation, force);
    }

    private void DoContinuousNutritionEffects(EntityUid uid, HungerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        base.DoContinuousNutritionEffects(uid, component.Satiation);
    }

    /// <summary>
    /// Gets the hunger threshold for an entity based on the amount of food specified.
    /// If a specific amount isn't specified, just uses the current hunger of the entity
    /// </summary>
    /// <param name="component"></param>
    /// <param name="hunger"></param>
    /// <returns></returns>
    public SatiationThreashold GetHungerThreshold(HungerComponent component, float? hunger = null)
    {
        return base.GetNutritionThreshold(component.Satiation, hunger);
    }

    /// <summary>
    /// A check that returns if the entity is below a hunger threshold.
    /// </summary>
    public bool IsHungerBelowState(EntityUid uid, SatiationThreashold threshold, float? food = null, HungerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false; // It's never going to go hungry, so it's probably fine to assume that it's not... you know, hungry.

        return base.IsNutritionBelowState(uid, component.Satiation, threshold, food);
    }

    public bool TryGetStatusIconPrototype(HungerComponent component, [NotNullWhen(true)] out StatusIconPrototype? prototype)
    {
        switch (component.Satiation.CurrentThreshold)
        {
            case SatiationThreashold.Full:
                prototype = Icons![0].Item2;
                break;
            case SatiationThreashold.Concerned:
                prototype = Icons![1].Item2;
                break;
            case SatiationThreashold.Desperate:
                prototype = Icons![2].Item2;
                break;
            default:
                prototype = null;
                break;
        }

        return prototype != null;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<HungerComponent>();
        while (query.MoveNext(out var uid, out var hunger))
        {
            if (_timing.CurTime < hunger.NextUpdateTime)
                continue;
            hunger.NextUpdateTime = _timing.CurTime + hunger.UpdateRate;

            ModifyHunger(uid, -hunger.Satiation.ActualDecayRate, hunger);
            DoContinuousNutritionEffects(uid, hunger);
        }
    }
}

