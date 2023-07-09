namespace Content.Shared.Storage.Components;

/// <summary>
///     Added to entities contained within entity storage, for directed event purposes.
/// </summary>
[RegisterComponent]
public sealed class InsideEntityStorageComponent : Component
{
    public EntityUid Storage;
}
