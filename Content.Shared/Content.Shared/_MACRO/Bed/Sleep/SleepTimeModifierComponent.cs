using Robust.Shared.GameStates;

namespace Content.Shared._MACRO.Bed.Sleep;

/// <summary>
///     Component that modifies an entity's sleep wakeup cooldown
///     by a multiplier.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class SleepTimeModifierComponent : Component
{
    /// <summary>
    ///     When this entity is put to sleep, the cooldown before
    ///     an attached player can wake the entity up will be
    ///     multiplied by this value.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Modifier = 1;
}
