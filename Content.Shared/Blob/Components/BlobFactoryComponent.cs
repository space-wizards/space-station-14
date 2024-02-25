using Robust.Shared.GameStates;

namespace Content.Shared.Blob.Components;

/// <summary>
/// This is used for a blob structure that produces blob spores.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedBlobSystem))]
public sealed partial class BlobFactoryComponent : Component
{

}
