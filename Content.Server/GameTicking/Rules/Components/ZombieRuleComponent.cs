namespace Content.Server.GameTicking.Rules.Components;


[RegisterComponent, Access(typeof(ZombieRuleSystem))]
public sealed class ZombieRuleComponent : Component
{
    public Dictionary<string, string> InitialInfectedNames = new();

    public string PatientZeroPrototypeID = "InitialInfected";
    public string InitialZombieVirusPrototype = "PassiveZombieVirus";
    public const string ZombifySelfActionPrototype = "TurnUndead";
}
