namespace Content.Server.Storage;

/// <summary>
/// This is used for restricting anchor operations on storage (one bag max per tile)
/// and ejecting sapient contents on anchor.
/// </summary>
[RegisterComponent]
public sealed partial class AnchorableStorageComponent : Component;
