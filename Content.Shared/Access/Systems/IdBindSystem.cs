using Content.Shared.IdentityManagement.Components;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.Roles;

namespace Content.Shared.Access.Systems;

public sealed class IdBindSystem : EntitySystem
{
    [Dependency] private readonly SharedIdCardSystem _cardSystem = default!;
    [Dependency] private readonly SharedPdaSystem _pdaSystem = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();
        //Activate on being added or when starting gear is equipped
        SubscribeLocalEvent<IdBindComponent, StartingGearEquippedEvent>(StartAttempt);
        SubscribeLocalEvent<IdBindComponent, ComponentInit>(StartAttempt);
    }

    private void StartAttempt(Entity<IdBindComponent> ent, ref StartingGearEquippedEvent args)
    {
        TryBind(ent);
    }

    private void StartAttempt(Entity<IdBindComponent> ent, ref ComponentInit args)
    {
        TryBind(ent);
    }

    private void TryBind(Entity<IdBindComponent> ent)
    {
        if (!_cardSystem.TryFindIdCard(ent, out var cardId))
            return;

        if (!TryComp<MetaDataComponent>(ent, out var data))
            return;

        _cardSystem.TryChangeFullName(cardId, data.EntityName, cardId);

        if (!ent.Comp.BindPDAOwner)
            return;

        //Get PDA from main slot and set us as owner
        if (!_inventory.TryGetSlotEntity(ent, "id", out var uPda))
            return;

        if (!TryComp<PdaComponent>(uPda, out var pDA))
            return;

        _pdaSystem.SetOwner(uPda.Value, pDA, data.EntityName);
    }
}

