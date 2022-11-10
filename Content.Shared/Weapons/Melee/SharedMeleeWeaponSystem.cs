using Content.Shared.ActionBlocker;
using Content.Shared.CombatMode;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Players;
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
    [Dependency] protected readonly InventorySystem Inventory = default!;
    [Dependency] protected readonly SharedPopupSystem PopupSystem = default!;

    protected ISawmill Sawmill = default!;

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
        SubscribeLocalEvent<MeleeWeaponComponent, HandDeselectedEvent>(OnMeleeDropped);
        SubscribeLocalEvent<MeleeWeaponComponent, HandSelectedEvent>(OnMeleeSelected);

        SubscribeAllEvent<LightAttackEvent>(OnLightAttack);
        SubscribeAllEvent<StartHeavyAttackEvent>(OnStartHeavyAttack);
        SubscribeAllEvent<StopHeavyAttackEvent>(OnStopHeavyAttack);
        SubscribeAllEvent<HeavyAttackEvent>(OnHeavyAttack);
        SubscribeAllEvent<DisarmAttackEvent>(OnDisarmAttack);
        SubscribeAllEvent<StopAttackEvent>(OnStopAttack);
    }

    private void OnMeleeSelected(EntityUid uid, MeleeWeaponComponent component, HandSelectedEvent args)
    {
        if (component.AttackRate.Equals(0f))
            return;

        // If someone swaps to this weapon then reset its cd.
        var curTime = Timing.CurTime;
        var minimum = curTime + TimeSpan.FromSeconds(1 / component.AttackRate);

        if (minimum < component.NextAttack)
            return;

        component.NextAttack = minimum;
        Dirty(component);
    }

    private void OnMeleeDropped(EntityUid uid, MeleeWeaponComponent component, HandDeselectedEvent args)
    {
        if (component.WindUpStart == null)
            return;

        component.WindUpStart = null;
        Dirty(component);
    }

    private void OnStopAttack(StopAttackEvent msg, EntitySessionEventArgs args)
    {
        var user = args.SenderSession.AttachedEntity;

        if (user == null)
            return;

        var weapon = GetWeapon(user.Value);

        if (weapon?.Owner != msg.Weapon)
            return;

        if (!weapon.Attacking)
            return;

        weapon.Attacking = false;
        Dirty(weapon);
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

    private void OnLightAttack(LightAttackEvent msg, EntitySessionEventArgs args)
    {
        var user = args.SenderSession.AttachedEntity;

        if (user == null)
            return;

        var weapon = GetWeapon(user.Value);

        if (weapon?.Owner != msg.Weapon)
            return;

        AttemptAttack(args.SenderSession.AttachedEntity!.Value, weapon, msg, args.SenderSession);
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

        AttemptAttack(args.SenderSession.AttachedEntity.Value, weapon, msg, args.SenderSession);
    }

    private void OnDisarmAttack(DisarmAttackEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity == null)
        {
            return;
        }

        var userWeapon = GetWeapon(args.SenderSession.AttachedEntity.Value);

        if (userWeapon == null)
            return;

        AttemptAttack(args.SenderSession.AttachedEntity.Value, userWeapon, msg, args.SenderSession);
    }

    private void OnGetState(EntityUid uid, MeleeWeaponComponent component, ref ComponentGetState args)
    {
        args.State = new MeleeWeaponComponentState(component.AttackRate, component.Attacking, component.NextAttack,
            component.WindUpStart);
    }

    private void OnHandleState(EntityUid uid, MeleeWeaponComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not MeleeWeaponComponentState state)
            return;

        component.Attacking = state.Attacking;
        component.AttackRate = state.AttackRate;
        component.NextAttack = state.NextAttack;
        component.WindUpStart = state.WindUpStart;
    }

    public MeleeWeaponComponent? GetWeapon(EntityUid entity)
    {
        MeleeWeaponComponent? melee;

        // Use inhands entity if we got one.
        if (EntityManager.TryGetComponent(entity, out SharedHandsComponent? hands) &&
            hands.ActiveHandEntity is { } held)
        {
            if (EntityManager.TryGetComponent(held, out melee))
            {
                return melee;
            }

            return null;
        }

        // Use hands clothing if applicable.
        if (Inventory.TryGetSlotEntity(entity, "gloves", out var gloves) &&
            TryComp<MeleeWeaponComponent>(gloves, out var glovesMelee))
        {
            return glovesMelee;
        }

        // Use our own melee
        if (TryComp(entity, out melee))
        {
            return melee;
        }

        return null;
    }

    public void AttemptLightAttack(EntityUid user, MeleeWeaponComponent weapon, EntityUid target)
    {
        if (!TryComp<TransformComponent>(target, out var targetXform))
            return;

        AttemptAttack(user, weapon, new LightAttackEvent(target, weapon.Owner, targetXform.Coordinates), null);
    }

    public void AttemptDisarmAttack(EntityUid user, MeleeWeaponComponent weapon, EntityUid target)
    {
        if (!TryComp<TransformComponent>(target, out var targetXform))
            return;

        AttemptAttack(user, weapon, new DisarmAttackEvent(target, targetXform.Coordinates), null);
    }

    /// <summary>
    /// Called when a windup is finished and an attack is tried.
    /// </summary>
    private void AttemptAttack(EntityUid user, MeleeWeaponComponent weapon, AttackEvent attack, ICommonSession? session)
    {
        var curTime = Timing.CurTime;

        if (weapon.NextAttack > curTime)
            return;

        if (!CombatMode.IsInCombatMode(user))
            return;

        if (!Blocker.CanAttack(user))
            return;

        // Windup time checked elsewhere.

        if (weapon.NextAttack < curTime)
            weapon.NextAttack = curTime;

        weapon.NextAttack += TimeSpan.FromSeconds(1f / weapon.AttackRate);

        // Attack confirmed
        string animation;

        switch (attack)
        {
            case LightAttackEvent light:
                DoLightAttack(user, light, weapon, session);
                animation = weapon.ClickAnimation;
                break;
            case DisarmAttackEvent disarm:
                if (!DoDisarm(user, disarm, weapon, session))
                    return;

                animation = weapon.ClickAnimation;
                break;
            case HeavyAttackEvent heavy:
                DoHeavyAttack(user, heavy, weapon, session);
                animation = weapon.WideAnimation;
                break;
            default:
                throw new NotImplementedException();
        }

        // Play a sound to give instant feedback; same with playing the animations
        Audio.PlayPredicted(weapon.SwingSound, weapon.Owner, user);

        DoLungeAnimation(user, weapon.Angle, attack.Coordinates.ToMap(EntityManager), weapon.Range, animation);
        weapon.Attacking = true;
        Dirty(weapon);
    }

    /// <summary>
    /// When an attack is released get the actual modifier for damage done.
    /// </summary>
    public float GetModifier(MeleeWeaponComponent component, bool lightAttack)
    {
        if (lightAttack)
            return 1f;

        var windup = component.WindUpStart;
        if (windup == null)
            return 0f;

        var releaseTime = (Timing.CurTime - windup.Value).TotalSeconds;
        var windupTime = component.WindupTime.TotalSeconds;

        // Wraps around back to 0
        releaseTime %= (2 * windupTime);

        var releaseDiff = Math.Abs(releaseTime - windupTime);

        if (releaseDiff < 0)
            releaseDiff = Math.Min(0, releaseDiff + GracePeriod);
        else
            releaseDiff = Math.Max(0, releaseDiff - GracePeriod);

        var fraction = (windupTime - releaseDiff) / windupTime;

        if (fraction < 0.4)
            fraction = 0;

        DebugTools.Assert(fraction <= 1);
        return (float) fraction * component.HeavyDamageModifier.Float();
    }

    protected virtual void DoLightAttack(EntityUid user, LightAttackEvent ev, MeleeWeaponComponent component, ICommonSession? session)
    {

    }

    protected virtual void DoHeavyAttack(EntityUid user, HeavyAttackEvent ev, MeleeWeaponComponent component, ICommonSession? session)
    {

    }

    protected virtual bool DoDisarm(EntityUid user, DisarmAttackEvent ev, MeleeWeaponComponent component, ICommonSession? session)
    {
        if (Deleted(ev.Target) ||
            user == ev.Target)
            return false;

        return true;
    }

    private void DoLungeAnimation(EntityUid user, Angle angle, MapCoordinates coordinates, float length, string? animation)
    {
        // TODO: Assert that offset eyes are still okay.
        if (!TryComp<TransformComponent>(user, out var userXform))
            return;

        var invMatrix = userXform.InvWorldMatrix;
        var localPos = invMatrix.Transform(coordinates.Position);

        if (localPos.LengthSquared <= 0f)
            return;

        localPos = userXform.LocalRotation.RotateVec(localPos);

        // We'll play the effect just short visually so it doesn't look like we should be hitting but actually aren't.
        const float BufferLength = 0.2f;
        var visualLength = length - BufferLength;

        if (localPos.Length > visualLength)
            localPos = localPos.Normalized * visualLength;

        DoLunge(user, angle, localPos, animation);
    }

    public abstract void DoLunge(EntityUid user, Angle angle, Vector2 localPos, string? animation);
}
