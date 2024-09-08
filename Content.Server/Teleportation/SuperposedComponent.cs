namespace Content.Server.Teleportation;

[RegisterComponent, Access(typeof(SuperposedSystem))]
public sealed partial class SuperposedComponent : Component
{
    [DataField]
    public float MinObserverRange = 2f;

    [DataField]
    public float MaxObserverRange = 5f;

    [DataField]
    public float SuperposeRange = 2f;

    [DataField]
    public float MaxOffset = 0.25f;

    [DataField]
    public bool Observed = false;

    [DataField]
    public EntityUid[] PossibleLocations = {};
}
