using Content.Shared.Light.Components;
using Robust.Shared.ComponentTrees;
using Robust.Shared.Physics;

namespace Content.Client.Light.Components;

[RegisterComponent]
public sealed partial class TileEmissionTreeComponent : Component, IComponentTreeComponent<TileEmissionComponent>
{
    public DynamicTree<ComponentTreeEntry<TileEmissionComponent>> Tree { get; set; }
}
