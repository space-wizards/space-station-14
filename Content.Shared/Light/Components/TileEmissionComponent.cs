using Content.Shared.Audio;
using Robust.Shared.ComponentTrees;
using Robust.Shared.GameStates;
using Robust.Shared.Physics;

namespace Content.Shared.Light.Components;

/// <summary>
/// Will draw lighting in a range around the tile.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TileEmissionComponent : Component, IComponentTreeEntry<TileEmissionComponent>
{
    [DataField, AutoNetworkedField]
    public float Range = 0.25f;

    [DataField(required: true), AutoNetworkedField]
    public Color Color = Color.Transparent;

    public EntityUid? TreeUid { get; set; }
    public DynamicTree<ComponentTreeEntry<TileEmissionComponent>>? Tree { get; set; }
    public bool AddToTree => Range > 0f && !Color.Equals(Color.Transparent);
    public bool TreeUpdateQueued { get; set; }
}
