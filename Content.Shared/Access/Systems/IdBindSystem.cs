using Content.Shared.IdentityManagement.Components;
using Content.Shared.Roles;

namespace Content.Shared.Access.Systems;

public sealed class IdBindSystem : EntitySystem
{
    [Dependency] private readonly SharedIdCardSystem _cardSystem = default!;

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

        if (TryComp<MetaDataComponent>(ent, out var data))
            _cardSystem.TryChangeFullName(cardId, data.EntityName, cardId);
    }
}

