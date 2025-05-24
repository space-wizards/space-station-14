using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.NPC.Components;
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
        SubscribeLocalEvent<FactionClothingComponent, InventoryRelayedEvent<NpcFactionSystem.TryRemoveFactionAttemptEvent>>(OnTryRemoveFaction);
    }

    private void OnEquipped(Entity<FactionClothingComponent> ent, ref GotEquippedEvent args)
    {
        EnsureComp<NpcFactionMemberComponent>(args.Equipee, out var factionComp);
        var faction = (args.Equipee, factionComp);
        _faction.AddFaction(faction, ent.Comp.Faction);
    }

    private void OnUnequipped(Entity<FactionClothingComponent> ent, ref GotUnequippedEvent args)
    {
        if (!TryComp<NpcFactionMemberComponent>(args.Equipee, out var factionComp))
            return;
        var faction = (args.Equipee, factionComp);
        if (!_faction.IsStartingMember(faction, ent.Comp.Faction))
        {
            _faction.RemoveFaction(args.Equipee, ent.Comp.Faction);
        }
    }

    private void OnTryRemoveFaction(Entity<FactionClothingComponent> ent, ref InventoryRelayedEvent<NpcFactionSystem.TryRemoveFactionAttemptEvent> args)
    {
        if (ent.Comp.Faction == args.Args.Faction)
        {
            args.Args.Cancel();
        }
    }
}
