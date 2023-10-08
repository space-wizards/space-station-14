using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server;
// Also needed FlashableComponent on entity to work
[RegisterComponent]
public sealed partial class DestroyedByFlashingComponent : Component
{
    /// <summary>
    /// how many flashes can this entity survive
    /// </summary>
    public int LifeCount = 1;

    /// <summary>
    /// The name of the prototype that appears on the destroyed shadow kudzu
    /// </summary>
    // (according to the idea - a visual effect)
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public EntProtoId RemoveEffect = "EffectDarknessPulse";
}
