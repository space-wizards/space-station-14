using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.ForceAttack;

/// <summary>
/// This is used to force a player-controlled mob to attack nearby enemies, preventing "friendly antag"ing.
/// </summary>
[RegisterComponent]
[AutoGenerateComponentPause]
public sealed partial class ForceAttackComponent : Component
{
    /// <summary>
    /// The next time this component will attempt to force an attack.
    /// </summary>
    [AutoPausedField]
    public TimeSpan NextAttack = TimeSpan.MaxValue;

    /// <summary>
    /// Whether an enemy is in range.
    /// </summary>
    public bool InRange = false;

    /// <summary>
    /// The time this component will wait before forcing an attack when an enemy is in range.
    /// </summary>
    [DataField]
    public TimeSpan PassiveTime = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The message displayed on forced attack
    /// </summary>
    [DataField]
    public LocId Message = "force-attack-component-message";
}
