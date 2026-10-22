using System.Linq;
using Content.Shared._MACRO.Body.Components;
using Content.Shared.Body;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared._MACRO.Body.EntitySystems;

public sealed partial class EquipmentOrganSystem : EntitySystem
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EquipmentOrganComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<EquipmentOrganComponent, OrganGotInsertedEvent>(OnGotInserted);
        SubscribeLocalEvent<EquipmentOrganComponent, OrganGotRemovedEvent>(OnGotRemoved);
    }

    private void OnMapInit(Entity<EquipmentOrganComponent> ent, ref MapInitEvent args)
    {
        _container.EnsureContainer<Container>(ent, ent.Comp.ContainerId);

        // Spawn equipment
        foreach (var entProtoId in ent.Comp.Equipment)
        {
            InsertEquipment(ent, entProtoId);
        }
    }

    private void InsertEquipment(Entity<EquipmentOrganComponent> ent, EntProtoId entProtoId)
    {
        if (!PredictedTrySpawnInContainer(entProtoId, ent.Owner, ent.Comp.ContainerId, out var item))
            return;

        Comp<OrganAttachedComponent>(item.Value).AttachedOrgan = ent;
    }

    private void OnGotInserted(Entity<EquipmentOrganComponent> ent, ref OrganGotInsertedEvent args)
    {
        if (!_container.TryGetContainer(ent,ent.Comp.ContainerId, out var container))
            return;

        foreach (var organ in container.ContainedEntities.ToList())
        {
            var organComp = Comp<OrganAttachedComponent>(organ);

            if (organComp.HandEquipment)
            {
                _hands.TryForcePickup(
                    args.Target,
                    organ,
                    organComp.Slot,
                    checkActionBlocker: false);
            }
            else
            {
                _inventory.TryEquip(args.Target,
                    organ,
                    organComp.Slot,
                    predicted:true,
                    silent: true,
                    force: true);
            }

            EnsureComp<UnremoveableComponent>(organ);
        }
    }

    private void OnGotRemoved(Entity<EquipmentOrganComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        var container = _container.EnsureContainer<Container>(ent, ent.Comp.ContainerId);

        var enumerator = _inventory.GetHandOrInventoryEntities(ent.AsType()).ToList();

        foreach (var item in enumerator)
        {
            if (!TryComp<OrganAttachedComponent>(item, out var organComp))
                continue;

            // Only remove it if you're the one who owns it.
            if (organComp.AttachedOrgan != ent)
                continue;

            RemComp<UnremoveableComponent>(item);
            _container.Insert(item, container);
        }
    }
}
