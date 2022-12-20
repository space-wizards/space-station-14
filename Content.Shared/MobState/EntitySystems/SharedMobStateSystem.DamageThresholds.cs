using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.MobState.Components;

namespace Content.Shared.MobState.EntitySystems;

public abstract partial class SharedMobStateSystem
{
    private void OnDamageRecieved(EntityUid _, MobStateComponent component, DamageChangedEvent args)
    {
        CheckDamageThreshold(component, args.Damageable.TotalDamage, args.Origin);
    }

    private (MobState state, FixedPoint2 threshold)? GetEarliestState(MobStateComponent component,
        FixedPoint2 minimumDamage, Predicate<MobState> predicate)
    {
        foreach (var (threshold, state) in component._lowestToHighestStates)
        {
            if (threshold < minimumDamage ||
                !predicate(state))
            {
                continue;
            }

            return (state, threshold);
        }

        return null;
    }

    private (MobState state, FixedPoint2 threshold)? GetPreviousState(MobStateComponent component,
        FixedPoint2 maximumDamage, Predicate<MobState> predicate)
    {
        foreach (var (threshold, state) in component._highestToLowestStates)
        {
            if (threshold > maximumDamage ||
                !predicate(state))
            {
                continue;
            }

            return (state, threshold);
        }

        return null;
    }

    public (MobState state, FixedPoint2 threshold)? GetEarliestCriticalState(MobStateComponent component,
        FixedPoint2 minimumDamage)
    {
        return GetEarliestState(component, minimumDamage, s => s == MobState.Critical);
    }

    public (MobState state, FixedPoint2 threshold)? GetEarliestIncapacitatedState(MobStateComponent component,
        FixedPoint2 minimumDamage)
    {
        return GetEarliestState(component, minimumDamage, s => s is MobState.Critical or MobState.Dead);
    }

    public (MobState state, FixedPoint2 threshold)? GetEarliestDeadState(MobStateComponent component,
        FixedPoint2 minimumDamage)
    {
        return GetEarliestState(component, minimumDamage, s => s == MobState.Dead);
    }

    public (MobState state, FixedPoint2 threshold)? GetPreviousCriticalState(MobStateComponent component,
        FixedPoint2 minimumDamage)
    {
        return GetPreviousState(component, minimumDamage, s => s == MobState.Critical);
    }

    public bool TryGetStateThreshold(
        MobStateComponent component,
        FixedPoint2 damage,
        out MobState state,
        out FixedPoint2 threshold)
    {
        var highestState = GetState(component, damage);

        if (highestState == null)
        {
            state = MobState.Invalid;
            threshold = default;
            return false;
        }

        (state, threshold) = highestState.Value;
        return true;
    }

    private bool TryGetStateThreshold(
        (MobState state, FixedPoint2 threshold)? tuple,
        out MobState state,
        out FixedPoint2 threshold)
    {
        if (tuple == null)
        {
            state = MobState.Invalid;
            threshold = default;
            return false;
        }

        (state, threshold) = tuple.Value;
        return true;
    }

    public bool TryGetEarliestCriticalThreshold(
        MobStateComponent component,
        FixedPoint2 minimumDamage,
        out MobState state,
        out FixedPoint2 threshold)
    {
        var earliestState = GetEarliestCriticalState(component, minimumDamage);

        return TryGetStateThreshold(earliestState, out state, out threshold);
    }

    public bool TryGetEarliestIncapacitatedThreshold(
        MobStateComponent component,
        FixedPoint2 minimumDamage,
        out MobState state,
        out FixedPoint2 threshold)
    {
        var earliestState = GetEarliestIncapacitatedState(component, minimumDamage);

        return TryGetStateThreshold(earliestState, out state, out threshold);
    }

    public bool TryGetEarliestDeadThreshold(
        MobStateComponent component,
        FixedPoint2 minimumDamage,
        out MobState state,
        out FixedPoint2 threshold)
    {
        var earliestState = GetEarliestDeadState(component, minimumDamage);

        return TryGetStateThreshold(earliestState, out state, out threshold);
    }

    public bool TryGetPreviousCriticalThreshold(
        MobStateComponent component,
        FixedPoint2 maximumDamage,
        out MobState state,
        out FixedPoint2 threshold)
    {
        var earliestState = GetPreviousCriticalState(component, maximumDamage);

        return TryGetStateThreshold(earliestState, out state, out threshold);
    }
}
