using Robust.Shared.GameStates;

namespace Content.Shared.Placeable;

/// <summary>
/// Utility for <see cref="ItemPlacerSystem"/> that stores the original state of
/// a <see cref="CollisionWakeComponent"/> for later restoring.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ItemPlacerSystem))]
public sealed partial class StoredCollisionWakeComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool OriginalEnabled = true;
}

