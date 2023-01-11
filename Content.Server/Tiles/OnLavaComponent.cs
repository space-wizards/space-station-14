using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Tiles;

/// <summary>
/// Applied to entities currently vaulting lava.
/// </summary>
[RegisterComponent, Access(typeof(LavaSystem))]
public sealed class OnLavaComponent : Component
{
    [DataField("nextUpdate", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan NextUpdate = TimeSpan.Zero;
}
