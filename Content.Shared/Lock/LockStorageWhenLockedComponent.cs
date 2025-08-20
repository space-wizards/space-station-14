using Robust.Shared.GameStates;

namespace Content.Shared.Lock;

/// <summary>
/// When this component is added to an entity, its storage will be locked when the item is locked.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class LockStorageWhenLockedComponent : Component;
