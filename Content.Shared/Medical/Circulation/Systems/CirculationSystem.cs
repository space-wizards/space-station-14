using System.Diagnostics.CodeAnalysis;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Circulation.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.Circulation.Systems;

public sealed partial class CirculationSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    private float _accumulatedFrameTime;
    public const float CirculationUpdateInterval = 0.5f;

    public override void Initialize()
    {
        SubscribeLocalEvent<CirculationComponent, ComponentHandleState>(OnHandleCirculationState);
        SubscribeLocalEvent<CirculationComponent, ComponentGetState>(OnGetCirculationState);
        SubscribeLocalEvent<CirculationVesselComponent, ComponentHandleState>(OnHandleVesselState);
        SubscribeLocalEvent<CirculationVesselComponent, ComponentGetState>(OnGetVesselState);
    }

    private void OnHandleCirculationState(EntityUid uid, CirculationComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not CirculationComponentState state)
            return;
        component.Reagents = state.Reagents;
        component.LinkedVessels = state.LinkedVessels;
        component.TotalReagentVolume = state.TotalReagentVolume;
        component.TotalCapacity = state.TotalCapacity;
    }

    private void OnGetCirculationState(EntityUid uid, CirculationComponent component, ref ComponentGetState args)
    {
        args.State = new CirculationComponentState(
            component.Reagents,
            component.LinkedVessels,
            component.TotalReagentVolume,
            component.TotalCapacity
        );
    }

    private void OnHandleVesselState(EntityUid uid, CirculationVesselComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not CirculationVesselComponentState state)
            return;
        component.Capacity = state.Capacity;
        component.Parent = state.Parent;
        component.LocalReagents = state.LocalReagents;
    }

    private void OnGetVesselState(EntityUid uid, CirculationVesselComponent component, ref ComponentGetState args)
    {
        args.State = new CirculationVesselComponentState(
            component.Parent,
            component.LocalReagents,
            component.Capacity
        );
    }


    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _accumulatedFrameTime += frameTime;
        if (_accumulatedFrameTime < CirculationUpdateInterval)
            return;
        _accumulatedFrameTime -= CirculationUpdateInterval;
        foreach (var circulation in EntityQuery<CirculationComponent>())
        {
            RunCirculationTick(circulation.Owner, circulation);
        }
    }

    private void RunCirculationTick(EntityUid circulationEntity, CirculationComponent circulation)
    {
        var updateEvent = new CirculationTickEvent(circulation, new Dictionary<string, FixedPoint2>());
        RaiseLocalEvent(circulationEntity, updateEvent);
        foreach (var vesselEntity in circulation.LinkedVessels)
        {
            RaiseLocalEvent(vesselEntity, updateEvent);
        }

        foreach (var (reagentId, volumeAdjustment) in updateEvent.VolumeChanges)
        {
            AdjustSharedVolume(circulation.Owner, reagentId, volumeAdjustment, circulation);
        }
    }

    public bool AddNewReagentType(EntityUid entity, string reagentId, FixedPoint2 volume,
        CirculationComponent? circulation = null)
    {
        if (!Resolve(entity, ref circulation) || !_prototypeManager.TryIndex<ReagentPrototype>(reagentId, out _) ||
            !circulation.Reagents.TryAdd(reagentId, volume))
            return false;
        UpdateSharedVolume(entity, circulation);
        return true;
    }

    public bool RemoveReagentType(EntityUid entity, string reagentId, CirculationComponent? circulation = null)
    {
        if (!Resolve(entity, ref circulation) || !_prototypeManager.TryIndex<ReagentPrototype>(reagentId, out _) ||
            !circulation.Reagents.Remove(reagentId))
            return false;
        UpdateSharedVolume(entity, circulation);
        return true;
    }
}
