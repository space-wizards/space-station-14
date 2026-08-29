using Content.Shared.Cuffs;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Shared.Item;

public sealed partial class ForcedItemStatusEffectSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private StatusEffectsSystem _status = default!;

    public override void Initialize()
    {
        Subs.SubscribeWithRelay<ForcedItemStatusEffectItemComponent, BeforeTargetHandcuffedEvent>(OnBeforeGotHandcuffed);
    }

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

            if (_hands.TryForcePickupAnyHand(args.Target, spawned, false))
            {
                if (entity.Comp.Unremovable)
                    EnsureComp<UnremoveableComponent>(spawned);

                EnsureComp<ForcedItemStatusEffectItemComponent>(spawned, out var spawnedComp);
                spawnedComp.StatusEffect = entity;
                spawnedComp.RemoveWhenCuffed = entity.Comp.RemoveWhenCuffed;
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
        // Same reason as above.
        if (!_net.IsServer)
            return;

        if (!entity.Comp.SuccessfullySpawned)
            return;

        if (entity.Comp.ItemEntities.Count == 0)
            return;

        foreach (var item in entity.Comp.ItemEntities)
        {
            DisposeItem(item, entity, args.Target, false);
        }

        _audio.PlayPvs(entity.Comp.DespawnSound, args.Target);
    }

    [SubscribeLocalEvent]
    private void OnItemShutdown(Entity<ForcedItemStatusEffectItemComponent> entity, ref ComponentShutdown args)
    {
        if (!TryComp<ForcedItemStatusEffectComponent>(entity.Comp.StatusEffect, out var effect))
            return;

        effect.ItemEntities.Remove(entity);
        Dirty(entity.Comp.StatusEffect.Value, effect);
    }

    private void OnBeforeGotHandcuffed(Entity<ForcedItemStatusEffectItemComponent> entity, ref BeforeTargetHandcuffedEvent args)
    {
        if (!entity.Comp.RemoveWhenCuffed)
            return;

        if (!TryComp<ForcedItemStatusEffectComponent>(entity.Comp.StatusEffect, out var forcedItem))
            return;

        if (!TryComp<StatusEffectComponent>(entity.Comp.StatusEffect, out var status) || status.AppliedTo == null)
            return;

        DisposeItem(entity, (entity.Comp.StatusEffect.Value, forcedItem), status.AppliedTo.Value, true, args.User);
    }

    [SubscribeLocalEvent]
    private void OnGotHandcuffed(Entity<ForcedItemStatusEffectComponent> entity, ref StatusEffectRelayedEvent<TargetHandcuffedEvent> args)
    {
        if (!entity.Comp.RemoveWhenCuffed)
            return;

        if (!TryComp<StatusEffectComponent>(entity, out var status) || status.AppliedTo == null)
            return;

        if (MetaData(entity).EntityPrototype?.ID is not { } entProto)
            return;

        _status.TryRemoveStatusEffect(status.AppliedTo.Value, entProto);
    }

    private void DisposeItem(EntityUid entity, Entity<ForcedItemStatusEffectComponent> status, EntityUid holder, bool playSound = true, EntityUid? user = null)
    {
        // Unremovable causes issues with predicted deletion so we get rid of it first.
        if (HasComp<UnremoveableComponent>(entity))
            RemComp<UnremoveableComponent>(entity);

        // Cleanup is handled via shutdown of the item.
        PredictedQueueDel(entity);

        if (!playSound)
            return;

        // We want to predict the audio only if we have a user.
        if (user == null && _net.IsServer)
            _audio.PlayPvs(status.Comp.DespawnSound, holder);
        else if (user != null)
            _audio.PlayPredicted(status.Comp.DespawnSound, holder, user.Value);
    }

    private bool SpawnItemInInventory(Entity<ForcedItemStatusEffectComponent> ent, EntityUid target, string slot)
    {
        if (!_inventory.SpawnItemInSlot(target, slot, ent.Comp.Item, out var item, true, force: ent.Comp.Force))
            return false;

        if (ent.Comp.Unremovable)
            EnsureComp<UnremoveableComponent>(item.Value);

        EnsureComp<ForcedItemStatusEffectItemComponent>(item.Value, out var spawnedComp);
        spawnedComp.StatusEffect = ent;
        spawnedComp.RemoveWhenCuffed = ent.Comp.RemoveWhenCuffed;
        ent.Comp.ItemEntities.Add(item.Value);

        Dirty(item.Value, spawnedComp);
        return true;
    }
}
