using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Blob.Components;

/// <summary>
/// This is used for a blob structure that produces resources over time.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedBlobSystem))]
[AutoGenerateComponentState]
public sealed partial class BlobResourceComponent : Component
{
    /// <summary>
    /// The time at which more points are generated.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan NextResourceGen;

    /// <summary>
    /// The delay between each resource generation.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Delay = TimeSpan.FromSeconds(4);

    /// <summary>
    /// An amount added to <see cref="Delay"/> every time resource is generated.
    /// </summary>
    [DataField]
    public TimeSpan DelayAccumulation = TimeSpan.FromSeconds(0.25f);

    /// <summary>
    /// The amount of points created for each generation.
    /// </summary>
    [DataField]
    public int Resource = 1;
}
