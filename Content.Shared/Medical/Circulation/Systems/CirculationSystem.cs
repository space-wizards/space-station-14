using System.Diagnostics.CodeAnalysis;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Circulation.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.Circulation.Systems;

public sealed partial class CirculationSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    private float _accumulatedFrameTime;
    public const float CirculationUpdateInterval = 0.5f;
    public override void Initialize()
    {
    }
    //TODO: Event subscriptions and networking

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _accumulatedFrameTime += frameTime;
        if (_accumulatedFrameTime < CirculationUpdateInterval)
            return;
        _accumulatedFrameTime -= CirculationUpdateInterval;
        foreach (var circulation in EntityQuery<CirculationComponent>())
        {
            var updateEvent = new CirculationTickEvent(circulation, new Dictionary<string, FixedPoint2>());
            RaiseLocalEvent(circulation.Owner, ref updateEvent);
            foreach (var (reagentId, volumeAdjustment) in updateEvent.VolumeChanges)
            {
                AdjustReagentVolume(circulation.Owner, reagentId, volumeAdjustment, circulation);
            }
        }
    }
    public bool AddNewReagentType(EntityUid entity, string reagentId, FixedPoint2 volume,
        CirculationComponent? circulation = null)
    {
        if (!Resolve(entity, ref circulation) || !_prototypeManager.TryIndex<ReagentPrototype>(reagentId, out _) ||
            !circulation.Reagents.TryAdd(reagentId, volume))
            return false;
        UpdateTotalVolume(entity, circulation);
        return true;
    }

    public bool RemoveReagentType(EntityUid entity, string reagentId, CirculationComponent? circulation = null)
    {
        if (!Resolve(entity, ref circulation) || !_prototypeManager.TryIndex<ReagentPrototype>(reagentId, out _) ||
            !circulation.Reagents.Remove(reagentId))
            return false;
        UpdateTotalVolume(entity, circulation);
        return true;
    }

    private bool HasReagent(string reagentName)
    {
        return TryGetCirculationReagent(reagentName, out _);
    }

    private bool TryGetCirculationReagent(string reagentName, [NotNullWhen(true)] out ReagentPrototype? reagentProto)
    {
        return _prototypeManager.TryIndex(reagentName, out reagentProto);
    }
}
