using Robust.Shared.ComponentTrees;
using Robust.Shared.GameStates;
using Robust.Shared.Physics;

namespace Content.Shared.Wall;

/// <summary>
/// Marks an entity as wall-mounted.
/// Allows interaction through other anchored entities on the same tile within the <see cref="Arc"/>.
/// Hides the sprite when viewed from outside that arc.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class WallMountComponent : Component, IComponentTreeEntry<WallMountComponent>
{
    /// <summary>
    /// Range of angles where interaction through other anchored entities on the same tile is allowed.
    /// Bigger is more permissive.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Angle Arc = new(MathF.PI);

    /// <summary>
    /// The direction the allowed angle range faces, relative to the entity's rotation.
    /// Defaults to south.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Angle Direction = Angle.Zero;

    /// <summary>
    /// If true, the sprite is only visible from within the facing <see cref="Arc"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool DirectionalVisibility = true;

    [ViewVariables]
    public EntityUid? TreeUid { get; set; }

    [ViewVariables]
    public DynamicTree<ComponentTreeEntry<WallMountComponent>>? Tree { get; set; }

    [ViewVariables]
    public bool AddToTree => Arc < Math.Tau && DirectionalVisibility;

    public bool TreeUpdateQueued { get; set; }
}
