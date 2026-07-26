// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Actions;
using Content.Server.Destructible;
using Content.Shared.Buckle.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Damage.Components;
using Content.Shared.DeadSpace.TheCircle.Dreadnought;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Interaction.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Stunnable;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server.DeadSpace.TheCircle.Dreadnought;

public sealed class DreadnoughtLastStandSystem : EntitySystem
{
    private const string OuterClothingSlot = "outerClothing";
    private readonly Dictionary<EntityUid, (EntityUid Wearer, TimeSpan StunDuration)> _pendingStrapDestruction = [];

    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DestructibleSystem _destructible = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _thresholds = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DreadnoughtLastStandComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<DreadnoughtLastStandComponent, DreadnoughtLastStandActionEvent>(OnAction);
        SubscribeLocalEvent<StrapComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<DreadnoughtLastStandActiveComponent, UpdateMobStateEvent>(OnUpdateMobState,
            after: [typeof(MobThresholdSystem)]);
        SubscribeLocalEvent<DreadnoughtLastStandActiveComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<DreadnoughtLastStandActiveComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        SubscribeLocalEvent<DreadnoughtLastStandActiveComponent, ComponentShutdown>(OnActiveShutdown);
    }

    private void OnStrapped(Entity<StrapComponent> ent, ref StrappedEvent args)
    {
        var wearer = args.Buckle.Owner;
        if (!_inventory.TryGetSlotEntity(wearer, OuterClothingSlot, out var outerClothing) ||
            !TryComp<DreadnoughtLastStandComponent>(outerClothing.Value, out var dreadnought))
            return;

        // Buckle() still validates its parent relationship after this event.
        // Destroy the strap on the following update, once buckling has completed.
        _pendingStrapDestruction[ent.Owner] = (wearer, dreadnought.StrapBreakStunDuration);
    }

    private void OnGetActions(Entity<DreadnoughtLastStandComponent> ent, ref GetItemActionsEvent args)
    {
        if (!ent.Comp.Used &&
            args.SlotFlags is { } slotFlags &&
            (slotFlags & ent.Comp.RequiredSlots) != SlotFlags.NONE)
            args.AddAction(ref ent.Comp.ActionEntity, ent.Comp.Action);
    }

    private void OnAction(Entity<DreadnoughtLastStandComponent> ent, ref DreadnoughtLastStandActionEvent args)
    {
        if (args.Handled ||
            ent.Comp.Used ||
            HasComp<DreadnoughtLastStandActiveComponent>(args.Performer) ||
            !TryComp<ClothingComponent>(ent, out var clothing) ||
            clothing.InSlotFlag is not { } slotFlags ||
            (slotFlags & ent.Comp.RequiredSlots) == SlotFlags.NONE ||
            Transform(ent).ParentUid != args.Performer)
            return;

        args.Handled = true;
        ent.Comp.Used = true;
        var active = EnsureComp<DreadnoughtLastStandActiveComponent>(args.Performer);
        active.EndsAt = _timing.CurTime + ent.Comp.Duration;
        active.SpeedModifier = ent.Comp.SpeedModifier;
        active.Expired = false;
        if (!HasComp<IgnoreSlowOnDamageComponent>(args.Performer))
        {
            AddComp<IgnoreSlowOnDamageComponent>(args.Performer);
            active.AppliedIgnoreSlowOnDamage = true;
        }
        _thresholds.SetMobStateThresholds(args.Performer, new SortedDictionary<FixedPoint2, MobState>
        {
            [0] = MobState.Alive,
            [160] = MobState.Dead,
        });
        EnsureComp<UnremoveableComponent>(ent);
        Dirty(args.Performer, active);
        _actions.RemoveAction(args.Performer, ent.Comp.ActionEntity);
        _movement.RefreshMovementSpeedModifiers(args.Performer);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var (strap, pending) in _pendingStrapDestruction)
        {
            if (Deleted(strap))
                continue;

            var coordinates = _transform.GetMapCoordinates(strap);
            _audio.PlayPvs(new SoundCollectionSpecifier("MetalBreak"), strap);
            Spawn("SheetSteel1", coordinates);
            _destructible.DestroyEntity(strap);
            _stun.TryUpdateParalyzeDuration(pending.Wearer, pending.StunDuration);
        }
        _pendingStrapDestruction.Clear();

        var query = EntityQueryEnumerator<DreadnoughtLastStandActiveComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.Expired || _timing.CurTime < component.EndsAt)
                continue;

            component.Expired = true;
            Dirty(uid, component);
            EnsureComp<UnrevivableComponent>(uid);
            _mobState.UpdateMobState(uid);
        }
    }

    private void OnUpdateMobState(Entity<DreadnoughtLastStandActiveComponent> ent, ref UpdateMobStateEvent args)
    {
        if (ent.Comp.Expired)
            args.State = MobState.Dead;
    }

    private void OnExamined(Entity<DreadnoughtLastStandActiveComponent> ent, ref ExaminedEvent args)
    {
        var remaining = TimeSpan.FromTicks(Math.Max(0, (ent.Comp.EndsAt - _timing.CurTime).Ticks));
        args.PushMarkup(Loc.GetString("dreadnought-last-stand-examine",
            ("time", remaining.ToString(@"mm\:ss"))));
    }

    private void OnRefreshSpeed(Entity<DreadnoughtLastStandActiveComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(ent.Comp.SpeedModifier);
    }

    private void OnActiveShutdown(Entity<DreadnoughtLastStandActiveComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.AppliedIgnoreSlowOnDamage)
            RemComp<IgnoreSlowOnDamageComponent>(ent);

        _movement.RefreshMovementSpeedModifiers(ent);
    }
}
