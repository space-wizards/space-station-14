namespace Content.Server.Tiles;

/// <summary>
/// If this component lands on top of a lava tile it will be deleted.
/// </summary>
[RegisterComponent, Access(typeof(LavaSystem))]
public sealed class LavaDisintegrationComponent : Component
{

}
