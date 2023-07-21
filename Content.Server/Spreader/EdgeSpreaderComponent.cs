using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
namespace Content.Server.Spreader;

/// <summary>
/// Added to entities being considered for spreading via <see cref="SpreaderSystem"/>.
/// This needs to be manually added and removed.
/// </summary>
[RegisterComponent, Access(typeof(SpreaderSystem))]
public sealed class EdgeSpreaderComponent : Component
{
    [DataField("spreadUpdateCooldown", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan SpreadUpdateCooldown = TimeSpan.FromSeconds(1);
}
