// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.DoAfter;
using Content.Shared.Inventory;

namespace Content.Shared.DeadSpace.TheCircle.Dreadnought;

public sealed class DreadnoughtBuckleSystem : EntitySystem
{
    private const string OuterClothingSlot = "outerClothing";
    private readonly HashSet<(EntityUid Buckle, EntityUid Strap)> _completedBuckleAttempts = [];

    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StrapComponent, StrapAttemptEvent>(OnStrapAttempt);
        SubscribeLocalEvent<StrapComponent, DreadnoughtBuckleDoAfterEvent>(OnBuckleDoAfter);
    }

    private void OnStrapAttempt(Entity<StrapComponent> ent, ref StrapAttemptEvent args)
    {
        if (args.Cancelled || args.User != args.Buckle.Owner)
            return;

        var wearer = args.Buckle.Owner;
        var attempt = (wearer, ent.Owner);
        if (_completedBuckleAttempts.Contains(attempt))
            return;

        if (!_inventory.TryGetSlotEntity(wearer, OuterClothingSlot, out var outerClothing) ||
            !TryComp<DreadnoughtLastStandComponent>(outerClothing.Value, out var dreadnought) ||
            dreadnought.BuckleDelay <= TimeSpan.Zero)
            return;

        args.Cancelled = true;
        var doAfter = new DoAfterArgs(
            EntityManager,
            wearer,
            dreadnought.BuckleDelay,
            new DreadnoughtBuckleDoAfterEvent(),
            ent.Owner,
            target: wearer,
            used: ent.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnBuckleDoAfter(Entity<StrapComponent> ent, ref DreadnoughtBuckleDoAfterEvent args)
    {
        if (args.Cancelled ||
            args.Handled ||
            args.Target is not { } wearer ||
            args.User != wearer ||
            args.Used != ent.Owner ||
            !_inventory.TryGetSlotEntity(wearer, OuterClothingSlot, out var outerClothing) ||
            !HasComp<DreadnoughtLastStandComponent>(outerClothing.Value))
            return;

        var attempt = (wearer, ent.Owner);
        _completedBuckleAttempts.Add(attempt);
        try
        {
            args.Handled = _buckle.TryBuckle(wearer, wearer, ent.Owner, popup: false);
        }
        finally
        {
            _completedBuckleAttempts.Remove(attempt);
        }
    }
}
