using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Circulation.Components;

namespace Content.Shared.Medical.Circulation.Systems;

public partial class CirculationSystem
{
    public FixedPoint2 GetReagentVolumeInVessel(EntityUid vesselEntity, string reagentId,
        CirculationVesselComponent? vessel = null)
    {
        return !Resolve(vesselEntity, ref vessel) ? FixedPoint2.Zero : GetReagentVolumeInVessel(reagentId, vessel);
    }

    private FixedPoint2 GetReagentVolumeInVessel(string reagentId,
        CirculationComponent circulation, CirculationVesselComponent vessel)
    {
        return circulation.Reagents[reagentId] * (vessel.Capacity / circulation.TotalCapacity);
    }

    public FixedPoint2 GetReagentVolumeInVessel(string reagentId,
        CirculationVesselComponent vessel)
    {
        if (TryComp<CirculationComponent>(vessel.Parent, out var circulation))
            return GetReagentVolumeInVessel(reagentId, circulation, vessel);
        if (vessel.LocalReagents == null || !vessel.LocalReagents.TryGetValue(reagentId, out var volume))
            return FixedPoint2.Zero;
        return volume;
    }

    public bool VesselIsLinked(EntityUid vesselEntity, CirculationVesselComponent? vessel = null)
    {
        if (!Resolve(vesselEntity, ref vessel))
            return false;
        return (vessel.Parent != EntityUid.Invalid);
    }

    //Do not use this for vessels on organs/bodyparts, those are automatically handled
    public bool LinkVessel(EntityUid circulationEntity, EntityUid vesselEntity,
        CirculationComponent? circulation = null, CirculationVesselComponent? vessel = null)
    {
        if (!Resolve(vesselEntity, ref vessel) || !Resolve(circulationEntity, ref circulation))
            return false;
        return LinkVessel(circulation, vessel, circulationEntity, vesselEntity);
    }

    private bool LinkVessel(CirculationComponent circulation, CirculationVesselComponent vessel,
        EntityUid circulationEntity, EntityUid vesselEntity)
    {
        //If this vessel contains standalone reagents, mix them with the the main reagents!
        if (vessel.LocalReagents != null)
        {
            foreach (var (reagent, volume) in vessel.LocalReagents)
            {
                AdjustReagentVolume(circulationEntity, reagent, volume, circulation);
            }

            vessel.LocalReagents.Clear();
            vessel.LocalReagents = null;
        }

        circulation.TotalCapacity += vessel.Capacity;
        vessel.Parent = circulationEntity;
        return circulation.LinkedVessels.Add(vesselEntity);
    }

    //Do not use this for vessels on organs/bodyparts, those are automatically handled
    public bool UnLinkVessel(EntityUid vesselEntity, CirculationVesselComponent? vessel = null)
    {
        if (!Resolve(vesselEntity, ref vessel))
            return false;
        if (TryComp<CirculationComponent>(vessel.Parent, out var circulation))
            return UnLinkVessel(vesselEntity, vessel, circulation);
        vessel.Parent = EntityUid.Invalid;
        return false;
    }

    private bool UnLinkVessel(EntityUid vesselEntity, CirculationVesselComponent vessel,  CirculationComponent circulation)
    {
        vessel.LocalReagents = new Dictionary<string, FixedPoint2>();
        foreach (var (reagentName, volume) in circulation.Reagents)
        {
            var standAloneVolume = volume * GetReagentVolumeInVessel(reagentName, circulation, vessel);
            vessel.LocalReagents.Add(reagentName, standAloneVolume);
            AdjustReagentVolume(vessel.Parent, reagentName, -standAloneVolume, circulation);
        }

        circulation.LinkedVessels.Remove(vesselEntity);
        vessel.Parent = EntityUid.Invalid;
        return true;
    }

    private void VesselBodyAttach(EntityUid vesselOwner, CirculationComponent circulation,
        CirculationVesselComponent vessel)
    {
        if (TryComp<BodyComponent>(vesselOwner, out _))
        {
            LinkVessel(circulation, vessel, vesselOwner, vesselOwner);
        }
        else
        {
            if (TryComp<BodyPartComponent>(vesselOwner, out var bodyPart))
            {
                if (bodyPart.Body == null)
                    return;
                LinkVessel(circulation, vessel, bodyPart.Body.Value, vesselOwner);
            }
            if (TryComp<OrganComponent>(vesselOwner, out var organ))
            {
                if (organ.Body == null)
                    return;
                LinkVessel(circulation, vessel, organ.Body.Value, vesselOwner);
            }
        }
    }

    private void VesselBodyDetach(EntityUid vesselOwner, CirculationComponent circulation, CirculationVesselComponent vessel)
    {
        UnLinkVessel(vesselOwner, vessel, circulation);
    }
}
