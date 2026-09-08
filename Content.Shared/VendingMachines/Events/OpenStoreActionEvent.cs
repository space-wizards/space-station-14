using Content.Shared.Actions;

namespace Content.Shared.VendingMachines.Events;

/// <summary>
/// Event fired by local action to open the store.
/// Used by the ghost role mainly.
/// </summary>
public sealed partial class OpenStoreActionEvent : InstantActionEvent;
