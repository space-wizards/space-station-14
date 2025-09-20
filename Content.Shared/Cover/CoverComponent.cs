using Robust.Shared.GameStates;
using Robust.Shared.Physics;
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
    public float MaxDistance = 6f;

    /// <summary>
    /// The distance at which the cover is completely ineffective.
    /// </summary>
    [DataField]
    public float MinDistance = 0.9f;

    /// <summary>
    /// Should cover information show on examine?
    /// Requires suitable fixtures on <see cref="CollisionGroup.Impassable"/> or <see cref="CollisionGroup.BulletImpassable"/>
    /// Requires cvar
    /// </summary>
    [DataField]
    public bool ShowExamine = true;
}
