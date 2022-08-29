using System.Diagnostics.CodeAnalysis;
using Content.Shared.CombatMode;
using Content.Shared.Hands.Components;
using Content.Shared.Popups;
using Robust.Shared.Collections;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Melee;

public abstract class SharedNewMeleeWeaponSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly IMapManager MapManager = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly SharedCombatModeSystem CombatMode = default!;
    [Dependency] protected readonly SharedPopupSystem PopupSystem = default!;

    protected ISawmill Sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();
        Sawmill = Logger.GetSawmill("melee");

        SubscribeLocalEvent<NewMeleeWeaponComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<NewMeleeWeaponComponent, ComponentHandleState>(OnHandleState);

        SubscribeAllEvent<StartAttackEvent>(OnAttackStart);
        SubscribeAllEvent<ReleaseWideAttackEvent>(OnReleaseWide);
        SubscribeAllEvent<ReleasePreciseAttackEvent>(OnReleasePrecise);
        SubscribeAllEvent<StopAttackEvent>(StopAttack);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Anything that's active is assumed to be winding up so.
        foreach (var comp in EntityQuery<NewMeleeWeaponComponent>())
        {
            if (!comp.Active)
            {
                DebugTools.Assert(comp.WindupAccumulator.Equals(0f));
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
        Sawmill.Debug($"Started weapon attack");
    }

    private bool TryRelease(ReleaseAttackEvent ev, EntitySessionEventArgs args, [NotNullWhen(true)] out NewMeleeWeaponComponent? component)
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

        if (component.WindupAccumulator < component.WindupTime)
            return false;

        return true;
    }

    protected virtual void OnReleasePrecise(ReleasePreciseAttackEvent msg, EntitySessionEventArgs args)
    {
        if (!TryRelease(msg, args, out var component))
            return;

        Sawmill.Debug($"Released precise weapon attack on {ToPrettyString(msg.Target)}");
        AttemptAttack(args.SenderSession.AttachedEntity!.Value, component, msg);
    }

    protected virtual void OnReleaseWide(ReleaseWideAttackEvent msg, EntitySessionEventArgs args)
    {
        if (!TryRelease(msg, args, out var component))
            return;

        Sawmill.Debug("Released wide weapon attack");
        AttemptAttack(args.SenderSession.AttachedEntity!.Value, component, msg);
    }

    protected virtual void StopAttack(StopAttackEvent ev, EntitySessionEventArgs args)
    {
        // TODO: This is janky as fuck. Might need the status effect prediction PR.
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

        if (!userWeapon.Active)
            return;

        Sawmill.Debug("Stopped weapon attack");
        weapon.WindupAccumulator = 0f;
        userWeapon.Active = false;
        Dirty(weapon);
    }

    private void OnGetState(EntityUid uid, NewMeleeWeaponComponent component, ref ComponentGetState args)
    {
        args.State = new NewMeleeWeaponComponentState
        {
            Active = component.Active,
            WindupAccumulator = component.WindupAccumulator,
        };
    }

    private void OnHandleState(EntityUid uid, NewMeleeWeaponComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not NewMeleeWeaponComponentState state)
            return;

        component.Active = state.Active;
        component.WindupAccumulator = state.WindupAccumulator;
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
    private void AttemptAttack(EntityUid user, NewMeleeWeaponComponent weapon, ReleaseAttackEvent attack)
    {
        // Don't check for active because some other caller may be doing this?
        if (weapon.WindupAccumulator < weapon.WindupTime)
            return;

        if (!TryComp<TransformComponent>(user, out var userXform))
            return;

        // Attack confirmed
        Audio.PlayPredicted(weapon.SwingSound, weapon.Owner, user);

        // TODO: Do the swing on client and server
        // TODO: If attack hits on server send the hit thing
        // TODO: Play the animation on shared (client for you + server for others)
        var hit = new ValueList<EntityUid>();

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

        DoLungeAnimation(user, attack.Coordinates.ToMap(EntityManager));

        weapon.Active = false;
        weapon.WindupAccumulator = 0f;
        Dirty(weapon);
    }

    protected virtual void DoPreciseAttack(EntityUid user, ReleasePreciseAttackEvent ev, NewMeleeWeaponComponent component)
    {

    }

    protected virtual void DoWideAttack(EntityUid user, ReleaseWideAttackEvent ev, NewMeleeWeaponComponent component)
    {

    }

    protected void DoLungeAnimation(EntityUid user, MapCoordinates coordinates)
    {
        // TODO: Assert that offset eyes are still okay.
        if (!TryComp<TransformComponent>(user, out var userXform))
            return;

        var invMatrix = userXform.InvWorldMatrix;
        var localPos = invMatrix.Transform(coordinates.Position);

        if (localPos.LengthSquared <= 0f)
            return;

        localPos = userXform.LocalRotation.RotateVec(localPos);
        DoLunge(user, localPos);
    }

    protected abstract void DoLunge(EntityUid user, Vector2 localPos);

    [Serializable, NetSerializable]
    protected sealed class MeleeLungeEvent : EntityEventArgs
    {
        public EntityUid Entity;
        public Vector2 LocalPos;

        public MeleeLungeEvent(Vector2 localPos)
        {
            LocalPos = localPos;
        }
    }
}
