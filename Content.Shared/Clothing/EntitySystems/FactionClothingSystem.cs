using Content.Shared.Clothing.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.NPC.Systems;

namespace Content.Shared.Clothing.EntitySystems;

/// <summary>
/// Handles <see cref="FactionClothingComponent"/> faction adding and removal.
/// </summary>
public sealed class FactionClothingSystem : EntitySystem
{
    [Dependency] private readonly NpcFactionSystem _faction = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FactionClothingComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<FactionClothingComponent, GotUnequippedEvent>(OnUnequipped);
    }

    private void OnEquipped(Entity<FactionClothingComponent> ent, ref GotEquippedEvent args)
    {
        if (TryComp<NpcFactionMemberComponent>(args.Equipee, out var faction))
            ent.Comp.AlreadyMember = faction.Factions.Contains(ent.Comp.Faction);

        _faction.AddFaction((args.Equipee, faction), ent.Comp.Faction);
    }

    private void OnUnequipped(Entity<FactionClothingComponent> ent, ref GotUnequippedEvent args)
    {
        if (ent.Comp.AlreadyMember)
        {
            ent.Comp.AlreadyMember = false;
            return;
        }

        _faction.RemoveFaction(args.Equipee, ent.Comp.Faction);
    }
}
