using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Organs.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HeartComponent : Component
{

    //TODO: implement fibrillation :^)

    /// <summary>
    /// Is this heart currently beating?
    /// For mapped organs this should be set to false, unless you want the heart to beat while
    /// it's sitting on the ground. Fun fact: the heart contains its own neurons and will keep beating even after death or
    /// removal from the body as long as it is supplied with oxygen.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsBeating = true;


    /// <summary>
    /// How much blood does this heart pump per cycle
    /// </summary>
    [DataField, AutoNetworkedField]//TODO: Required
    public FixedPoint2 PumpVolume = 30;

    /// <summary>
    /// The resting heartrate for this organ
    /// </summary>
    [DataField, AutoNetworkedField] //TODO: required
    public FixedPoint2 RestingRate = 90;

    /// <summary>
    /// The current heart rate. If this is less than 0, it will be set to the resting rate on map init.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 CurrentRate = -1;

    /// <summary>
    /// The maximum rate that this heart will safely beat at.
    /// </summary>
    [DataField(required:true), AutoNetworkedField]
    public FixedPoint2 MaximumRate = 190;

    public FixedPoint2 RestingRateSeconds => RestingRate/60;
    public FixedPoint2 CurrentRateSeconds => CurrentRate/60;
    public FixedPoint2 MaximumRateSeconds => MaximumRate/60;
}
