using Content.Shared.Wall;
using Robust.Shared.ComponentTrees;
using Robust.Shared.Physics;

namespace Content.Client.Wall.Components;

[RegisterComponent]
public sealed partial class WallMountTreeComponent : Component, IComponentTreeComponent<WallMountComponent>
{
    public DynamicTree<ComponentTreeEntry<WallMountComponent>> Tree { get; set; } = default!;
}
