using Content.Shared.Wall;
using Robust.Shared.ComponentTrees;
using Robust.Shared.Physics;

namespace Content.Client.Wall.Components;

/// <summary>
/// Stores a component tree of <see cref="WallMountComponent"/>.
/// </summary>
[RegisterComponent]
public sealed partial class WallMountTreeComponent : Component, IComponentTreeComponent<WallMountComponent>
{
    /// <inheritdoc/>
    public DynamicTree<ComponentTreeEntry<WallMountComponent>> Tree { get; set; } = default!;
}
