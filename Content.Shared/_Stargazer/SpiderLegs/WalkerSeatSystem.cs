// Content.Shared/_Stargazer/SpiderLegs/WalkerSeatSystem.cs
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Vehicle.Components;
using Content.Shared.Vehicle.Systems;

namespace Content.Shared._Stargazer.SpiderLegs;

/// <summary>
/// Логика <see cref="WalkerSeatComponent"/>. Штатные системы (пристёгивание, транспорт) не меняются:
/// задержки вставляются через StrapAttemptEvent / UnstrapAttemptEvent, а водитель назначается
/// через публичный API VehicleSystem — так же, как это делает штатный StrapVehicle.
/// </summary>
public sealed partial class WalkerSeatSystem : EntitySystem
{
    [Dependency] private VehicleSystem _vehicle = default!;
    [Dependency] private SharedBuckleSystem _buckle = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WalkerSeatComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<WalkerSeatComponent, UnstrappedEvent>(OnUnstrapped);
        SubscribeLocalEvent<WalkerSeatComponent, StrapAttemptEvent>(OnStrapAttempt);
        SubscribeLocalEvent<WalkerSeatComponent, UnstrapAttemptEvent>(OnUnstrapAttempt);
        SubscribeLocalEvent<WalkerSeatComponent, WalkerSeatMountDoAfterEvent>(OnMountDoAfter);
        SubscribeLocalEvent<WalkerSeatComponent, WalkerSeatDismountDoAfterEvent>(OnDismountDoAfter);

        SubscribeLocalEvent<WalkerRiderComponent, DamageModifyEvent>(OnRiderDamageModify);
    }

    // ---------- Водитель (копия StrapVehicle) ----------

    private void OnStrapped(Entity<WalkerSeatComponent> ent, ref StrappedEvent args)
    {
        var rider = args.Buckle.Owner;

        if (TryComp<VehicleComponent>(ent, out var vehicle))
            _vehicle.TrySetOperator((ent.Owner, vehicle), rider);

        EnsureComp<WalkerRiderComponent>(rider).Seat = ent.Owner;
    }

    private void OnUnstrapped(Entity<WalkerSeatComponent> ent, ref UnstrappedEvent args)
    {
        var rider = args.Buckle.Owner;

        if (TryComp<VehicleComponent>(ent, out var vehicle) && vehicle.Operator == rider)
            _vehicle.TryRemoveOperator((ent.Owner, vehicle));

        if (TryComp<WalkerRiderComponent>(rider, out var riderComp) && riderComp.Seat == ent.Owner)
            RemComp<WalkerRiderComponent>(rider);
    }

    // ---------- Посадка с задержкой ----------

    private void OnStrapAttempt(Entity<WalkerSeatComponent> ent, ref StrapAttemptEvent args)
    {
        // Системные пристёгивания (без пользователя) не трогаем.
        if (args.Cancelled || args.User is not { } user || ent.Comp.MountDelay <= 0f)
            return;

        // Это наш же вызов после завершённого do-after.
        if (ent.Comp.PendingMount == args.Buckle.Owner)
            return;

        args.Cancelled = true;

        var doAfter = new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(ent.Comp.MountDelay),
            new WalkerSeatMountDoAfterEvent(), ent.Owner, target: args.Buckle.Owner, used: ent.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = false,
        };

        if (_doAfter.TryStartDoAfter(doAfter) && args.Popup)
            _popup.PopupClient(Loc.GetString("walker-seat-mount-start", ("seat", ent.Owner)), user, user);
    }

    private void OnMountDoAfter(EntityUid uid, WalkerSeatComponent comp, WalkerSeatMountDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } rider)
            return;

        comp.PendingMount = rider;
        args.Handled = _buckle.TryBuckle(rider, args.User, uid);
        comp.PendingMount = null;
    }

    // ---------- Долгое стаскивание водителя ----------

    private void OnUnstrapAttempt(Entity<WalkerSeatComponent> ent, ref UnstrapAttemptEvent args)
    {
        // Без пользователя (станы, крит, системные вызовы), сам водитель или само кресло — без задержки.
        if (args.Cancelled
            || args.User is not { } user
            || user == args.Buckle.Owner
            || user == ent.Owner
            || ent.Comp.DismountDelay <= 0f)
        {
            return;
        }

        if (ent.Comp.PendingDismount == args.Buckle.Owner)
            return;

        args.Cancelled = true;

        var rider = args.Buckle.Owner;
        var doAfter = new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(ent.Comp.DismountDelay),
            new WalkerSeatDismountDoAfterEvent(), ent.Owner, target: rider, used: ent.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfter) || !args.Popup)
            return;

        _popup.PopupClient(Loc.GetString("walker-seat-dismount-start-user",
            ("rider", Identity.Entity(rider, EntityManager)), ("seat", ent.Owner)), user, user);

        _popup.PopupEntity(Loc.GetString("walker-seat-dismount-start-rider",
            ("user", Identity.Entity(user, EntityManager)), ("seat", ent.Owner)), rider, rider, PopupType.MediumCaution);
    }

    private void OnDismountDoAfter(EntityUid uid, WalkerSeatComponent comp, WalkerSeatDismountDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } rider)
            return;

        comp.PendingDismount = rider;
        args.Handled = _buckle.TryUnbuckle(rider, args.User);
        comp.PendingDismount = null;
    }

    // ---------- Броня водителя ----------

    private void OnRiderDamageModify(EntityUid uid, WalkerRiderComponent comp, DamageModifyEvent args)
    {
        if (!TryComp<WalkerSeatComponent>(comp.Seat, out var seat) || seat.RiderArmor == null)
            return;

        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, seat.RiderArmor);
    }
}
