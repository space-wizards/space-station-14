using Content.Shared.Storage;
using Robust.Shared.GameStates;

namespace Content.Shared.Lock;

/// <summary>
/// Prevents using an entity's <see cref="StorageComponent"/> if its <see cref="LockComponent"/> is locked.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class LockedStorageComponent : Component;
