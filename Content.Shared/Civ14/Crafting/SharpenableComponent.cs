using Robust.Shared.Prototypes;

namespace Content.Shared.Crafting;

[RegisterComponent]
public sealed partial class SharpenableComponent : Component
{
    /// <summary>
    /// Time (in seconds) to sharpen the stick
    /// </summary>
    [DataField("sharpenTime")]
    public float SharpenTime = 5.0f;

    /// <summary>
    /// Prototype that will result after sharping
    /// </summary>
    [DataField("resultPrototype")]
    public string ResultPrototype = "SharpenedStick";

    /// <summary>
    /// If true, players will be able to sharpen using bare hands
    /// </summary>
    [DataField("canSharpenByHand")]
    public bool CanSharpenByHand = false;
}