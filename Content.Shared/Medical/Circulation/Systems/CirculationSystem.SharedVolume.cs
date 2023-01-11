using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Circulation.Components;

namespace Content.Shared.Medical.Circulation.Systems;

public sealed partial class CirculationSystem
{
    private void UpdateSharedVolume(EntityUid entity, CirculationComponent circulation)
    {
        circulation.TotalReagentVolume = 0;
        foreach (var (_, volume) in circulation.Reagents)
        {
            circulation.TotalReagentVolume += volume;
        }

        var ev = new CirculationReagentsUpdated(circulation);
        Dirty(circulation);
        RaiseLocalEvent(entity, ev);
    }

    public bool AdjustSharedVolume(EntityUid entity, string reagentId, FixedPoint2 volume,
        CirculationComponent? circulation = null)
    {
        if (!Resolve(entity, ref circulation) || !_prototypeManager.TryIndex<ReagentPrototype>(reagentId, out _))
            return false;
        if (!circulation.Reagents.TryGetValue(reagentId, out var oldVolume))
        {
            circulation.Reagents.Add(reagentId, 0);
        }
        circulation.Reagents[reagentId] = oldVolume + volume;
        if (circulation.Reagents[reagentId] <= 0)
        {
            circulation.Reagents.Remove(reagentId);
        }
        UpdateSharedVolume(entity, circulation);
        return true;
    }


    public bool SetSharedVolume(EntityUid entity, string reagentId, FixedPoint2 volume,
        CirculationComponent? circulation = null)
    {
        if (!Resolve(entity, ref circulation) || !_prototypeManager.TryIndex<ReagentPrototype>(reagentId, out _))
            return false;

        if (volume <= 0)
        {
            circulation.Reagents[reagentId] = volume;
        }
        else
        {
            circulation.Reagents.Remove(reagentId);
        }
        UpdateSharedVolume(entity, circulation);
        return true;
    }

    private bool TryGetSharedVolume(EntityUid entity, string reagentName, out FixedPoint2 volume,
        CirculationComponent? circulation)
    {
        volume = 0;
        return _prototypeManager.TryIndex<ReagentPrototype>(reagentName, out _)
               && Resolve(entity, ref circulation)
               && !circulation.Reagents.TryGetValue(reagentName, out volume);
    }



}
