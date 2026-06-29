using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Delivery;

/// <summary>
/// Component given to deliveries.
/// This delivery will "prime" based on circumstances defined in the datafield.
/// When primed, it will attempt to explode every few seconds, with the chance increasing each time it fails to do so.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(DeliveryModifierSystem))]
public sealed partial class DeliveryBombComponent : Component
{
    /// <summary>
    /// How often will this bomb retry to explode.
    /// </summary>
    [DataField]
    public TimeSpan ExplosionRetryDelay = TimeSpan.FromSeconds(3);

    /// <summary>
    /// The time at which the next retry will happen
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextExplosionRetry;

    /// <summary>
    /// The chance this bomb explodes each time it attempts to do so.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ExplosionChance = 0.05f;

    /// <summary>
    /// How much should the chance of explosion increase each failed retry?
    /// </summary>
    [DataField]
    public float ExplosionChanceRetryIncrease = 0.01f;

    /// <summary>
    /// Should this bomb get primed when the delivery is unlocked?
    /// </summary>
    [DataField]
    public bool PrimeOnUnlock = true;

    /// <summary>
    /// Should this bomb get primed when the delivery is broken?
    /// Requires to be fragile as well.
    /// </summary>
    [DataField]
    public bool PrimeOnBreakage = true;

    /// <summary>
    /// Should this bomb get primed when the delivery expires?
    /// Requires to be priority as well.
    /// </summary>
    [DataField]
    public bool PrimeOnExpire = true;

    /// <summary>
    /// Multiplier to choose when a crazy person actually opens it.
    /// Multiplicative, not additive.
    /// </summary>
    [DataField]
    public float SpesoMultiplier = 1.5f;
}
