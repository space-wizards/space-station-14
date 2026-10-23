using Robust.Shared.GameStates;

namespace Content.Shared.Storage.Components;

/// <summary>
/// Allows bags with <see cref="DumpableComponent"/> to deposit their contents into this entity after a do-after.
/// Individual items are accepted by handlers for <see cref="Events.BagDepositAttemptEvent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BagDepositComponent : Component;
