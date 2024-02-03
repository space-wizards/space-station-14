namespace Content.Server.Zombies;

/// <summary>
/// Zombified entities with this component cannot infect other entities by attacking.
/// </summary>
[RegisterComponent]
public sealed partial class NonSpreaderZombieComponent: Component
{

}
