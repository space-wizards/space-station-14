using Content.Shared.Gibbing;
using Content.Shared.Implants.Components;
using Content.Shared.Storage;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Content.Shared.Antag;
using System.Linq;


namespace Content.Shared.Implants;

public abstract partial class SharedImplanterSystem
{
    public void InitializeImplanted()
    {
        SubscribeLocalEvent<ImplantedComponent, ComponentInit>(OnImplantedInit);
        SubscribeLocalEvent<ImplantedComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ImplantedComponent, GibbedBeforeDeletionEvent>(OnGibbed);
        SubscribeLocalEvent<ImplantedComponent, ComponentGetStateAttemptEvent>(OnImplantedGetStateAttempt);
        // When someone gains ShowAntagIconsComponent mid-round, re-dirty all implanted components
        // so the new observer (admin/ghost) receives the previously hidden states.
        SubscribeLocalEvent<ShowAntagIconsComponent, ComponentStartup>((_, _, _) => DirtyAllImplanted());
    }

    private void OnImplantedGetStateAttempt(EntityUid uid, ImplantedComponent component, ref ComponentGetStateAttemptEvent args)
    {
        args.Cancelled = !CanGetState(uid, args.Player);
    }

    private bool CanGetState(EntityUid uid, ICommonSession? player)
    {
        if (player?.AttachedEntity is not { } attachedUid)
            return true;

        if (HasComp<ShowAntagIconsComponent>(attachedUid))
            return true;

        if (uid == attachedUid)
            return true;

        return false;
    }

    private void DirtyAllImplanted()
    {
        var query = AllEntityQuery<ImplantedComponent>();
        while (query.MoveNext(out var uid, out var comp))
            Dirty(uid, comp);
    }


    private void OnImplantedInit(Entity<ImplantedComponent> ent, ref ComponentInit args)
    {
        ent.Comp.ImplantContainer = _container.EnsureContainer<Container>(ent.Owner, ImplanterComponent.ImplantSlotId);
        ent.Comp.ImplantContainer.OccludesLight = false;
    }

    private void OnShutdown(Entity<ImplantedComponent> ent, ref ComponentShutdown args)
    {
        // If the entity is deleted, get rid of the implants.
        _container.CleanContainer(ent.Comp.ImplantContainer);
    }

    private void OnGibbed(Entity<ImplantedComponent> ent, ref GibbedBeforeDeletionEvent args)
    {
        // Iterate over a snapshot to avoid InvalidOperationException if EmptyContainer
        // modifies the ContainedEntities collection via container events.
        foreach (var implant in ent.Comp.ImplantContainer.ContainedEntities.ToList())
        {
            if (TryComp<StorageComponent>(implant, out var storage))
                _container.EmptyContainer(storage.Container, destination: Transform(ent).Coordinates);
        }
    }
}
