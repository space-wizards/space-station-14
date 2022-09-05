using System.Diagnostics.CodeAnalysis;
using Content.Shared.ActionBlocker;
using Content.Shared.CombatMode;
using Content.Shared.Hands.Components;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
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

        SubscribeAllEvent<LightAttackEvent>(OnLightAttack);
        SubscribeAllEvent<StartHeavyAttackEvent>(OnStartHeavyAttack);
        SubscribeAllEvent<StopHeavyAttackEvent>(OnStopHeavyAttack);
        SubscribeAllEvent<HeavyAttackEvent>(OnHeavyAttack);
    }

    private void OnStartHeavyAttack(StartHeavyAttackEvent msg, EntitySessionEventArgs args)
    {
        var user = args.SenderSession.AttachedEntity;

        if (user == null)
            return;

        var weapon = GetWeapon(user.Value);

        if (weapon?.Owner != msg.Weapon)
            return;

        DebugTools.Assert(weapon.WindUpStart == null);
        weapon.WindUpStart = Timing.CurTime;
        Dirty(weapon);
    }

    protected abstract void Popup(string message, EntityUid? uid, EntityUid? user);

    [Serializable, NetSerializable]
    protected sealed class MeleeWeaponComponentState : ComponentState
    {
        public bool Active;
        public bool Accumulating;
        public float WindupAccumulator;
    }

    private void OnLightAttack(LightAttackEvent msg, EntitySessionEventArgs args)
    {
        var user = args.SenderSession.AttachedEntity;

        if (user == null)
            return;

        var weapon = GetWeapon(user.Value);

        if (weapon?.Owner != msg.Weapon)
            return;

        if (weapon.NextAttack < Timing.CurTime)
            return;

        AttemptAttack(args.SenderSession.AttachedEntity!.Value, weapon, msg);
    }

    private void OnStopHeavyAttack(StopHeavyAttackEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity == null ||
            !TryComp<MeleeWeaponComponent>(msg.Weapon, out var weapon))
        {
            return;
        }

        var userWeapon = GetWeapon(args.SenderSession.AttachedEntity.Value);

        if (userWeapon != weapon)
            return;

        if (weapon.WindUpStart.Equals(null))
        {
            return;
        }

        weapon.WindUpStart = null;
        Dirty(weapon);
    }

    private void OnHeavyAttack(HeavyAttackEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity == null ||
            !TryComp<MeleeWeaponComponent>(msg.Weapon, out var weapon))
        {
            return;
        }

        var userWeapon = GetWeapon(args.SenderSession.AttachedEntity.Value);

        if (userWeapon != weapon)
            return;

        AttemptAttack(args.SenderSession.AttachedEntity.Value, weapon, msg);
    }

    private void OnGetState(EntityUid uid, MeleeWeaponComponent component, ref ComponentGetState args)
    {
        args.State = new MeleeWeaponComponentState
        {
            Active = component.Active,
            Accumulating = component.Accumulating,
            WindupAccumulator = component.NextAttack,
        };
    }

    private void OnHandleState(EntityUid uid, MeleeWeaponComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not MeleeWeaponComponentState state)
            return;

        component.Active = state.Active;
        component.Accumulating = state.Accumulating;
        component.NextAttack = state.WindupAccumulator;
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
    private void AttemptAttack(EntityUid user, MeleeWeaponComponent weapon, AttackEvent attack)
    {
        if (!Blocker.CanAttack(user))
            return;

        // Windup time checked elsewhere.

        if (!CombatMode.IsInCombatMode(user))
            return;

        var curTime = Timing.CurTime;

        if (weapon.NextAttack < curTime)
            weapon.NextAttack = curTime;

        // TODO: Disarms

        var attacks = 0;
        var fireRate = TimeSpan.FromSeconds(1f / weapon.AttackRate);

        while (weapon.NextAttack <= curTime)
        {
            weapon.NextAttack += fireRate;
            attacks++;
        }

        DebugTools.Assert(attacks > 0);

        // Attack confirmed
        // Play a sound to give instant feedback; same with playing the animations
        Audio.PlayPredicted(weapon.SwingSound, weapon.Owner, user);

        switch (attack)
        {
            case LightAttackEvent precise:
                DoPreciseAttack(user, precise, weapon);
                break;
            case HeavyAttackEvent wide:
                DoWideAttack(user, wide, weapon);
                break;
            default:
                throw new NotImplementedException();
        }

        DoLungeAnimation(user, attack.Coordinates.ToMap(EntityManager), weapon.Animation);
        Dirty(weapon);
    }

    /// <summary>
    /// When an attack is released get the actual modifier for damage done.
    /// </summary>
    public static float GetModifier(MeleeWeaponComponent component)
    {
        var total = component.WindupTime - AttackBuffer;
        var amount = component.NextAttack + GracePeriod - AttackBuffer;
        var fraction = Math.Clamp(amount / total, 0f, 1f);
        return GetModifier(fraction);
    }

    public static float GetModifier(float fraction)
    {
        return fraction;
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
