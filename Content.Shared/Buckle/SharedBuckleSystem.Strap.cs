using System.Linq;
using Content.Shared.Buckle.Components;
using Content.Shared.Construction;
using Content.Shared.Destructible;
using Content.Shared.Foldable;
using Content.Shared.Storage;
using Robust.Shared.Containers;

namespace Content.Shared.Buckle;

public abstract partial class SharedBuckleSystem
{
    private void InitializeStrap()
    {
        SubscribeLocalEvent<StrapComponent, ComponentRemove>((e, ref _) => StrapRemoveAll(e));
        SubscribeLocalEvent<StrapComponent, DestructionEventArgs>((e, ref _) => StrapRemoveAll(e));
        SubscribeLocalEvent<StrapComponent, BreakageEventArgs>((e, ref _) => StrapRemoveAll(e));
        SubscribeLocalEvent<StrapComponent, MachineDeconstructedEvent>((e, ref _) => StrapRemoveAll(e));
    }

    [SubscribeLocalEvent]
    private void OnStrapStartup(Entity<StrapComponent> ent, ref ComponentStartup args)
    {
        Appearance.SetData(ent, StrapVisuals.State, ent.Comp.BuckledEntities.Count != 0);

        // Raise events on anything that starts buckled.
        foreach (var buckle in ent.Comp.BuckledEntities)
        {
            if (!TryComp<BuckleComponent>(buckle, out var buckleComp))
                continue;

            var ev = new StrappedEvent(ent, (buckle, buckleComp));
            RaiseLocalEvent(ent, ref ev);

            var gotEv = new BuckledEvent(ent, (buckle, buckleComp));
            RaiseLocalEvent(buckle, ref gotEv);
        }
    }

    [SubscribeLocalEvent]
    private void OnStrapShutdown(Entity<StrapComponent> ent, ref ComponentShutdown args)
    {
        if (!TerminatingOrDeleted(ent))
            StrapRemoveAll(ent);
    }

    [SubscribeLocalEvent]
    private void OnStrapTerminating(Entity<StrapComponent> ent, ref EntityTerminatingEvent args)
    {
        StrapRemoveAll(ent);
    }

    [SubscribeLocalEvent]
    private void OnStrapContainerGettingInsertedAttempt(Entity<StrapComponent> ent, ref ContainerGettingInsertedAttemptEvent args)
    {
        // If someone is attempting to put this item inside of a backpack, ensure that it has no entities strapped to it.
        if (args.Container.ID == StorageComponent.ContainerId && ent.Comp.BuckledEntities.Count != 0)
            args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnAttemptFold(Entity<StrapComponent> ent, ref FoldAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        args.Cancelled = ent.Comp.BuckledEntities.Count != 0;
    }

    /// <summary>
    /// Remove everything attached to the strap
    /// </summary>
    private void StrapRemoveAll(Entity<StrapComponent> ent)
    {
        foreach (var buckle in ent.Comp.BuckledEntities.ToArray())
        {
            Unbuckle(buckle, buckle);
        }
    }

    private bool StrapHasSpace(EntityUid strapUid, BuckleComponent buckleComp, StrapComponent? strapComp = null)
    {
        if (!Resolve(strapUid, ref strapComp, false))
            return false;

        var avail = strapComp.Size;
        foreach (var buckle in strapComp.BuckledEntities)
        {
            avail -= CompOrNull<BuckleComponent>(buckle)?.Size ?? 0;
        }

        return avail >= buckleComp.Size;
    }

    /// <summary>
    /// Sets the enabled field in the strap component to a value
    /// </summary>
    public void StrapSetEnabled(EntityUid strapUid, bool enabled, StrapComponent? strapComp = null)
    {
        if (!Resolve(strapUid, ref strapComp, false) ||
            strapComp.Enabled == enabled)
            return;

        strapComp.Enabled = enabled;
        Dirty(strapUid, strapComp);

        if (!enabled)
            StrapRemoveAll((strapUid, strapComp));
    }
}
