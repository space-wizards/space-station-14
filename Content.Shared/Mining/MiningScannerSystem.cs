using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Mining.Components;
using Content.Shared.Mobs.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.Mining;

public sealed class MiningScannerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MiningScannerComponent, EntGotInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<MiningScannerComponent, EntGotRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<MiningScannerComponent, ItemToggledEvent>(OnToggled);
    }

    private void OnInserted(Entity<MiningScannerComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        UpdateViewerComponent(args.Container.Owner);
    }

    private void OnRemoved(Entity<MiningScannerComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        UpdateViewerComponent(args.Container.Owner);
    }

    private void OnToggled(Entity<MiningScannerComponent> ent, ref ItemToggledEvent args)
    {
        if (_container.TryGetContainingContainer((ent.Owner, null, null), out var container))
            UpdateViewerComponent(container.Owner);
    }

    public void UpdateViewerComponent(EntityUid uid)
    {
        // Don't do updates on non-mobs.
        if (!HasComp<MobStateComponent>(uid))
            return;

        Entity<MiningScannerComponent>? scannerEnt = null;

        if (_inventory.TryGetContainerSlotEnumerator(uid, out var enumerator))
        {
            while (enumerator.MoveNext(out var slot))
            {
                foreach (var ent in slot.ContainedEntities)
                {
                    if (!TryComp<MiningScannerComponent>(ent, out var scannerComponent) ||
                        !TryComp<ItemToggleComponent>(ent, out var toggle))
                        continue;

                    if (!toggle.Activated)
                        continue;

                    if (scannerEnt == null || scannerComponent.Range > scannerEnt.Value.Comp.Range)
                       scannerEnt = (ent, scannerComponent);
                }
            }
        }

        foreach (var hand in _hands.EnumerateHands(uid))
        {
            if (!TryComp<MiningScannerComponent>(hand.HeldEntity, out var scannerComponent) ||
                !TryComp<ItemToggleComponent>(hand.HeldEntity, out var toggle))
                continue;

            if (!toggle.Activated)
                continue;

            if (scannerEnt == null || scannerComponent.Range > scannerEnt.Value.Comp.Range)
                scannerEnt = (hand.HeldEntity.Value, scannerComponent);
        }

        if (_net.IsServer)
        {
            if (scannerEnt == null)
            {
                if (TryComp<MiningScannerViewerComponent>(uid, out var viewer))
                    viewer.QueueRemoval = true;
            }
            else
            {
                var viewer = EnsureComp<MiningScannerViewerComponent>(uid);
                viewer.ViewRange = scannerEnt.Value.Comp.Range;
                viewer.QueueRemoval = false;
                viewer.NextPingTime = _timing.CurTime + viewer.PingDelay;
                Dirty(uid, viewer);
            }
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<MiningScannerViewerComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var viewer, out var xform))
        {
            if (viewer.QueueRemoval)
            {
                RemCompDeferred(uid, viewer);
                continue;
            }

            if (_timing.CurTime < viewer.NextPingTime)
                continue;

            viewer.NextPingTime = _timing.CurTime + viewer.PingDelay;
            viewer.LastPingLocation = xform.Coordinates;
            _audio.PlayEntity(viewer.PingSound, uid, uid);
        }
    }
}
