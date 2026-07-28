namespace Content.Shared.Stacks;

/// <summary>
///     Raised on the original stack entity when it is split to create another.
/// </summary>
/// <param name="NewId">The entity id of the new stack.</param>
[ByRefEvent]
public readonly record struct StackSplitEvent(EntityUid NewId, EntityUid? user);

/// <summary>
///     Raised on the recipient stack entity when it is merged with another stack.
///     This is raised before the stack counts have been updated.
/// </summary>
/// <param name="Donor">The entity id of the donor stack. May get deleted after event is resolved from merging.</param>
/// <param name="Recipient">The entity id of the recipient stack.</param>
/// <param name="Amount">The amount in the stack transferred from the donor to the recipient</param>
[ByRefEvent]
public readonly record struct StackMergeEvent(EntityUid Donor, EntityUid Recipient, int Amount);
