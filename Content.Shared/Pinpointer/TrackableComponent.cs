using Robust.Shared.GameStates;

namespace Content.Shared.Pinpointer;

[RegisterComponent, NetworkedComponent]
public sealed partial class TrackableComponent : Component
{
    /// <summary>
    /// A list of entities that is currently tracking this target
    /// </summary>
    [DataField]
    public List<EntityUid> TrackedBy = new ();
}
