using Robust.Shared.GameStates;

namespace Content.Shared.Blob.Components;

/// <summary>
/// This is used for any mob or structure that is created by a blob and belongs to it.
/// This includes all blob tiles as well as spores and blobbernauts.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedBlobSystem))]
[AutoGenerateComponentState]
public sealed partial class BlobCreatedComponent : Component
{
    /// <summary>
    /// The blob marker that created this
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Creator;
}
