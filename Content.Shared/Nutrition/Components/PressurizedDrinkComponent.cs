using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Nutrition.Components;

/// <summary>
/// Lets a drink burst open when thrown while closed.
/// Requires <see cref="DrinkComponent"/> and <see cref="OpenableComponent"/> to work.
/// </summary>
[NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[RegisterComponent, Access(typeof(SharedPressurizedDrinkSystem))]
public sealed partial class PressurizedDrinkComponent : Component
{
    /// <summary>
    /// Name of the solution to use.
    /// </summary>
    [DataField]
    public string Solution = "drink";

    [DataField, AutoNetworkedField]
    public SoundSpecifier SpraySound = new SoundPathSpecifier("/Audio/Items/soda_spray.ogg");

    /// <summary>
    /// The longest amount of time that the drink can remain fizzy after being shaken.
    /// Used to calculate the current fizziness level.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan FizzyMaxDuration = TimeSpan.FromSeconds(120);

    /// <summary>
    /// The time at which the drink will be fully settled after being shaken.
    /// </summary>
    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan FizzySettleTime;

    /// <summary>
    /// How much to increase the drink's fizziness each time it's shaken.
    /// A value of 1 will maximize it with a single shake, and a value of
    /// 0.5 will increase it by half with each shake.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float FizzinessAddedPerShake = 0.4f;

    /// <summary>
    /// How much to modify the chance of spraying when the drink is opened.
    /// Increasing this effectively increases the fizziness value when checking if it should spray.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SprayChanceModOpened = -0.25f;

    /// <summary>
    /// How much to modify the chance of spraying when the drink is thrown.
    /// Increasing this effectively increases the fizziness value when checking if it should spray.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SprayChanceModThrown = 0.25f;

    /// <summary>
    /// Holds the current randomly-rolled threshold value for spraying.
    /// If fizziness exceeds this value when the drink is opened, it will spray.
    /// By rolling this value when the drink is shaken, we can have randomization
    /// while still having prediction!
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SprayFizzinessThresholdRoll;

    /// <summary>
    /// Popup message shown to user when sprayed by the drink.
    /// </summary>
    [DataField]
    public LocId SprayHolderMessageSelf = "pressurized-drink-spray-holder-self";

    /// <summary>
    /// Popup message shown to others when a user is sprayed by the drink.
    /// </summary>
    [DataField]
    public LocId SprayHolderMessageOthers = "pressurized-drink-spray-holder-others";

    /// <summary>
    /// Popup message shown when the drink sprays without a target.
    /// </summary>
    [DataField]
    public LocId SprayGroundMessage = "pressurized-drink-spray-ground";
}
