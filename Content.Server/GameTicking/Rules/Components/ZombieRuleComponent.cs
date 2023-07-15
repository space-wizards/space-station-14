namespace Content.Server.GameTicking.Rules.Components;


[RegisterComponent, Access(typeof(ZombieRuleSystem))]
public sealed class ZombieRuleComponent : Component
{
    public Dictionary<string, string> InitialInfectedNames = new();

    public string PatientZeroPrototypeID = "InitialInfected";
    public const string ZombifySelfActionPrototype = "TurnUndead";

    /// <summary>
    ///   After this many seconds the players will be forced to turn into zombies (at minimum)
    ///   Defaults to 20 minutes. 20*60 = 1200 seconds.
    ///
    ///   Zombie time for a given player is:
    ///   random MinZombieForceSecs to MaxZombieForceSecs + up to PlayerZombieForceVariation
    /// </summary>
    [DataField("minZombieForceSecs"), ViewVariables(VVAccess.ReadWrite)]
    public float MinZombieForceSecs = 1200;

    /// <summary>
    ///   After this many seconds the players will be forced to turn into zombies (at maximum)
    ///   Defaults to 30 minutes. 30*60 = 1800 seconds.
    /// </summary>
    [DataField("maxZombieForceSecs"), ViewVariables(VVAccess.ReadWrite)]
    public float MaxZombieForceSecs = 1800;

    /// <summary>
    ///   How many additional seconds each player will get (at random) to scatter forced zombies over time.
    ///   Defaults to 2 minutes. 2*60 = 120 seconds.
    /// </summary>
    [DataField("playerZombieForceVariationSecs"), ViewVariables(VVAccess.ReadWrite)]
    public float PlayerZombieForceVariationSecs = 120;
}
