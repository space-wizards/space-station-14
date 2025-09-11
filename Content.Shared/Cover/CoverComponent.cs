using Robust.Shared.GameStates;
namespace Content.Shared.Cover.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedCoverSystem))]
public sealed partial class CoverComponent : Component
{
    /// <summary>
    /// The maximum cover provided as percent.
    /// </summary>
    [DataField]
    public float CoverPct = 0.5f;

    /// <summary>
    /// The distance at which the cover reaches max effectivness.
    /// </summary>
    [DataField]
    public float MaxDistance = 10f;

    /// <summary>
    /// The distance at which the cover is completely ineffective.
    /// </summary>
    [DataField]
    public float MinDistance = .8f;
}
