using Content.Shared.EntityTable.EntitySelectors;

namespace Content.Server.Arcade.Components;

/// <summary>
///     Represents the reward pool that an arcade machine can hold.
///     Arcade machines have a limited number of prizes they can dispense, and
///     prizes are dispensed when a game signals that it has been won.
/// </summary>
[RegisterComponent]
public sealed partial class ArcadeRewardComponent : Component
{
    /// <summary>
    /// Table that determines what gets spawned.
    /// </summary>
    [DataField(required: true)]
    public EntityTableSelector PossibleRewards = default!;

    /// <summary>
    /// The minimum number of prizes the arcade machine can have.
    /// </summary>
    [DataField]
    public int RewardMinAmount = 0;

    /// <summary>
    /// The maximum number of prizes the arcade machine can have.
    /// </summary>
    [DataField]
    public int RewardMaxAmount = 0;

    /// <summary>
    /// The remaining number of prizes the arcade machine can dispense.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public int RewardAmount = 0;
}
