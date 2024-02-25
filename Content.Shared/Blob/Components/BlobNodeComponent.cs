using Robust.Shared.GameStates;

namespace Content.Shared.Blob.Components;

/// <summary>
/// This is used for a blob structure that pulses other nearby blob structures.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedBlobSystem))]
public sealed partial class BlobNodeComponent : Component
{

}
