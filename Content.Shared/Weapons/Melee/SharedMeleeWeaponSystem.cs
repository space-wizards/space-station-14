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
    [Dependency] protected readonly ActionBlockerSystem Blocker = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly SharedCombatModeSystem CombatMode = default!;
    [Dependency] protected readonly SharedPopupSystem PopupSystem = default!;

    protected ISawmill Sawmill = default!;

    /// <summary>
    /// Amount of time a click has to be held before it's considered an attack.
    /// </summary>
    public const float AttackBuffer = 0.25f;

    /// <summary>
    /// If an attack is released within this buffer it's assumed to be full damage.
    /// </summary>
    public const float GracePeriod = 0.05f;

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
            if (!comp.Active)
            {
                continue;
            }

            // TODO: Server needs to cancel CanAttacks + IsInCombatMode

            if (comp.Accumulating)
            {
                comp.WindupAccumulator += frameTime;

                if (comp.WindupAccumulator > comp.WindupTime)
                {
                    comp.Accumulating = false;
                    var diff = comp.WindupAccumulator - comp.WindupTime;
                    comp.WindupAccumulator -= diff;
                }
            }
            else
            {
                comp.WindupAccumulator -= frameTime;

                if (comp.WindupAccumulator <= 0f)
                {
                    comp.Active = false;
                    comp.Accumulating = true;
                    comp.WindupAccumulator = 0f;
                }
            }

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
    protected sealed class MeleeWeaponComponentState : ComponentState
    {
        public bool Active;
        public bool Accumulating;
        public float WindupAccumulator;
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
        weapon.Accumulating = true;
        Dirty(weapon);
        // Sawmill.Debug($"Started weapon attack");
    }

    private bool TryRelease(ReleaseAttackEvent ev, EntitySessionEventArgs args, [NotNullWhen(true)] out MeleeWeaponComponent? component)
    {
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

        if (component.WindupAccumulator < AttackBuffer)
            return false;

        Sawmill.Debug($"Released at {component.WindupAccumulator}");

        return true;
    }

    protected virtual void OnReleasePrecise(ReleasePreciseAttackEvent msg, EntitySessionEventArgs args)
    {
        if (!TryRelease(msg, args, out var component))
            return;

        // Sawmill.Debug($"Released precise weapon attack on {ToPrettyString(msg.Target)}");
        AttemptAttack(args.SenderSession.AttachedEntity!.Value, component, msg);
    }

    protected virtual void OnReleaseWide(ReleaseWideAttackEvent msg, EntitySessionEventArgs args)
    {
        if (!TryRelease(msg, args, out var component))
            return;

        // Sawmill.Debug("Released wide weapon attack");
        AttemptAttack(args.SenderSession.AttachedEntity!.Value, component, msg);
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
        userWeapon.WindupAccumulator = 0f;
        Dirty(weapon);
    }

    private void OnGetState(EntityUid uid, MeleeWeaponComponent component, ref ComponentGetState args)
    {
        args.State = new MeleeWeaponComponentState
        {
            Active = component.Active,
            Accumulating = component.Accumulating,
            WindupAccumulator = component.WindupAccumulator,
        };
    }

    private void OnHandleState(EntityUid uid, MeleeWeaponComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not MeleeWeaponComponentState state)
            return;

        component.Active = state.Active;
        component.Accumulating = state.Accumulating;
        component.WindupAccumulator = state.WindupAccumulator;
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
    private void AttemptAttack(EntityUid user, MeleeWeaponComponent weapon, ReleaseAttackEvent attack)
    {
        if (!Blocker.CanAttack(user))
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
                DoPreciseAttack(user, precise, weapon);
                break;
            case ReleaseWideAttackEvent wide:
                DoWideAttack(user, wide, weapon);
                break;
            default:
                throw new NotImplementedException();
        }

        DoLungeAnimation(user, attack.Coordinates.ToMap(EntityManager), weapon.Animation);

        weapon.Active = false;
        weapon.WindupAccumulator = 0f;
        Dirty(weapon);
    }

    /// <summary>
    /// When an attack is released get the actual modifier for damage done.
    /// </summary>
    protected float GetModifier(MeleeWeaponComponent component)
    {
        var total = component.WindupTime - AttackBuffer;
        var amount = component.WindupAccumulator + GracePeriod - AttackBuffer;
        var fraction = Math.Clamp(amount / total, 0f, 1f);
        var modifier = MathF.Pow(fraction, 2f);
        Sawmill.Debug($"Got melee modifier of {modifier}");
        return modifier;
    }

    protected virtual void DoPreciseAttack(EntityUid user, ReleasePreciseAttackEvent ev, MeleeWeaponComponent component)
    {

    }

    protected virtual void DoWideAttack(EntityUid user, ReleaseWideAttackEvent ev, MeleeWeaponComponent component)
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
