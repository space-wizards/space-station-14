using Content.Shared.Interaction;
using Content.Shared.Lock;
using Content.Shared.Materials;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.Events;

namespace Content.Shared.Storage.EntitySystems;

/// <summary>
/// Uses the bag's normal dump do-after to deposit its contents through the target's insertion rules.
/// </summary>
public sealed partial class BagDepositSystem : EntitySystem
{
    [Dependency] private DumpableSystem _dumpable = default!;
    [Dependency] private LockSystem _lock = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;
    [Dependency] private SharedStorageSystem _storage = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BagDepositComponent, InteractUsingEvent>(OnInteractUsing,
            before: new[] { typeof(SharedStorageSystem), typeof(SharedMaterialStorageSystem), typeof(SharedMaterialReclaimerSystem) });
        SubscribeLocalEvent<BagDepositComponent, GetDumpableVerbEvent>(OnGetDumpableVerb);
        SubscribeLocalEvent<BagDepositComponent, DumpEvent>(OnDump);
    }

    private void OnInteractUsing(Entity<BagDepositComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !CanDeposit(ent) || _lock.IsLocked(args.Used)
            || !TryComp<DumpableComponent>(args.Used, out var dumpable)
            || !TryComp<StorageComponent>(args.Used, out var storage)
            || storage.Container.ContainedEntities.Count == 0)
        {
            return;
        }

        _dumpable.StartDoAfter(args.Used, ent, args.User, dumpable);
        args.Handled = true;
    }

    private void OnGetDumpableVerb(Entity<BagDepositComponent> ent, ref GetDumpableVerbEvent args)
    {
        if (CanDeposit(ent))
            args.Verb = Loc.GetString("storage-component-transfer-verb");
    }

    private void OnDump(Entity<BagDepositComponent> ent, ref DumpEvent args)
    {
        if (args.Handled)
            return;

        // Failed deposits leave their items in the bag instead of falling back to dumping on the ground.
        args.Handled = true;
        if (!CanDeposit(ent))
            return;

        foreach (var item in args.DumpQueue)
        {
            if (TerminatingOrDeleted(item) || EntityManager.IsQueuedForDeletion(item)
                || !_storage.TryGetContainingStorage(item, out var source) || _lock.IsLocked(source.Value.Owner))
            {
                continue;
            }

            var ev = new BagDepositAttemptEvent(args.User, item);
            RaiseLocalEvent(ent, ref ev);
            args.PlaySound |= ev.Handled;
        }
    }

    private bool CanDeposit(EntityUid uid)
    {
        return _power.IsPowered(uid) && !_lock.IsLocked(uid)
            && (!TryComp<MaterialReclaimerComponent>(uid, out var reclaimer)
                || reclaimer.Powered && reclaimer.Enabled && !reclaimer.Broken);
    }
}
