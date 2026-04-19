using Content.Server.Hands.Systems;
using Content.Shared.Item.ItemToggle.Components;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Checks if the item in the active hand has <see cref="ItemToggleComponent"/> and whether its
/// <see cref="ItemToggleComponent.Activated"/> state matches the expected value.
/// </summary>
public sealed partial class ItemTogglePrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    /// <summary>
    /// The activation state to check for.
    /// </summary>
    [DataField]
    public bool Activated = true;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<string>(NPCBlackboard.ActiveHand, out var activeHand, _entManager))
            return false;

        var handsSystem = _entManager.System<HandsSystem>();
        if (!handsSystem.TryGetHeldItem(owner, activeHand, out var heldEntity))
            return false;

        if (!_entManager.TryGetComponent<ItemToggleComponent>(heldEntity, out var itemToggle))
            return false;

        if (!itemToggle.OnUse)
            return false;

        return itemToggle.Activated == Activated;
    }
}
