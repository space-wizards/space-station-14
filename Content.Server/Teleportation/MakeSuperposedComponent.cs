namespace Content.Server.Teleportation;

/// <summary>
/// Used to ensure SuperposedComponent on entity spawn with some chance.
/// </summary>
[RegisterComponent, Access(typeof(MakeSuperposedSystem))]
public sealed partial class MakeSuperposedComponent : Component
{
    [DataField]
    public float Chance = 1f;
}
