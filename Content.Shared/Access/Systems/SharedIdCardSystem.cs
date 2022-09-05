using System.Diagnostics.CodeAnalysis;
using Content.Shared.Access.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Robust.Shared.GameStates;

namespace Content.Shared.Access.Systems;

public abstract class SharedIdCardSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventorySystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IdCardComponent, ComponentGetState>(OnComponentGetState);
        SubscribeLocalEvent<IdCardComponent, ComponentHandleState>(OnComponentHandleState);
    }

    private void OnComponentGetState(EntityUid uid, IdCardComponent component, ref ComponentGetState args)
    {
        args.State = new IdCardComponentState(component.FullName, component.JobTitle);
    }

    private void OnComponentHandleState(EntityUid uid, IdCardComponent component, ref ComponentHandleState args)
    {
        if (args.Current is IdCardComponentState state)
        {
            component.FullName = state.FullName;
            component.JobTitle = state.JobTitle;
        }
    }

    /// <summary>
    ///     Attempt to find an ID card on an entity. This will look in the entity itself, in the entity's hands, and
    ///     in the entity's inventory.
    /// </summary>
    public bool TryFindIdCard(EntityUid uid, [NotNullWhen(true)] out IdCardComponent? idCard)
    {
        // check held item?
        if (EntityManager.TryGetComponent(uid, out SharedHandsComponent? hands) &&
            hands.ActiveHandEntity is EntityUid heldItem &&
            TryGetIdCard(heldItem, out idCard))
        {
            return true;
        }

        // check entity itself
        if (TryGetIdCard(uid, out idCard))
            return true;

        // check inventory slot?
        if (_inventorySystem.TryGetSlotEntity(uid, "id", out var idUid) && TryGetIdCard(idUid.Value, out idCard))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Attempt to get an id card component from an entity, either by getting it directly from the entity, or by
    ///     getting the contained id from a <see cref="PDAComponent"/>.
    /// </summary>
    public bool TryGetIdCard(EntityUid uid, [NotNullWhen(true)] out IdCardComponent? idCard)
    {
        if (EntityManager.TryGetComponent(uid, out idCard))
            return true;

        if (EntityManager.TryGetComponent(uid, out PDAComponent? pda) && pda.ContainedID != null)
        {
            idCard = pda.ContainedID;
            return true;
        }

        return false;
    }
}
