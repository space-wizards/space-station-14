using Robust.Shared.GameStates;

namespace Content.Shared.Storage.Components;

/// <summary>
/// Added to entities contained within entity storage, for directed event purposes.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class InsideEntityStorageComponent : Component
{
    /// <summary>
    /// The entity storage this entity is inside.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid Storage;
}
