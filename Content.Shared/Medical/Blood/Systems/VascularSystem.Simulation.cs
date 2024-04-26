using Content.Shared.Body.Organ;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Blood.Components;
using Content.Shared.Medical.Organs.Components;

namespace Content.Shared.Medical.Blood.Systems;

public sealed partial class VascularSystem
{
    public void SetHealthyBloodPressure(Entity<VascularSystemComponent> vascularEntity, BloodPressure healthyPressure)
    {

        if (healthyPressure.High < 0 || healthyPressure.Low < 0)
        {
            Log.Error("Neither blood pressure value can be negative!");
            return;
        }

        if (healthyPressure.High < healthyPressure.Low)
        {
            Log.Warning("HealthyHighPressure pressure must be equal to or above HealthyLowPressure! Clamping value!");
            healthyPressure.High = healthyPressure.Low;
        }

        vascularEntity.Comp.HealthyBloodPressure = healthyPressure;
        vascularEntity.Comp.HighPressureVascularConstant =
            CalculateVascularConstant(healthyPressure.High, vascularEntity.Comp.OptimalCardiacOutput);
        vascularEntity.Comp.LowPressureVascularConstant =
            CalculateVascularConstant(healthyPressure.Low, vascularEntity.Comp.OptimalCardiacOutput);
        Dirty(vascularEntity);
    }

    private void VascularSystemUpdate(Entity<VascularSystemComponent, BloodstreamComponent> vascularEntity)
    {
        FixedPoint2 cardiacOutput = 0;

        foreach (var entity in vascularEntity.Comp1.CirculationEntities)
        {
            FixedPoint2 efficiency = 1;
            if (!TryComp<HeartComponent>(entity, out var heartComp))
                continue;
            if (TryComp<OrganComponent>(entity, out var organComp))
                efficiency = organComp.Efficiency;
            cardiacOutput += _cardioSystem.GetCurrentCardiacOutput((entity, heartComp),
                GetVolumeRatio((vascularEntity.Owner, vascularEntity.Comp2)),
                efficiency);
        }

        var high = CalculateBloodPressure(cardiacOutput, vascularEntity.Comp1.VascularResistance,
            vascularEntity.Comp1.HighPressureVascularConstant);
        var low = CalculateBloodPressure(cardiacOutput, vascularEntity.Comp1.VascularResistance,
            vascularEntity.Comp1.LowPressureVascularConstant);
        vascularEntity.Comp1.CurrentBloodPressure = (high, low);
        vascularEntity.Comp1.Pulse = GetHighestPulse(vascularEntity.Comp1);
        Dirty(vascularEntity, vascularEntity.Comp1);
    }

    #region UtilityMethods

    public float GetVolumeRatio(Entity<BloodstreamComponent> bloodstream)
    {
        return Math.Clamp(bloodstream.Comp.MaxVolume.Float() / bloodstream.Comp.Volume.Float(), 0f, 1f);
    }

    private float CalculateVascularConstant(
        FixedPoint2 targetPressure,
        FixedPoint2 cardiacOutput)
    {
        return (targetPressure / cardiacOutput).Float();
    }

    private FixedPoint2 CalculateBloodPressure(
        FixedPoint2 cardiacOutput,
        FixedPoint2 vascularResistance,
        FixedPoint2 vascularConstant
    )
    {
        return cardiacOutput * (vascularResistance * vascularConstant);
    }

    private FixedPoint2? GetHighestPulse(VascularSystemComponent vascularSystemComp)
    {
        FixedPoint2? pulse = null;
        foreach (var circEnt in vascularSystemComp.CirculationEntities)
        {
            if (!TryComp<HeartComponent>(circEnt, out var heart))
                continue;
            if (pulse == null || pulse < heart.CurrentRate)
                pulse = heart.CurrentRate;
        }
        return pulse;
    }

#endregion

}
