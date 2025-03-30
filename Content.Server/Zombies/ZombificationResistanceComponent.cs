namespace Content.Server.Zombies;

[RegisterComponent]
public sealed partial class ZombificationResistanceComponent: Component
{
    [DataField("coefficient")]
    public float ResistanceCoefficient = 1f;
}
