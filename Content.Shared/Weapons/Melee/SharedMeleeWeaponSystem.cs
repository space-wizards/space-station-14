using System.Diagnostics.CodeAnalysis;
using Content.Shared.ActionBlocker;
using Content.Shared.CombatMode;
using Content.Shared.Hands.Components;
using Content.Shared.Popups;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Melee;

public abstract class SharedMeleeWeaponSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly IMapManager MapManager = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly SharedCombatModeSystem CombatMode = default!;
    [Dependency] protected readonly SharedPopupSystem PopupSystem = default!;

    protected ISawmill Sawmill = default!;

    /// <summary>
    /// Amount of time a click has to be held before it's considered a heavy attack instead of a light attack.
    /// </summary>
    public const float HeavyBuffer = 0.25f;

    public override void Initialize()
    {
        base.Initialize();
        Sawmill = Logger.GetSawmill("melee");

        SubscribeLocalEvent<MeleeWeaponComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<MeleeWeaponComponent, ComponentHandleState>(OnHandleState);

        SubscribeAllEvent<StartAttackEvent>(OnAttackStart);
        SubscribeAllEvent<ReleaseWideAttackEvent>(OnReleaseWide);
        SubscribeAllEvent<ReleasePreciseAttackEvent>(OnReleasePrecise);
        SubscribeAllEvent<StopAttackEvent>(StopAttack);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Anything that's active is assumed to be winding up so.
        foreach (var comp in EntityQuery<MeleeWeaponComponent>())
        {
            comp.CooldownAccumulator = MathF.Max(0f, comp.CooldownAccumulator - frameTime);

            // If we're not holding a charged attack might be releasing.
            if (!comp.Active)
            {
                comp.WindupAccumulator = MathF.Max(0f, comp.WindupAccumulator - frameTime);
                Dirty(comp);
                continue;
            }

            if (comp.WindupAccumulator.Equals(comp.WindupTime))
                continue;

            comp.WindupAccumulator = MathF.Min(comp.WindupTime, comp.WindupAccumulator + frameTime);
            Dirty(comp);
        }
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

    // TODO: Did I always have to mark abstracts as serializable???
    /// <summary>
    /// Raised when an attack hold is ended after windup.
    /// </summary>
    [Serializable, NetSerializable]
    protected abstract class ReleaseAttackEvent : EntityEventArgs
    {
        public EntityUid Weapon;
        public EntityCoordinates Coordinates;
    }

    [Serializable, NetSerializable]
    protected sealed class ReleaseWideAttackEvent : ReleaseAttackEvent
    {
    }

    [Serializable, NetSerializable]
    protected sealed class ReleasePreciseAttackEvent : ReleaseAttackEvent
    {
        public EntityUid Target;
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
        public bool Active;
        public float WindupAccumulator;
        public float CooldownAccumulator;
    }

    protected virtual void OnAttackStart(StartAttackEvent msg, EntitySessionEventArgs args)
    {
        var user = args.SenderSession.AttachedEntity;

        if (user == null)
            return;

        var weapon = GetWeapon(user.Value);

        if (weapon?.Owner != msg.Weapon)
            return;

        if (weapon.Active)
            return;

        weapon.Active = true;
        Dirty(weapon);
        // Sawmill.Debug($"Started weapon attack");
    }

    private bool TryRelease(ReleaseAttackEvent ev, EntitySessionEventArgs args, [NotNullWhen(true)] out MeleeWeaponComponent? component, out bool heavy)
    {
        heavy = false;
        component = null;

        if (args.SenderSession.AttachedEntity == null ||
            !TryComp(ev.Weapon, out component))
        {
            return false;
        }

        var userWeapon = GetWeapon(args.SenderSession.AttachedEntity.Value);

        if (userWeapon != component)
            return false;

        if (!component.Active)
            return false;

        if (component.WindupAccumulator < HeavyBuffer)
            return true;

        if (component.WindupAccumulator < component.WindupTime)
            return false;

        heavy = true;
        return true;
    }

    protected virtual void OnReleasePrecise(ReleasePreciseAttackEvent msg, EntitySessionEventArgs args)
    {
        if (!TryRelease(msg, args, out var component, out var heavy))
            return;

        // Sawmill.Debug($"Released precise weapon attack on {ToPrettyString(msg.Target)}");
        AttemptAttack(args.SenderSession.AttachedEntity!.Value, component, msg, heavy);
    }

    protected virtual void OnReleaseWide(ReleaseWideAttackEvent msg, EntitySessionEventArgs args)
    {
        if (!TryRelease(msg, args, out var component, out var heavy))
            return;

        // Sawmill.Debug("Released wide weapon attack");
        AttemptAttack(args.SenderSession.AttachedEntity!.Value, component, msg, heavy);
    }

    protected virtual void StopAttack(StopAttackEvent ev, EntitySessionEventArgs args)
    {
        // TODO: This is janky as fuck. Might need the status effect prediction PR.
        if (args.SenderSession.AttachedEntity == null ||
            !TryComp<MeleeWeaponComponent>(ev.Weapon, out var weapon))
        {
            return;
        }

        var userWeapon = GetWeapon(args.SenderSession.AttachedEntity.Value);

        if (userWeapon != weapon)
            return;

        if (weapon.WindupAccumulator <= 0f)
            return;

        if (!userWeapon.Active)
            return;

        // Sawmill.Debug("Stopped weapon attack");
        userWeapon.Active = false;
        Dirty(weapon);
    }

    private void OnGetState(EntityUid uid, MeleeWeaponComponent component, ref ComponentGetState args)
    {
        args.State = new NewMeleeWeaponComponentState
        {
            Active = component.Active,
            WindupAccumulator = component.WindupAccumulator,
            CooldownAccumulator = component.CooldownAccumulator,
        };
    }

    private void OnHandleState(EntityUid uid, MeleeWeaponComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not NewMeleeWeaponComponentState state)
            return;

        component.Active = state.Active;
        component.WindupAccumulator = state.WindupAccumulator;
        component.CooldownAccumulator = state.CooldownAccumulator;
    }

    public MeleeWeaponComponent? GetWeapon(EntityUid entity)
    {
        if (!CombatMode.IsInCombatMode(entity))
            return null;

        // Use inhands entity if we got one.
        if (EntityManager.TryGetComponent(entity, out SharedHandsComponent? hands) &&
            hands.ActiveHandEntity is { } held &&
            EntityManager.TryGetComponent(held, out MeleeWeaponComponent? melee))
        {
            return melee;
        }

        if (TryComp(entity, out melee))
        {
            return melee;
        }

        return null;
    }

    /// <summary>
    /// Called when a windup is finished and an attack is tried.
    /// </summary>
    private void AttemptAttack(EntityUid user, MeleeWeaponComponent weapon, ReleaseAttackEvent attack, bool heavy)
    {
        if (!_blocker.CanAttack(user))
            return;

        // Windup time checked elsewhere.

        if (!TryComp<SharedCombatModeComponent>(user, out var combatMode))
            return;

        // Try to do a disarm
        if (combatMode.Disarm == true)
        {
            if (TryComp<SharedHandsComponent>(user, out var hands)
                && hands.ActiveHand is { IsEmpty: false })
            {
                if (Timing.IsFirstTimePredicted)
                    PopupSystem.PopupEntity(Loc.GetString("disarm-action-free-hand"), user, Filter.Local());

                return;
            }


            return;
        }

        // Attack confirmed
        // Play a sound to give instant feedback; same with playing the animations
        Audio.PlayPredicted(weapon.SwingSound, weapon.Owner, user);

        switch (attack)
        {
            case ReleasePreciseAttackEvent precise:
                DoPreciseAttack(user, precise, weapon, heavy);
                break;
            case ReleaseWideAttackEvent wide:
                DoWideAttack(user, wide, weapon, heavy);
                break;
            default:
                throw new NotImplementedException();
        }

        DoLungeAnimation(user, attack.Coordinates.ToMap(EntityManager), weapon.Animation);

        weapon.Active = false;

        weapon.CooldownAccumulator = weapon.CooldownTime;
        weapon.WindupAccumulator = 0f;
        Dirty(weapon);
    }

    protected virtual void DoPreciseAttack(EntityUid user, ReleasePreciseAttackEvent ev, MeleeWeaponComponent component, bool heavy)
    {

    }

    protected virtual void DoWideAttack(EntityUid user, ReleaseWideAttackEvent ev, MeleeWeaponComponent component, bool heavy)
    {

    }

    private void DoLungeAnimation(EntityUid user, MapCoordinates coordinates, string? animation)
    {
        // TODO: Assert that offset eyes are still okay.
        if (!TryComp<TransformComponent>(user, out var userXform))
            return;

        var invMatrix = userXform.InvWorldMatrix;
        var localPos = invMatrix.Transform(coordinates.Position);

        if (localPos.LengthSquared <= 0f)
            return;

        localPos = userXform.LocalRotation.RotateVec(localPos);
        DoLunge(user, localPos, animation);
    }

    public abstract void DoLunge(EntityUid user, Vector2 localPos, string? animation);
}
