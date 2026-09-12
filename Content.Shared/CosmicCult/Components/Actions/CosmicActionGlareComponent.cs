using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.CosmicCult.Components.Actions;

[NetworkedComponent, RegisterComponent]
// [AutoGenerateComponentPause]
public sealed partial class CosmicActionGlareComponent : Component
{
    /// <summary>
    /// The duration of Null Glare's flash/disorientation.
    /// </summary>
    [DataField] public TimeSpan DurationDefault = TimeSpan.FromSeconds(5);
    [DataField] public TimeSpan DurationEmpowered = TimeSpan.FromSeconds(10);

    /// <summary>
    /// The range of Null Glare.
    /// </summary>
    [DataField] public int RangeDefault = 8;
    [DataField] public int RangeEmpowered = 10;

    /// <summary>
    /// The movement speed penalty inflicted by Null Glare.
    /// </summary>
    [DataField] public float MovePenaltyDefault = 0.25f;
    [DataField] public float MovePenaltyEmpowered = 0.4f;

    /// <summary>
    /// The stun duration inflicted by Null Glare.
    /// </summary>
    [DataField] public TimeSpan StunDefault = TimeSpan.FromSeconds(0);
    [DataField] public TimeSpan StunEmpowered = TimeSpan.FromSeconds(1);
}
