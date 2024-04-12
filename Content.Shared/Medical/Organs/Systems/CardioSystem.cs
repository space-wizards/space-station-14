using Content.Shared.FixedPoint;
using Content.Shared.Medical.Organs.Components;

namespace Content.Shared.Medical.Organs.Systems;

public sealed class CardioSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<HeartComponent, MapInitEvent>(OnHeartMapInit);
    }

    private void OnHeartMapInit(EntityUid uid, HeartComponent heart, ref MapInitEvent args)
    {
        if (!heart.IsBeating)
        {
            heart.CurrentRate = 0;
        }
        if (heart.CurrentRate < 0)
            heart.CurrentRate = heart.RestingRate;
        Dirty(uid, heart);
    }


    /// <summary>
    /// Get the OPTIMAL possible cardiac output for the specified organ NOT taking into account efficiency OR blood volume
    /// </summary>
    /// <param name="heart"> Target heart organ</param>
    /// <returns>Cardiac Output Value in reagentUnits per second</returns>
    public FixedPoint2 GetOptimalCardiacOutput(Entity<HeartComponent> heart)
    {
        return heart.Comp.PumpVolume  * heart.Comp.CurrentRateSeconds;
    }

    /// <summary>
    /// Get the CURRENT cardiac for the specified organ taking into account efficiency OR blood volume
    /// </summary>
    /// <param name="heart">Target heart organ</param>
    /// <param name="bloodVolumeRatio">Ratio of current to max blood volume (0-1)</param>
    /// <param name="efficiency">Efficiency of the heart organ</param>
    /// <returns>Cardiac Output Value in reagentUnits per second</returns>
    public FixedPoint2 GetCurrentCardiacOutput(Entity<HeartComponent> heart, FixedPoint2 bloodVolumeRatio, FixedPoint2 efficiency)
    {
        if (!heart.Comp.IsBeating)
            return 0; //If the heart ain't beating, we ain't pumpin!

        return GetOptimalCardiacOutput(heart) * bloodVolumeRatio  * efficiency;
    }



}
