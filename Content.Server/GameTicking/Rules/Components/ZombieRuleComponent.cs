namespace Content.Server.GameTicking.Rules.Components;


[RegisterComponent]
public sealed class ZombieRuleComponent : Component
{
    public Dictionary<string, string> _initialInfectedNames = new();

    public string PatientZeroPrototypeID = "InitialInfected";
    public string InitialZombieVirusPrototype = "PassiveZombieVirus";
    public const string ZombifySelfActionPrototype = "TurnUndead";
}
