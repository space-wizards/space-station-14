using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Shared.Item;

public sealed partial class ForcedItemStatusEffectSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    [SubscribeLocalEvent]
    private void OnEffectApplied(Entity<ForcedItemStatusEffectComponent> entity, ref StatusEffectAppliedEvent args)
    {
        // "Why the IsServer check? Cannot this be predicted?"
        // Yes and no. Status effects cannot really have a "user", as such we cannot play the sound in a predicted way.
        // It only works for the local client who had this status effect applied to them.
        // The audio could be the only unpredicted thing, but the audio and visual being not predicted
        // makes it extremely obvious as to this being a fake item and not for example being a real armblade or some shit.
        // Basically, if we want to sync it up we need self-predicting audio, like how popups do it.
        // Also container prediction makes this a nightmare too but thats a lesser issue
        if (!_net.IsServer)
            return;

        if (entity.Comp.SuccessfullySpawned)
            return;

        if (entity.Comp.Hands)
        {
            var spawned = SpawnAtPosition(entity.Comp.Item, Transform(entity).Coordinates);

            if (_hands.TryForcePickupAnyHand(args.Target, spawned))
            {
                if (entity.Comp.Unremovable)
                    EnsureComp<UnremoveableComponent>(spawned);

                EnsureComp<ForcedItemStatusEffectItemComponent>(spawned, out var spawnedComp);
                spawnedComp.StatusEffect = entity;
                entity.Comp.ItemEntities.Add(spawned);

                entity.Comp.SuccessfullySpawned = true;
                Dirty(spawned, spawnedComp);
            }
            else
            {
                QueueDel(spawned);
            }
        }

        if (entity.Comp.Slots != SlotFlags.NONE)
        {
            var slots = _inventory.GetSlotEnumerator(args.Target, entity.Comp.Slots);
            while (slots.MoveNext(out var container))
            {
                if (container.ContainedEntity != null && entity.Comp.DropExisting)
                {
                    if (_inventory.TryUnequip(args.Target, container.ID, true, entity.Comp.Force) &&
                        SpawnItemInInventory(entity, args.Target, container.ID))
                    {
                        entity.Comp.SuccessfullySpawned = true;
                        continue;
                    }
                }

                if (container.ContainedEntities.Count == 0 && SpawnItemInInventory(entity, args.Target, container.ID))
                    entity.Comp.SuccessfullySpawned = true;
            }
        }

        if (!entity.Comp.SuccessfullySpawned)
            return;


        _audio.PlayPvs(entity.Comp.SpawnSound, args.Target);
        Dirty(entity);
    }

    [SubscribeLocalEvent]
    private void OnEffectRemoved(Entity<ForcedItemStatusEffectComponent> entity, ref StatusEffectRemovedEvent args)
    {
        // Same reason as with the event above.
        if (!_net.IsServer)
            return;

        if (!entity.Comp.SuccessfullySpawned)
            return;

        foreach (var item in entity.Comp.ItemEntities)
        {
            PredictedQueueDel(item);
        }

        entity.Comp.ItemEntities = new();
        _audio.PlayPvs(entity.Comp.DespawnSound, args.Target);

        Dirty(entity);
    }

    [SubscribeLocalEvent]
    private void OnItemShutdown(Entity<ForcedItemStatusEffectItemComponent> entity, ref ComponentShutdown args)
    {
        if (!TryComp<ForcedItemStatusEffectComponent>(entity.Comp.StatusEffect, out var effect))
            return;

        effect.ItemEntities.Remove(entity);
        Dirty(entity.Comp.StatusEffect.Value, effect);
    }

    private bool SpawnItemInInventory(Entity<ForcedItemStatusEffectComponent> ent, EntityUid target, string slot)
    {
        if (!_inventory.SpawnItemInSlot(target, slot, ent.Comp.Item, out var item, true, force: ent.Comp.Force))
            return false;

        if (ent.Comp.Unremovable)
            EnsureComp<UnremoveableComponent>(item.Value);

        EnsureComp<ForcedItemStatusEffectItemComponent>(item.Value, out var spawnedComp);
        spawnedComp.StatusEffect = ent;
        ent.Comp.ItemEntities.Add(item.Value);

        Dirty(item.Value, spawnedComp);
        return true;
    }
}
