using Content.Shared.Actions.ActionTypes;

namespace Content.Client.Actions;

/// <summary>
///     This event is raised when a user clicks on an empty action slot. Enables other systems to fill this slow.
/// </summary>
public sealed class FillActionSlotEvent : EntityEventArgs
{
    public ActionType? Action;
}
