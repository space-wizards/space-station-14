namespace Content.Server.Storage.Components;

/// <summary>
///     Added to entities contained within entity storage, for directed event purposes.
/// </summary>
[RegisterComponent]
public sealed partial class InsideEntityStorageComponent : Component
{
    public EntityUid Storage;
}
