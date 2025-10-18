using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Nutrition.Components;

/// <summary>
/// Represents a solution container that can hold the pressure from a solution that
/// gets fizzy when aggitated, and can spray the solution when opened or thrown.
/// Handles simulating the fizziness of the solution, responding to aggitating events,
/// and spraying the solution out when opening or throwing the entity.
/// </summary>
[NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[RegisterComponent, Access(typeof(PressurizedSolutionSystem))]
public sealed partial class PressurizedSolutionComponent : Component
{
    /// <summary>
    /// The name of the solution to use.
    /// </summary>
    [DataField]
    public string Solution = "drink";

    /// <summary>
    /// The sound to play when the solution sprays out of the container.
    /// </summary>
    [DataField]
    public SoundSpecifier SpraySound = new SoundPathSpecifier("/Audio/Items/soda_spray.ogg");

    /// <summary>
    /// The longest amount of time that the solution can remain fizzy after being aggitated.
    /// Put another way, how long the solution will remain fizzy when aggitated the maximum amount.
    /// Used to calculate the current fizziness level.
    /// </summary>
    [DataField]
    public TimeSpan FizzinessMaxDuration = TimeSpan.FromSeconds(120);

    /// <summary>
    /// The time at which the solution will be fully settled after being shaken.
    /// </summary>
    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan FizzySettleTime;

    /// <summary>
    /// How much to increase the solution's fizziness each time it's shaken.
    /// This assumes the solution has maximum fizzability.
    /// A value of 1 will maximize it with a single shake, and a value of
    /// 0.5 will increase it by half with each shake.
    /// </summary>
    [DataField]
    public float FizzinessAddedOnShake = 1.0f;

    /// <summary>
    /// How much to increase the solution's fizziness when it lands after being thrown.
    /// This assumes the solution has maximum fizzability.
    /// </summary>
    [DataField]
    public float FizzinessAddedOnLand = 0.25f;

    /// <summary>
    /// How much to modify the chance of spraying when the entity is opened.
    /// Increasing this effectively increases the fizziness value when checking if it should spray.
    /// </summary>
    [DataField]
    public float SprayChanceModOnOpened = -0.01f; // Just enough to prevent spraying at 0 fizziness

    /// <summary>
    /// How much to modify the chance of spraying when the entity is shaken.
    /// Increasing this effectively increases the fizziness value when checking if it should spray.
    /// </summary>
    [DataField]
    public float SprayChanceModOnShake = -1; // No spraying when shaken by default

    /// <summary>
    /// How much to modify the chance of spraying when the entity lands after being thrown.
    /// Increasing this effectively increases the fizziness value when checking if it should spray.
    /// </summary>
    [DataField]
    public float SprayChanceModOnLand = 0.25f;

    /// <summary>
    /// Holds the current randomly-rolled threshold value for spraying.
    /// If fizziness exceeds this value when the entity is opened, it will spray.
    /// By rolling this value when the entity is aggitated, we can have randomization
    /// while still having prediction!
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SprayFizzinessThresholdRoll;

    /// <summary>
    /// Popup message shown to user when sprayed by the solution.
    /// </summary>
    [DataField]
    public LocId SprayHolderMessageSelf = "pressurized-solution-spray-holder-self";

    /// <summary>
    /// Popup message shown to others when a user is sprayed by the solution.
    /// </summary>
    [DataField]
    public LocId SprayHolderMessageOthers = "pressurized-solution-spray-holder-others";

    /// <summary>
    /// Popup message shown above the entity when the solution sprays without a target.
    /// </summary>
    [DataField]
    public LocId SprayGroundMessage = "pressurized-solution-spray-ground";
}
