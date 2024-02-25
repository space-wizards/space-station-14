using Robust.Shared.GameStates;

namespace Content.Shared.Blob.Components;

/// <summary>
/// This is used for a blob structure that produces resources over time.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedBlobSystem))]
public sealed partial class BlobResourceComponent : Component
{

}
