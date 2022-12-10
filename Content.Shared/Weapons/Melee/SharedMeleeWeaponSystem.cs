using System.Linq;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Melee;

public abstract class SharedMeleeWeaponSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly IMapManager MapManager = default!;
    [Dependency] private   readonly IPrototypeManager _protoManager = default!;
    [Dependency] protected readonly ISharedAdminLogManager AdminLogger = default!;
    [Dependency] protected readonly ActionBlockerSystem Blocker = default!;
    [Dependency] protected readonly DamageableSystem Damageable = default!;
    [Dependency] protected readonly InventorySystem Inventory = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly SharedCombatModeSystem CombatMode = default!;
    [Dependency] protected readonly SharedInteractionSystem Interaction = default!;
    [Dependency] private   readonly SharedPhysicsSystem _physics = default!;
    [Dependency] protected readonly SharedPopupSystem PopupSystem = default!;
    [Dependency] private   readonly StaminaSystem _stamina = default!;

    protected ISawmill Sawmill = default!;

    public const float DamagePitchVariation = 0.05f;
    private const int AttackMask = (int) (CollisionGroup.MobMask | CollisionGroup.Opaque);

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
            component.WindUpStart, component.ClickAnimation, component.WideAnimation, component.Range);
    }

    private void OnHandleState(EntityUid uid, MeleeWeaponComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not MeleeWeaponComponentState state)
            return;

        component.Attacking = state.Attacking;
        component.AttackRate = state.AttackRate;
        component.NextAttack = state.NextAttack;
        component.WindUpStart = state.WindUpStart;

        component.ClickAnimation = state.ClickAnimation;
        component.WideAnimation = state.WideAnimation;
        component.Range = state.Range;
    }

    public MeleeWeaponComponent? GetWeapon(EntityUid entity)
    {
        MeleeWeaponComponent? melee;

        var ev = new GetMeleeWeaponEvent();
        RaiseLocalEvent(entity, ev);
        if (ev.Handled)
        {
            return EntityManager.GetComponentOrNull<MeleeWeaponComponent>(ev.Weapon);
        }

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

    protected abstract bool InRange(EntityUid user, EntityUid target, float range, ICommonSession? session);

    protected virtual void DoLightAttack(EntityUid user, LightAttackEvent ev, MeleeWeaponComponent component, ICommonSession? session)
    {
        // Can't attack yourself
        // Not in LOS.
        if (user == ev.Target ||
            ev.Target == null ||
            Deleted(ev.Target) ||
            // For consistency with wide attacks stuff needs damageable.
            !HasComp<DamageableComponent>(ev.Target) ||
            !TryComp<TransformComponent>(ev.Target, out var targetXform))
        {
            Audio.PlayPredicted(component.SwingSound, component.Owner, user);
            return;
        }

        if (!InRange(user, ev.Target.Value, component.Range, session))
        {
            Audio.PlayPredicted(component.SwingSound, component.Owner, user);
            return;
        }

        var damage = component.Damage * GetModifier(component, true);

        // Sawmill.Debug($"Melee damage is {damage.Total} out of {component.Damage.Total}");

        // Raise event before doing damage so we can cancel damage if the event is handled
        var hitEvent = new MeleeHitEvent(new List<EntityUid> { ev.Target.Value }, user, damage);
        RaiseLocalEvent(component.Owner, hitEvent);

        if (hitEvent.Handled)
            return;

        var targets = new List<EntityUid>(1)
        {
            ev.Target.Value
        };

        Interaction.DoContactInteraction(ev.Weapon, ev.Target);
        Interaction.DoContactInteraction(user, ev.Weapon);

        // If the user is using a long-range weapon, this probably shouldn't be happening? But I'll interpret melee as a
        // somewhat messy scuffle. See also, heavy attacks.
        Interaction.DoContactInteraction(user, ev.Target);

        // For stuff that cares about it being attacked.
        RaiseLocalEvent(ev.Target.Value, new AttackedEvent(component.Owner, user, targetXform.Coordinates));

        var modifiedDamage = DamageSpecifier.ApplyModifierSets(damage + hitEvent.BonusDamage, hitEvent.ModifiersList);
        var damageResult = Damageable.TryChangeDamage(ev.Target, modifiedDamage, origin:user);

        if (damageResult != null && damageResult.Total > FixedPoint2.Zero)
        {
            // If the target has stamina and is taking blunt damage, they should also take stamina damage based on their blunt to stamina factor
            if (damageResult.DamageDict.TryGetValue("Blunt", out var bluntDamage))
            {
                _stamina.TakeStaminaDamage(ev.Target.Value, (bluntDamage * component.BluntStaminaDamageFactor).Float());
            }

            if (component.Owner == user)
            {
                AdminLogger.Add(LogType.MeleeHit,
                    $"{ToPrettyString(user):user} melee attacked {ToPrettyString(ev.Target.Value):target} using their hands and dealt {damageResult.Total:damage} damage");
            }
            else
            {
                AdminLogger.Add(LogType.MeleeHit,
                    $"{ToPrettyString(user):user} melee attacked {ToPrettyString(ev.Target.Value):target} using {ToPrettyString(component.Owner):used} and dealt {damageResult.Total:damage} damage");
            }

            PlayHitSound(ev.Target.Value, user, GetHighestDamageSound(modifiedDamage, _protoManager), hitEvent.HitSoundOverride, component.HitSound);
        }
        else
        {
            if (hitEvent.HitSoundOverride != null)
            {
                Audio.PlayPredicted(hitEvent.HitSoundOverride, component.Owner, user);
            }
            else
            {
                Audio.PlayPredicted(component.NoDamageSound, component.Owner, user);
            }
        }

        if (damageResult?.Total > FixedPoint2.Zero)
        {
            DoDamageEffect(targets, user, targetXform);
        }
    }

    protected abstract void DoDamageEffect(List<EntityUid> targets, EntityUid? user,  TransformComponent targetXform);

    protected virtual void DoHeavyAttack(EntityUid user, HeavyAttackEvent ev, MeleeWeaponComponent component, ICommonSession? session)
    {
        // TODO: This is copy-paste as fuck with DoPreciseAttack
        if (!TryComp<TransformComponent>(user, out var userXform))
        {
            return;
        }

        var targetMap = ev.Coordinates.ToMap(EntityManager);

        if (targetMap.MapId != userXform.MapID)
        {
            return;
        }

        var userPos = userXform.WorldPosition;
        var direction = targetMap.Position - userPos;
        var distance = Math.Min(component.Range, direction.Length);

        // This should really be improved. GetEntitiesInArc uses pos instead of bounding boxes.
        var entities = ArcRayCast(userPos, direction.ToWorldAngle(), component.Angle, distance, userXform.MapID, user);

        if (entities.Count == 0)
        {
            Audio.PlayPredicted(component.SwingSound, component.Owner, user);
            return;
        }

        var targets = new List<EntityUid>();
        var damageQuery = GetEntityQuery<DamageableComponent>();

        foreach (var entity in entities)
        {
            if (entity == user ||
                !damageQuery.HasComponent(entity))
                continue;

            targets.Add(entity);
        }

        var damage = component.Damage * GetModifier(component, false);
        // Sawmill.Debug($"Melee damage is {damage.Total} out of {component.Damage.Total}");

        // Raise event before doing damage so we can cancel damage if the event is handled
        var hitEvent = new MeleeHitEvent(targets, user, damage);
        RaiseLocalEvent(component.Owner, hitEvent);

        if (hitEvent.Handled)
            return;

        Interaction.DoContactInteraction(user, ev.Weapon);

        // For stuff that cares about it being attacked.
        foreach (var target in targets)
        {
            Interaction.DoContactInteraction(ev.Weapon, target);

            // If the user is using a long-range weapon, this probably shouldn't be happening? But I'll interpret melee as a
            // somewhat messy scuffle. See also, light attacks.
            Interaction.DoContactInteraction(user, target);

            RaiseLocalEvent(target, new AttackedEvent(component.Owner, user, Transform(target).Coordinates));
        }

        var modifiedDamage = DamageSpecifier.ApplyModifierSets(damage + hitEvent.BonusDamage, hitEvent.ModifiersList);
        var appliedDamage = new DamageSpecifier();

        foreach (var entity in targets)
        {
            RaiseLocalEvent(entity, new AttackedEvent(component.Owner, user, ev.Coordinates));

            var damageResult = Damageable.TryChangeDamage(entity, modifiedDamage, origin:user);

            if (damageResult != null && damageResult.Total > FixedPoint2.Zero)
            {
                appliedDamage += damageResult;

                if (component.Owner == user)
                {
                    AdminLogger.Add(LogType.MeleeHit,
                        $"{ToPrettyString(user):user} melee attacked {ToPrettyString(entity):target} using their hands and dealt {damageResult.Total:damage} damage");
                }
                else
                {
                    AdminLogger.Add(LogType.MeleeHit,
                        $"{ToPrettyString(user):user} melee attacked {ToPrettyString(entity):target} using {ToPrettyString(component.Owner):used} and dealt {damageResult.Total:damage} damage");
                }
            }
        }

        if (entities.Count != 0)
        {
            if (appliedDamage.Total > FixedPoint2.Zero)
            {
                var target = entities.First();
                PlayHitSound(target, user, GetHighestDamageSound(modifiedDamage, _protoManager), hitEvent.HitSoundOverride, component.HitSound);
            }
            else
            {
                if (hitEvent.HitSoundOverride != null)
                {
                    Audio.PlayPredicted(hitEvent.HitSoundOverride, component.Owner, user);
                }
                else
                {
                    Audio.PlayPredicted(component.NoDamageSound, component.Owner, user);
                }
            }
        }

        if (appliedDamage.Total > FixedPoint2.Zero)
        {
            DoDamageEffect(targets, user, Transform(targets[0]));
        }
    }

    private HashSet<EntityUid> ArcRayCast(Vector2 position, Angle angle, Angle arcWidth, float range, MapId mapId, EntityUid ignore)
    {
        // TODO: This is pretty sucky.
        var widthRad = arcWidth;
        var increments = 1 + 35 * (int) Math.Ceiling(widthRad / (2 * Math.PI));
        var increment = widthRad / increments;
        var baseAngle = angle - widthRad / 2;

        var resSet = new HashSet<EntityUid>();

        for (var i = 0; i < increments; i++)
        {
            var castAngle = new Angle(baseAngle + increment * i);
            var res = _physics.IntersectRay(mapId,
                new CollisionRay(position, castAngle.ToWorldVec(),
                    AttackMask), range, ignore, false).ToList();

            if (res.Count != 0)
            {
                resSet.Add(res[0].HitEntity);
            }
        }

        return resSet;
    }

    private void PlayHitSound(EntityUid target, EntityUid? user, string? type, SoundSpecifier? hitSoundOverride, SoundSpecifier? hitSound)
    {
        var playedSound = false;

        // Play sound based off of highest damage type.
        if (TryComp<MeleeSoundComponent>(target, out var damageSoundComp))
        {
            if (type == null && damageSoundComp.NoDamageSound != null)
            {
                Audio.PlayPredicted(damageSoundComp.NoDamageSound, target, user, AudioParams.Default.WithVariation(DamagePitchVariation));
                playedSound = true;
            }
            else if (type != null && damageSoundComp.SoundTypes?.TryGetValue(type, out var damageSoundType) == true)
            {
                Audio.PlayPredicted(damageSoundType, target, user, AudioParams.Default.WithVariation(DamagePitchVariation));
                playedSound = true;
            }
            else if (type != null && damageSoundComp.SoundGroups?.TryGetValue(type, out var damageSoundGroup) == true)
            {
                Audio.PlayPredicted(damageSoundGroup, target, user, AudioParams.Default.WithVariation(DamagePitchVariation));
                playedSound = true;
            }
        }

        // Use weapon sounds if the thing being hit doesn't specify its own sounds.
        if (!playedSound)
        {
            if (hitSoundOverride != null)
            {
                Audio.PlayPredicted(hitSoundOverride, target, user, AudioParams.Default.WithVariation(DamagePitchVariation));
                playedSound = true;
            }
            else if (hitSound != null)
            {
                Audio.PlayPredicted(hitSound, target, user, AudioParams.Default.WithVariation(DamagePitchVariation));
                playedSound = true;
            }
        }

        // Fallback to generic sounds.
        if (!playedSound)
        {
            switch (type)
            {
                // Unfortunately heat returns caustic group so can't just use the damagegroup in that instance.
                case "Burn":
                case "Heat":
                case "Cold":
                    Audio.PlayPredicted(new SoundPathSpecifier("/Audio/Items/welder.ogg"), target, user, AudioParams.Default.WithVariation(DamagePitchVariation));
                    break;
                // No damage, fallback to tappies
                case null:
                    Audio.PlayPredicted(new SoundPathSpecifier("/Audio/Weapons/tap.ogg"), target, user, AudioParams.Default.WithVariation(DamagePitchVariation));
                    break;
                case "Brute":
                    Audio.PlayPredicted(new SoundPathSpecifier("/Audio/Weapons/smash.ogg"), target, user, AudioParams.Default.WithVariation(DamagePitchVariation));
                    break;
            }
        }
    }

    public static string? GetHighestDamageSound(DamageSpecifier modifiedDamage, IPrototypeManager protoManager)
    {
        var groups = modifiedDamage.GetDamagePerGroup(protoManager);

        // Use group if it's exclusive, otherwise fall back to type.
        if (groups.Count == 1)
        {
            return groups.Keys.First();
        }

        var highestDamage = FixedPoint2.Zero;
        string? highestDamageType = null;

        foreach (var (type, damage) in modifiedDamage.DamageDict)
        {
            if (damage <= highestDamage)
                continue;

            highestDamageType = type;
        }

        return highestDamageType;
    }

    protected virtual bool DoDisarm(EntityUid user, DisarmAttackEvent ev, MeleeWeaponComponent component, ICommonSession? session)
    {
        if (Deleted(ev.Target) ||
            user == ev.Target)
            return false;

        // Play a sound to give instant feedback; same with playing the animations
        Audio.PlayPredicted(component.SwingSound, component.Owner, user);
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
