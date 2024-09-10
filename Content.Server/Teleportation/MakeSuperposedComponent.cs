namespace Content.Server.Teleportation;

[RegisterComponent, Access(typeof(MakeSuperposedSystem))]
public sealed partial class MakeSuperposedComponent : Component
{
    [DataField]
    public float Chance = 1f;
}
