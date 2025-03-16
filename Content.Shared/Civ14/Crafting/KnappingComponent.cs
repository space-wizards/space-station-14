using Robust.Shared.Prototypes;

namespace Content.Shared.Rocks;

[RegisterComponent]
public sealed partial class KnappingComponent : Component
{
    /// <summary>
    /// Amount of hits needed to complete the craft
    /// </summary>
    [DataField("hitsRequired")]
    public int HitsRequired = 3;

    /// <summary>
    /// Prototype that the knapping will result
    /// </summary>
    [DataField("resultPrototype")]
    public string ResultPrototype = "SharpenedFlint";

    /// <summary>
    /// If true, allow the user to knap into a boulder. takes longer
    /// </summary>
    [DataField("allowRockKnapping")]
    public bool AllowRockKnapping = true;

    /// <summary>
    /// time that each knapping hit takes
    /// </summary>
    [DataField("hitTime")]
    public float HitTime = 2.0f;

    /// <summary>
    /// amount of hits already done
    /// </summary>
    [DataField("currentHits")]
    public int CurrentHits = 0;
}