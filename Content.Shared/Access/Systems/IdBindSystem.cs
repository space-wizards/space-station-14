using Content.Shared.IdentityManagement.Components;
using Content.Shared.Roles;

namespace Content.Shared.Access.Systems;

public sealed class IdBindSystem : EntitySystem
{
    [Dependency] private readonly SharedIdCardSystem _cardSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<IdBindComponent, StartingGearEquippedEvent>(OnGearEquipped);
    }

    private void OnGearEquipped(Entity<IdBindComponent> ent, ref StartingGearEquippedEvent args)
    {
        if (!_cardSystem.TryFindIdCard(ent, out var cardId))
            return;

        if (TryComp<MetaDataComponent>(ent, out var data))
            _cardSystem.TryChangeFullName(cardId, data.EntityName, cardId);
    }
}

