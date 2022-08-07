using Content.Shared.CombatMode;
using Content.Shared.Hands.Components;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Weapons.Melee;

public abstract class SharedNewMeleeWeaponSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly IMapManager MapManager = default!;
    [Dependency] protected readonly SharedCombatModeSystem CombatMode = default!;
    [Dependency] protected readonly SharedPopupSystem PopupSystem = default!;
    [Dependency] protected readonly TagSystem TagSystem = default!;

    protected ISawmill Sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();
        Sawmill = Logger.GetSawmill("melee");

        SubscribeLocalEvent<NewMeleeWeaponComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<NewMeleeWeaponComponent, ComponentHandleState>(OnHandleState);
        SubscribeAllEvent<StartAttackEvent>(OnAttackStart);
        SubscribeAllEvent<ReleaseAttackEvent>(OnReleaseAttack);
        SubscribeAllEvent<StopAttackEvent>(StopAttack);
    }

    protected abstract void Popup(string message, EntityUid? uid, EntityUid? user);

    /// <summary>
    /// Raised when an attack hold is started.
    /// </summary>
    [Serializable, NetSerializable]
    protected sealed class StartAttackEvent : EntityEventArgs
    {
        public EntityUid Weapon;
    }

    /// <summary>
    /// Raised when an attack hold is ended after windup.
    /// </summary>
    [Serializable, NetSerializable]
    protected sealed class ReleaseAttackEvent : EntityEventArgs
    {
        public EntityUid Weapon;
        public EntityCoordinates Coordinates;
    }

    /// <summary>
    /// Raised when an attack is stopped and no attack should occur.
    /// </summary>
    [Serializable, NetSerializable]
    protected sealed class StopAttackEvent : EntityEventArgs
    {
        public EntityUid Weapon;
    }

    [Serializable, NetSerializable]
    protected sealed class NewMeleeWeaponComponentState : ComponentState
    {
        public TimeSpan NextAttack;
    }

    protected virtual void OnAttackStart(StartAttackEvent msg, EntitySessionEventArgs args)
    {
        var user = args.SenderSession.AttachedEntity;

        if (user == null)
            return;

        var weapon = GetWeapon(user.Value);

        if (weapon?.Owner != msg.Weapon)
            return;

        Sawmill.Debug($"Started weapon attack");
    }

    protected virtual void OnReleaseAttack(ReleaseAttackEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity == null ||
            !TryComp<NewMeleeWeaponComponent>(ev.Weapon, out var weapon))
        {
            return;
        }

        var userWeapon = GetWeapon(args.SenderSession.AttachedEntity.Value);

        if (userWeapon != weapon)
            return;

        Sawmill.Debug("Released weapon attack");
        AttemptAttack(args.SenderSession.AttachedEntity.Value, weapon, ev.Coordinates);
    }

    protected virtual void StopAttack(StopAttackEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity == null ||
            !TryComp<NewMeleeWeaponComponent>(ev.Weapon, out var weapon))
        {
            return;
        }

        var userWeapon = GetWeapon(args.SenderSession.AttachedEntity.Value);

        if (userWeapon != weapon)
            return;

        if (weapon.WindupAccumulator <= 0f)
            return;

        Sawmill.Debug("Stopped weapon attack");
        weapon.WindupAccumulator = 0f;
        Dirty(weapon);
    }

    private void OnGetState(EntityUid uid, NewMeleeWeaponComponent component, ref ComponentGetState args)
    {
        args.State = new NewMeleeWeaponComponentState
        {
            NextAttack = component.NextAttack,
        };
    }

    private void OnHandleState(EntityUid uid, NewMeleeWeaponComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not NewMeleeWeaponComponentState state)
            return;

        component.NextAttack = state.NextAttack;
    }

    public NewMeleeWeaponComponent? GetWeapon(EntityUid entity)
    {
        if (!EntityManager.TryGetComponent(entity, out SharedHandsComponent? hands) ||
            hands.ActiveHandEntity is not { } held)
        {
            return null;
        }

        if (!EntityManager.TryGetComponent(held, out NewMeleeWeaponComponent? gun))
            return null;

        if (!CombatMode.IsInCombatMode(entity))
            return null;

        return gun;
    }

    /// <summary>
    /// Called when a windup is finished and an attack is tried.
    /// </summary>
    private void AttemptAttack(EntityUid user, NewMeleeWeaponComponent weapon, EntityCoordinates coordinates)
    {
        if (weapon.WindupAccumulator < weapon.WindupTime)
            return;

        var toCoordinates = coordinates;

        if (TagSystem.HasTag(user, "GunsDisabled"))
        {
            Popup(Loc.GetString("gun-disabled"), user, user);
            return;
        }

        var fromCoordinates = Transform(user).Coordinates;

        // Attack confirmed
        // TODO: Do the swing on client and server
        // TODO: If attack hits on server send the hit thing
        // TODO: Play the animation on shared (client for you + server for others)

        weapon.WindupAccumulator = 0f;
        Dirty(weapon);
    }
}
