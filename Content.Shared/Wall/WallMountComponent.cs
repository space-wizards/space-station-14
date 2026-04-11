using Robust.Shared.ComponentTrees;
using Robust.Shared.GameStates;
using Robust.Shared.Physics;

namespace Content.Shared.Wall;

/// <summary>
///     This component enables an entity to ignore some obstructions for interaction checks.
/// </summary>
/// <remarks>
///     This will only exempt anchored entities that intersect the wall-mount. Additionally, this exemption will apply
///     in a limited arc, providing basic functionality for directional wall mounts.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class WallMountComponent : Component, IComponentTreeEntry<WallMountComponent>
{
    /// <summary>
    ///     Range of angles for which the exemption applies. Bigger is more permissive.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Angle Arc = new(MathF.PI);

    /// <summary>
    ///     The direction in which the exemption arc is facing, relative to the entity's rotation. Defaults to south.
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
