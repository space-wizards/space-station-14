namespace Content.Server.Pinpointer;

/// <summary>
/// Used to mark entities that are seeds for generating nav map regions on the client UI
/// </summary>
[RegisterComponent]
[Access(typeof(NavMapSystem))]
public sealed partial class NavMapRegionSeedComponent : Component
{

}
