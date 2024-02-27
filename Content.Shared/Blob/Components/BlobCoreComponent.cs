using Robust.Shared.GameStates;

namespace Content.Shared.Blob.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedBlobSystem))]
public sealed partial class BlobCoreComponent : Component
{

}
