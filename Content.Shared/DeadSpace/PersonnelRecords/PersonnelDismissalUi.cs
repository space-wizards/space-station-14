// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Serialization;

namespace Content.Shared.DeadSpace.PersonnelRecords;

/// <summary>
/// Sent from the ID card console's "Уволить" (Dismiss) button, on
/// <c>enum.IdCardConsoleUiKey.Key</c> - the same BUI key the stock ID card console uses.
/// Handled by a separate DS14 <c>PersonnelDismissalSystem</c> subscribed via
/// <c>Subs.BuiEvents&lt;PersonnelDismissalComponent&gt;</c>, entirely independent of
/// <c>IdCardConsoleSystem</c>'s own subscriptions on <c>IdCardConsoleComponent</c> for the same key
/// (different component, different message type - no collision).
///
/// Carries no data: the target is whatever ID card currently sits in the console's
/// <c>TargetIdSlot</c>, and the acting card is whatever sits in <c>PrivilegedIdSlot</c>, both
/// re-validated on the server exactly like every other ID console action.
/// The client is expected to require two presses ("Вы уверены?") before sending this, but that is
/// purely a UX affordance - the server does not know or care how many times the button was clicked.
/// </summary>
[Serializable, NetSerializable]
public sealed class PersonnelDismissMessage : BoundUserInterfaceMessage
{
}
