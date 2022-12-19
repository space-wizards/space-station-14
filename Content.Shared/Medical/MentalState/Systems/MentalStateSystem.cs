using Content.Shared.FixedPoint;
using Content.Shared.Medical.MentalState.Components;
using Content.Shared.MobState.EntitySystems;

namespace Content.Shared.Medical.MentalState.Systems;

public sealed class MentalStateSystem : EntitySystem
{
    [Dependency] private SharedMobStateSystem _mobStateSystem = default!;
    private FixedPoint2 _minMentalState = 0;
    private FixedPoint2 _unconciousnessThreshold = 30;
    private FixedPoint2 _maxMentalState = 100;

    public override void Initialize()
    {
    }

    public void SetBase(EntityUid target, FixedPoint2 baseValue, MentalStateComponent? stateComponent = null)
    {
        if (!Resolve(target, ref stateComponent, false))
            return;
        stateComponent.Base = FixedPoint2.Clamp(baseValue, _minMentalState,
            stateComponent.Caps.Count > 0 ? stateComponent.Caps.Min : _maxMentalState);

        UpdateMentalState(target, stateComponent);
    }

    public void AddBase(EntityUid target, FixedPoint2 baseValue, MentalStateComponent? stateComponent = null)
    {
        if (!Resolve(target, ref stateComponent, false))
            return;

        stateComponent.Base = FixedPoint2.Clamp(stateComponent.Base + baseValue, _minMentalState,
            stateComponent.Caps.Count > 0 ? stateComponent.Caps.Min : _maxMentalState);
        UpdateMentalState(target, stateComponent);
    }

    public void SetModifier(EntityUid target, FixedPoint2 modifier, MentalStateComponent? stateComponent = null)
    {
        if (!Resolve(target, ref stateComponent, false))
            return;
        stateComponent.Modifier = modifier;
        UpdateMentalState(target, stateComponent);
    }

    public void AddModifier(EntityUid target, FixedPoint2 modifier, MentalStateComponent? stateComponent = null)
    {
        if (!Resolve(target, ref stateComponent, false))
            return;
        stateComponent.Modifier += modifier;
        UpdateMentalState(target, stateComponent);
    }

    public void SetOffset(EntityUid target, FixedPoint2 offset, MentalStateComponent? stateComponent = null)
    {
        if (!Resolve(target, ref stateComponent, false))
            return;
        stateComponent.Offset = offset;
        UpdateMentalState(target, stateComponent);
    }

    public void AddOffset(EntityUid target, FixedPoint2 offset, MentalStateComponent? stateComponent = null)
    {
        if (!Resolve(target, ref stateComponent, false))
            return;
        stateComponent.Offset += offset;
        UpdateMentalState(target, stateComponent);
    }

    public void AddCap(EntityUid target, FixedPoint2 cap, MentalStateComponent? stateComponent = null)
    {
        if (!Resolve(target, ref stateComponent, false))
            return;

        stateComponent.Caps.Add(cap);

        stateComponent.Base = FixedPoint2.Clamp(stateComponent.Base, _minMentalState,
            stateComponent.Caps.Count > 0 ? stateComponent.Caps.Min : _maxMentalState);
        UpdateMentalState(target, stateComponent);
    }

    public void RemoveCap(EntityUid target, FixedPoint2 cap, MentalStateComponent? stateComponent = null)
    {
        if (!Resolve(target, ref stateComponent, false))
            return;

        stateComponent.Caps.Remove(cap);
        stateComponent.Base = FixedPoint2.Clamp(stateComponent.Base, _minMentalState,
            stateComponent.Caps.Count > 0 ? stateComponent.Caps.Min : _maxMentalState);
        UpdateMentalState(target, stateComponent);
    }


    private void UpdateMentalState(EntityUid target, MentalStateComponent? stateComponent)
    {
        UpdateMentalStateLocal(target, stateComponent);
        Dirty(target);
    }

    private void UpdateMentalStateLocal(EntityUid target, MentalStateComponent? stateComponent)
    {
        if (!Resolve(target, ref stateComponent, false))
            return;

        var newState = (stateComponent.Base * stateComponent.Modifier) + stateComponent.Offset;
        newState = FixedPoint2.Clamp(newState, _minMentalState,
            stateComponent.Caps.Count > 0 ? stateComponent.Caps.Min : 100);
        if (newState > _maxMentalState)
            newState = _maxMentalState; //make sure that we can never go over max mental state
        stateComponent.Value = newState;
        var ev = new MentalStateChangedEvent();
        RaiseLocalEvent(ev);
        if (!stateComponent.Unconscious && stateComponent.Value < _unconciousnessThreshold)
        {
            stateComponent.Unconscious = true;
        }
    }


    // public FixedPoint2 GetMentalState(EntityUid target, MentalStateComponent? component)
    // {
    //
    // }
}
