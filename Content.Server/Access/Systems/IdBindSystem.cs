using Content.Server.Access.Components;
using Content.Server.PDA;
using Content.Shared.Inventory;
using Content.Shared.Mind.Components;
using Content.Shared.PDA;
using Content.Shared.Roles;

namespace Content.Server.Access.Systems;

public sealed class IdBindSystem : EntitySystem
{
    [Dependency] private readonly IdCardSystem _cardSystem = default!;
    [Dependency] private readonly PdaSystem _pdaSystem = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();
        //Activate on mind being added
        SubscribeLocalEvent<IdBindComponent, MindAddedMessage>(TryBind);
    }

    private void TryBind(Entity<IdBindComponent> ent, ref MindAddedMessage args)
    {
        if (!_cardSystem.TryFindIdCard(ent, out var cardId))
            return;

        var data = MetaData(ent);

        _cardSystem.TryChangeFullName(cardId, data.EntityName, cardId);

        if (!ent.Comp.BindPDAOwner)
        {
			//Remove after running once
			RemCompDeferred<IdBindComponent>(ent);
			return;
        }

        //Get PDA from main slot and set us as owner
        if (!_inventory.TryGetSlotEntity(ent, "id", out var uPda))
            return;

        if (!TryComp<PdaComponent>(uPda, out var pDA))
            return;

        _pdaSystem.SetOwner(uPda.Value, pDA, ent, data.EntityName);
        //Remove after running once
        RemCompDeferred<IdBindComponent>(ent);
    }
}

