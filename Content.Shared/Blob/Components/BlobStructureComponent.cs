using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared.Blob.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedBlobSystem))]
[AutoGenerateComponentState]
public sealed partial class BlobStructureComponent : Component
{
    /// <summary>
    /// Whether or not the node is currently being pulsed by a nearby <see cref="BlobNodeComponent"/>
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Pulsed;
}
