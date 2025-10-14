using Content.Shared.Actions;

namespace Content.Shared.Store.Events;

/// <summary>
/// Opens a store specified by <see cref="StoreComponent"/>
/// Used for entities with a store built into themselves like Revenant or PAI
/// </summary>
public sealed partial class IntrinsicStoreActionEvent : InstantActionEvent
{
}
