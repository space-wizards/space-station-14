using Content.Shared.Power.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// Ammo provider that uses electric charge from a battery to provide ammunition to a weapon.
/// This works with both <see cref="BatteryComponent"/> and <see cref="PredictedBatteryComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(raiseAfterAutoHandleState: true), AutoGenerateComponentPause]
public sealed partial class BatteryAmmoProviderComponent : AmmoProviderComponent
{
    /// <summary>
    /// The projectile or hitscan entity to spawn when firing.
    /// </summary>
    [DataField("proto", required: true)]
    public EntProtoId Prototype;

    /// <summary>
    /// How much charge it costs to fire once, in watts.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float FireCost = 100;

    /// <summary>
    /// Timestamp for the next update for the shot counter and visuals.
    /// This is the expected time at which the next integer will be reached.
    /// Null if the charge rate is 0, meaning the shot amount is constant.
    /// Only used for predicted batteries.
    /// </summary>
    /// <remarks>
    /// Not a datafield since this is refreshed along with the battery's charge rate anyways.
    /// </remarks>
    [ViewVariables, AutoNetworkedField, AutoPausedField]
    public TimeSpan? NextUpdate;

    /// <summary>
    /// The time between reaching full charges at the current charge rate.
    /// Only used for predicted batteries.
    /// </summary>
    /// <remarks>
    /// Not a datafield since this is refreshed along with the battery's charge rate anyways.
    /// </remarks>
    [ViewVariables, AutoNetworkedField]
    public TimeSpan ChargeTime = TimeSpan.Zero;

    /// <summary>
    /// The current amount of available shots.
    /// BatteryComponent is not predicted, so we need to manually network this for the ammo indicator and examination text.
    /// </summary>
    /// <remarks>
    /// Not a datafield since this is only cached and refreshed on component startup.
    /// TODO: If we ever fully predict all batteries then remove this and just read the charge on the client.
    /// </remarks>
    [ViewVariables, AutoNetworkedField]
    public int Shots;

    /// <summary>
    /// The maximum amount of available shots.
    /// BatteryComponent is not predicted, so we need to manually network this for the ammo indicator and examination text.
    /// </summary>
    /// <remarks>
    /// Not a datafield since this is only cached and refreshed on component startup.
    /// TODO: If we ever fully predict all batteries then remove this and just read the charge on the client.
    /// </remarks>
    [ViewVariables, AutoNetworkedField]
    public int Capacity;
}
