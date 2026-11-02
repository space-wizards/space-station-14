using System.Numerics;
using Content.Shared.Actions;
using Content.Shared.Actions.Events;
using Content.Shared.CF.Actions;
using Content.Shared.CF.Character;
using Content.Shared.CF.Combat;
using Content.Shared.CF.Factions;
using Content.Shared.CF.Preferences;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.CF.Spells;

/// <summary>
/// Casts remaster spells from the radial wheel: homing bolts, grid areas, and automatic saves.
/// </summary>
public sealed class CFSpellSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedMapSystem _maps = default!;
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private CFDyingSystem _dying = default!;
    [Dependency] private CFCombatBridgeSystem _bridge = default!;
    [Dependency] private CFFactionSystem _factions = default!;
    [Dependency] private CFPrestidigitationSystem _prestidigitation = default!;
    [Dependency] private SharedActionsSystem _actionDelays = default!;
    [Dependency] private CFActionPoolSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CFSpellComponent, CFSpellCastInstantEvent>(OnInstant);
        SubscribeLocalEvent<CFSpellComponent, CFSpellCastTargetEvent>(OnTarget);
        SubscribeLocalEvent<CFSpellComponent, CFSpellCastAreaEvent>(OnArea);
        SubscribeLocalEvent<CFHomingProjectileComponent, PreventCollideEvent>(OnPreventCollide);
        SubscribeLocalEvent<CFHomingProjectileComponent, ProjectileHitEvent>(OnBoltHit);
        SubscribeLocalEvent<CFSpellComponent, ActionPerformedEvent>(OnVolleyPerformed);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        if (!_net.IsServer)
            return;

        SteerBolts();
        ExpireStatus();
    }

    private void OnInstant(Entity<CFSpellComponent> spell, ref CFSpellCastInstantEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        if (!_net.IsServer || !TrySpend(args.Performer, spell.Comp))
            return;

        ApplyTo(args.Performer, args.Performer, spell, explicitTarget: true);
    }

    private void OnTarget(Entity<CFSpellComponent> spell, ref CFSpellCastTargetEvent args)
    {
        if (args.Handled)
            return;

        if (spell.Comp.ChosenTrick != 0)
        {
            if (!InRange(args.Performer, args.Target, spell.Comp))
            {
                if (_net.IsServer)
                    _popup.PopupEntity(Loc.GetString("cf-spell-out-of-range"), args.Performer, args.Performer);
                return;
            }

            if (!_prestidigitation.TryTrick(args.Performer, args.Target, spell.Comp.ChosenTrick))
                return;

            if (!TrySpend(args.Performer, spell.Comp))
                return;

            args.Handled = true;
            return;
        }

        if (spell.Comp.MaxBolts > 1)
        {
            if (!_net.IsServer)
            {
                args.Handled = true;
                return;
            }

            if (!ShouldAffect(args.Performer, args.Target, spell.Comp, explicitTarget: true))
            {
                _popup.PopupEntity(Loc.GetString("cf-spell-no-target"), args.Performer, args.Performer);
                return;
            }

            if (!InRange(args.Performer, args.Target, spell.Comp))
            {
                _popup.PopupEntity(Loc.GetString("cf-spell-out-of-range"), args.Performer, args.Performer);
                return;
            }

            if (spell.Comp.PayThisShot)
            {
                if (!TrySpend(args.Performer, spell.Comp))
                    return;

                spell.Comp.VolleyLeft = Math.Clamp(spell.Comp.ChosenBolts, 1, spell.Comp.MaxBolts);
            }
            else if (spell.Comp.VolleyLeft <= 0)
            {
                return;
            }

            LaunchBolts(args.Performer, args.Target, spell, 1);
            spell.Comp.VolleyLeft--;
            args.Handled = true;
            return;
        }

        args.Handled = true;
        if (!_net.IsServer)
            return;

        if (!ShouldAffect(args.Performer, args.Target, spell.Comp, explicitTarget: true))
        {
            _popup.PopupEntity(Loc.GetString("cf-spell-no-target"), args.Performer, args.Performer);
            return;
        }

        if (!InRange(args.Performer, args.Target, spell.Comp))
        {
            _popup.PopupEntity(Loc.GetString("cf-spell-out-of-range"), args.Performer, args.Performer);
            return;
        }

        if (!TrySpend(args.Performer, spell.Comp))
            return;

        if (spell.Comp.Delivery == CFSpellDelivery.Projectile)
            LaunchBolts(args.Performer, args.Target, spell);
        else
            ApplyTo(args.Performer, args.Target, spell, explicitTarget: true);
    }

    private void OnArea(Entity<CFSpellComponent> spell, ref CFSpellCastAreaEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        if (!_net.IsServer)
            return;

        if (!TryComp(args.Performer, out TransformComponent? casterXform) || casterXform.GridUid is not { } grid)
        {
            _popup.PopupEntity(Loc.GetString("cf-spell-no-grid"), args.Performer, args.Performer);
            return;
        }

        if (!TryComp(grid, out MapGridComponent? gridComp))
            return;

        var casterMap = _transform.GetMapCoordinates(args.Performer, casterXform);
        var aimMap = _transform.ToMapCoordinates(args.Target);
        if (casterMap.MapId != aimMap.MapId)
            return;

        if (spell.Comp.Area is CFSpellAreaKind.Burst or CFSpellAreaKind.Square)
        {
            var feet = (aimMap.Position - casterMap.Position).Length() * CFSpellGrid.FeetPerTile;
            if (spell.Comp.RangeFeet > 0 && feet > spell.Comp.RangeFeet)
            {
                _popup.PopupEntity(Loc.GetString("cf-spell-out-of-range"), args.Performer, args.Performer);
                return;
            }
        }

        if (!TrySpend(args.Performer, spell.Comp))
            return;

        var casterLocal = _maps.WorldToLocal(grid, gridComp, casterMap.Position);
        var aimLocal = _maps.WorldToLocal(grid, gridComp, aimMap.Position);
        var tiles = CFSpellGrid.Collect(
            spell.Comp.Area,
            spell.Comp.AreaFeet,
            casterLocal,
            aimLocal,
            gridComp.TileSize,
            spell.Comp.IncludeSelf);

        var searchFrom = spell.Comp.Area is CFSpellAreaKind.Burst or CFSpellAreaKind.Square
            ? aimMap
            : casterMap;
        var reach = Math.Max(3f, spell.Comp.AreaFeet / CFSpellGrid.FeetPerTile + 3f);
        var affected = 0;
        foreach (var uid in _lookup.GetEntitiesInRange(searchFrom, reach))
        {
            if (!HasComp<MobStateComponent>(uid) || !TryComp(uid, out TransformComponent? mobXform))
                continue;

            if (mobXform.GridUid != grid)
                continue;

            var local = _maps.WorldToLocal(grid, gridComp, _transform.GetWorldPosition(uid));
            if (!tiles.Contains(CFSpellGrid.ToTile(local, gridComp.TileSize)))
                continue;

            if (!ShouldAffect(args.Performer, uid, spell.Comp, explicitTarget: false))
                continue;

            ApplyTo(args.Performer, uid, spell, explicitTarget: false);
            affected++;
        }

        if (affected == 0)
            _popup.PopupEntity(Loc.GetString("cf-spell-no-one"), args.Performer, args.Performer);
    }

    private void OnVolleyPerformed(Entity<CFSpellComponent> spell, ref ActionPerformedEvent args)
    {
        if (spell.Comp.MaxBolts <= 1 || spell.Comp.VolleyLeft <= 0)
            return;

        _actionDelays.RemoveCooldown(spell.Owner);
    }

    private void LaunchBolts(EntityUid caster, EntityUid target, Entity<CFSpellComponent> spell, int count = 0)
    {
        if (count <= 0)
        {
            count = spell.Comp.MaxBolts <= 1
                ? 1
                : Math.Clamp(spell.Comp.ChosenBolts, 1, spell.Comp.MaxBolts);
        }

        var from = _transform.GetMapCoordinates(caster);
        var to = _transform.GetMapCoordinates(target);
        var direction = to.Position - from.Position;
        if (direction.LengthSquared() < 0.04f)
            direction = new Vector2(1f, 0f);

        direction = Vector2.Normalize(direction);
        var side = new Vector2(-direction.Y, direction.X);
        for (var i = 0; i < count; i++)
        {
            var offset = (i - (count - 1) / 2f) * 0.35f;
            var origin = from.Position + direction * (0.75f + i * 0.12f) + side * offset;
            var bolt = Spawn(spell.Comp.BoltPrototype, new MapCoordinates(origin, from.MapId));
            _gun.ShootProjectile(bolt, direction, Vector2.Zero, null, caster, 18f);

            var homing = EnsureComp<CFHomingProjectileComponent>(bolt);
            homing.Target = target;
            homing.Caster = caster;
            homing.Action = spell.Owner;
            homing.Speed = 18f;
        }
    }

    private void SteerBolts()
    {
        var query = EntityQueryEnumerator<CFHomingProjectileComponent, PhysicsComponent>();
        while (query.MoveNext(out var uid, out var homing, out var physics))
        {
            if (homing.Resolved || !Exists(homing.Target))
            {
                QueueDel(uid);
                continue;
            }

            var targetPos = _transform.GetWorldPosition(homing.Target);
            var pos = _transform.GetWorldPosition(uid);
            var delta = targetPos - pos;
            var distance = delta.Length();
            if (distance < 0.35f)
            {
                ResolveBolt(uid, homing);
                QueueDel(uid);
                continue;
            }

            var direction = delta / distance;
            _physics.SetLinearVelocity(uid, direction * homing.Speed, body: physics);
            _transform.SetWorldRotation(uid, direction.ToWorldAngle());
        }
    }

    private void OnBoltHit(Entity<CFHomingProjectileComponent> bolt, ref ProjectileHitEvent args)
    {
        if (!_net.IsServer || bolt.Comp.Resolved || args.Target != bolt.Comp.Target)
            return;

        ResolveBolt(bolt.Owner, bolt.Comp);
    }

    private void ResolveBolt(EntityUid bolt, CFHomingProjectileComponent homing)
    {
        if (homing.Resolved)
            return;

        homing.Resolved = true;
        if (!Exists(homing.Target) || !TryComp(homing.Action, out CFSpellComponent? spell))
            return;

        if (ShouldAffect(homing.Caster, homing.Target, spell, explicitTarget: true))
            ApplyTo(homing.Caster, homing.Target, (homing.Action, spell), explicitTarget: true);
    }

    private void OnPreventCollide(Entity<CFHomingProjectileComponent> bolt, ref PreventCollideEvent args)
    {
        if (!IsProjectileFixture(bolt.Owner, args.OurFixture))
        {
            args.Cancelled = true;
            return;
        }

        if (args.OtherEntity == bolt.Comp.Target)
            return;

        if (args.OtherEntity == bolt.Comp.Caster
            || HasComp<MobStateComponent>(args.OtherEntity)
            || HasComp<CFHomingProjectileComponent>(args.OtherEntity))
            args.Cancelled = true;
    }

    private bool IsProjectileFixture(EntityUid uid, Fixture fixture)
    {
        if (!TryComp(uid, out FixturesComponent? manager))
            return true;

        foreach (var (id, candidate) in manager.Fixtures)
        {
            if (!ReferenceEquals(candidate, fixture))
                continue;

            return id == SharedProjectileSystem.ProjectileFixture;
        }

        return true;
    }

    private void ApplyTo(EntityUid caster, EntityUid target, Entity<CFSpellComponent> spell, bool explicitTarget)
    {
        var data = spell.Comp;
        GetCasting(caster, out var attackBonus, out var spellDc, out var abilityMod);

        var degree = CFStrikeDegree.Success;
        var landed = true;
        if (data.Defense == CFSpellDefense.SpellAttack)
        {
            attackBonus += _actions.MapPenalty(caster, agile: false);
            _actions.CountAttack(caster);
            degree = RollSpellAttack(caster, target, data, attackBonus);
            landed = degree is CFStrikeDegree.Success or CFStrikeDegree.CriticalSuccess;
        }
        else if (data.Defense is CFSpellDefense.Reflex or CFSpellDefense.Fortitude or CFSpellDefense.Will)
        {
            degree = RollSave(target, data.Defense, spellDc);
            landed = degree is CFStrikeDegree.Failure or CFStrikeDegree.CriticalFailure;
        }

        var flat = data.FlatDamage + (data.AddSpellModifier ? abilityMod : 0);
        var damage = 0;
        var displayCrit = false;

        if (data.Defense == CFSpellDefense.SpellAttack)
        {
            if (landed)
            {
                displayCrit = degree == CFStrikeDegree.CriticalSuccess;
                damage = RollDice(data.DiceCount, data.DiceSize, flat, displayCrit);
            }
        }
        else if (data.BasicSave && data.Defense != CFSpellDefense.None)
        {
            damage = RollDice(data.DiceCount, data.DiceSize, flat, doubleDice: false);
            switch (degree)
            {
                case CFStrikeDegree.CriticalSuccess:
                    damage = 0;
                    break;
                case CFStrikeDegree.Success:
                    damage /= 2;
                    break;
                case CFStrikeDegree.CriticalFailure:
                    damage *= 2;
                    displayCrit = true;
                    break;
            }
        }
        else if (landed || data.Defense == CFSpellDefense.None)
        {
            damage = RollDice(data.DiceCount, data.DiceSize, flat, doubleDice: false);
        }

        if (data.Friendly && data.HealDice > 0)
        {
            var healed = RollDice(data.HealDice, data.HealDie, data.HealFlat, doubleDice: false);
            if (healed > 0 && _bridge.TryEnsureCombatTarget(target, out _, out _, out _))
            {
                _dying.RestoreHitPoints(target, healed, caster);
                ShowNumber(target, CFStrikeDegree.Success, healed, "vitality");
            }

            damage = 0;
        }
        else if (damage > 0)
        {
            DealDamage(caster, target, damage, data.DamageType, displayCrit);
        }
        else if (data.Defense == CFSpellDefense.SpellAttack && !landed)
        {
            ShowNumber(target, CFStrikeDegree.Failure, 0, data.DamageType);
        }
        else if (data.BasicSave && data.Defense != CFSpellDefense.None && data.DiceCount > 0)
        {
            ShowNumber(target, CFStrikeDegree.Failure, 0, data.DamageType);
        }

        if (!landed)
        {
            if (damage <= 0 && data.DiceCount <= 0
                && data.Defense is CFSpellDefense.Reflex or CFSpellDefense.Fortitude or CFSpellDefense.Will)
                _popup.PopupEntity(Loc.GetString("cf-spell-resisted"), target);
            return;
        }

        ApplyRider(caster, target, data, degree);
        if (data.AcBonus != 0 || data.AttackBonus != 0)
            ApplyStatus(target, data, trueStrike: false);

        if (data.Rider == CFSpellRider.None
            && damage <= 0
            && data.HealDice <= 0
            && data.AcBonus == 0
            && data.AttackBonus == 0)
        {
            _popup.PopupEntity(Loc.GetString("cf-spell-affected"), target);
        }

        _ = explicitTarget;
    }

    private CFStrikeDegree RollSpellAttack(EntityUid caster, EntityUid target, CFSpellComponent spell, int attackBonus)
    {
        var armorClass = CFCommonerStats.ArmorClass;
        if (_bridge.TryEnsureCombatTarget(target, out var defender, out _, out _))
            armorClass = defender.ArmorClass;

        if (TryComp(target, out CFSpellStatusComponent? ward))
            armorClass += ward.AcBonus;

        armorClass -= spell.TargetAcPenalty;

        var d20 = _random.Next(1, 21);
        if (TryComp(caster, out CFSpellStatusComponent? boon))
        {
            attackBonus += boon.AttackBonus;
            if (boon.TrueStrike)
            {
                d20 = Math.Max(d20, _random.Next(1, 21));
                boon.TrueStrike = false;
            }
        }

        return CFMelee.DegreeOfSuccess(d20, attackBonus, armorClass);
    }

    private CFStrikeDegree RollSave(EntityUid target, CFSpellDefense defense, int spellDc)
    {
        var d20 = _random.Next(1, 21);
        return CFMelee.DegreeOfSuccess(d20, SaveBonus(target, defense), spellDc);
    }

    private int SaveBonus(EntityUid target, CFSpellDefense defense)
    {
        if (!TryComp(target, out CFCombatantComponent? combat) || string.IsNullOrEmpty(combat.ClassId))
            return CommonerSave(defense);

        if (!TryComp(target, out CFAbilityScoreComponent? abilities))
            return CommonerSave(defense);

        if (!_prototypes.TryIndex(combat.ClassId, out CFClassPrototype? cls))
            return CommonerSave(defense);

        var ability = defense switch
        {
            CFSpellDefense.Reflex => CFAbility.Dexterity,
            CFSpellDefense.Will => CFAbility.Wisdom,
            _ => CFAbility.Constitution,
        };

        var trained = CFAttributeMath.IsSaveTrained(cls, defense.ToString());
        return CFAttributeMath.ComputeSave(abilities.AbilityModifiers, ability, combat.Level, trained);
    }

    private static int CommonerSave(CFSpellDefense defense)
    {
        return defense switch
        {
            CFSpellDefense.Reflex => CFCommonerStats.Reflex,
            CFSpellDefense.Will => CFCommonerStats.Will,
            _ => CFCommonerStats.Fortitude,
        };
    }

    private void GetCasting(EntityUid caster, out int attackBonus, out int spellDc, out int abilityMod)
    {
        attackBonus = 0;
        abilityMod = 0;
        spellDc = 10;
        if (!TryComp(caster, out CFCombatantComponent? combat) || !TryComp(caster, out CFAbilityScoreComponent? abilities))
            return;

        var ability = CFAbility.Charisma;
        if (combat.ClassId != null
            && _prototypes.TryIndex(combat.ClassId, out CFClassPrototype? cls)
            && cls.Spellcasting != null)
        {
            ability = cls.Spellcasting.Ability;
        }
        else
        {
            var best = int.MinValue;
            foreach (var mental in new[] { CFAbility.Intelligence, CFAbility.Wisdom, CFAbility.Charisma })
            {
                var mod = abilities.GetModifier(mental);
                if (mod > best)
                {
                    best = mod;
                    ability = mental;
                }
            }
        }

        abilityMod = abilities.GetModifier(ability);
        var proficiency = CFAttributeMath.ProficiencyBonus(combat.Level, trained: true);
        attackBonus = abilityMod + proficiency;
        spellDc = 10 + abilityMod + proficiency;
    }

    private void DealDamage(EntityUid caster, EntityUid target, int amount, string damageType, bool critical)
    {
        if (!_bridge.TryEnsureCombatTarget(target, out var combat, out var hardness, out var isObject))
            return;

        if (isObject && hardness > 0)
            amount = Math.Max(0, amount - hardness);

        var display = amount <= 0
            ? CFStrikeDegree.Success
            : critical ? CFStrikeDegree.CriticalSuccess : CFStrikeDegree.Success;

        if (!isObject && _dying.IsDown(combat))
        {
            _dying.ApplyDamageWhileDown(target, combat, critical, caster);
            ShowNumber(target, display, amount, damageType);
            return;
        }

        if (amount > 0)
        {
            combat.CurrentHp = Math.Max(0, combat.CurrentHp - amount);
            Dirty(target, combat);
        }

        if (!isObject && combat.CurrentHp <= 0 && amount > 0)
            _dying.KnockOut(target, combat, critical, caster);
        else if (!isObject)
            _dying.SyncHealthBar(target, combat);

        ShowNumber(target, display, amount, damageType);
    }

    private void ApplyRider(EntityUid caster, EntityUid target, CFSpellComponent spell, CFStrikeDegree degree)
    {
        switch (spell.Rider)
        {
            case CFSpellRider.Stabilize:
                if (TryComp(target, out CFCombatantComponent? combat))
                {
                    combat.Dying = 0;
                    Dirty(target, combat);
                }

                RemComp<CFDyingTrackerComponent>(target);
                _popup.PopupEntity(Loc.GetString("cf-spell-stabilize"), target);
                break;
            case CFSpellRider.TrueStrike:
                ApplyStatus(target, spell, trueStrike: true);
                break;
            case CFSpellRider.Sleep:
                _stun.TryKnockdown(
                    target,
                    degree == CFStrikeDegree.CriticalFailure ? TimeSpan.FromSeconds(18) : TimeSpan.FromSeconds(6),
                    refresh: true,
                    autoStand: false,
                    drop: true,
                    force: true);
                _popup.PopupEntity(Loc.GetString("cf-spell-sleep"), target);
                break;
            case CFSpellRider.Paralyze:
                _stun.TryKnockdown(
                    target,
                    degree == CFStrikeDegree.CriticalFailure ? TimeSpan.FromSeconds(24) : TimeSpan.FromSeconds(6),
                    refresh: true,
                    autoStand: false,
                    drop: true,
                    force: true);
                _popup.PopupEntity(Loc.GetString("cf-spell-paralyze"), target);
                break;
            case CFSpellRider.Prone:
                _stun.TryKnockdown(target, TimeSpan.FromSeconds(3), refresh: true, autoStand: true, drop: false, force: true);
                _popup.PopupEntity(Loc.GetString("cf-spell-prone"), target);
                break;
            case CFSpellRider.Push:
                Push(caster, target, spell.PushFeet);
                break;
            case CFSpellRider.Light:
                _popup.PopupEntity(Loc.GetString("cf-spell-light"), target);
                break;
        }
    }

    private void ApplyStatus(EntityUid target, CFSpellComponent spell, bool trueStrike)
    {
        var status = EnsureComp<CFSpellStatusComponent>(target);
        if (spell.AcBonus != 0)
            status.AcBonus = spell.AcBonus;

        if (spell.AttackBonus != 0)
            status.AttackBonus = spell.AttackBonus;

        if (trueStrike)
            status.TrueStrike = true;

        var expires = _timing.CurTime + TimeSpan.FromSeconds(Math.Max(1f, spell.DurationSeconds));
        if (expires > status.Expires)
            status.Expires = expires;
    }

    private void Push(EntityUid origin, EntityUid target, int feet)
    {
        if (!TryComp(target, out PhysicsComponent? body) || body.BodyType == BodyType.Static)
            return;

        var direction = _transform.GetWorldPosition(target) - _transform.GetWorldPosition(origin);
        if (direction.LengthSquared() < 0.01f)
            direction = new Vector2(1f, 0f);
        else
            direction = Vector2.Normalize(direction);

        var speed = Math.Max(1, feet) / CFSpellGrid.FeetPerTile * 6f;
        _physics.SetLinearVelocity(target, direction * speed, body: body);
    }

    private void ExpireStatus()
    {
        var query = EntityQueryEnumerator<CFSpellStatusComponent>();
        while (query.MoveNext(out var uid, out var status))
        {
            if (status.Expires > _timing.CurTime)
                continue;

            RemComp<CFSpellStatusComponent>(uid);
        }
    }

    private bool TrySpend(EntityUid caster, CFSpellComponent spell)
    {
        if (!HasSlot(caster, spell))
        {
            if (spell.Rank > 0)
                _popup.PopupEntity(Loc.GetString("cf-spell-no-slots"), caster, caster);
            return false;
        }

        if (spell.Rank <= 0 || !TryComp<CFSpellbookComponent>(caster, out var book))
            return true;

        if (spell.Rank >= book.Slots.Count || book.Slots[spell.Rank] <= 0)
            return true;

        while (book.Expended.Count <= spell.Rank)
            book.Expended.Add(0);

        book.Expended[spell.Rank]++;
        Dirty(caster, book);
        return true;
    }

    /// <summary>True when this cast can spend one slot of the spell's rank. Cantrips and creatures without a book always can.</summary>
    public bool HasSlot(EntityUid caster, CFSpellComponent spell)
    {
        if (spell.Rank <= 0 || !TryComp<CFSpellbookComponent>(caster, out var book))
            return true;

        if (spell.Rank >= book.Slots.Count || book.Slots[spell.Rank] <= 0)
            return true;

        var spent = spell.Rank < book.Expended.Count ? book.Expended[spell.Rank] : 0;
        return spent < book.Slots[spell.Rank];
    }

    private bool InRange(EntityUid caster, EntityUid target, CFSpellComponent spell)
    {
        if (spell.RangeFeet <= 0)
            return true;

        var from = _transform.GetMapCoordinates(caster);
        var to = _transform.GetMapCoordinates(target);
        if (from.MapId != to.MapId)
            return false;

        return (from.Position - to.Position).Length() * CFSpellGrid.FeetPerTile <= spell.RangeFeet + 0.05f;
    }

    private bool ShouldAffect(EntityUid caster, EntityUid target, CFSpellComponent spell, bool explicitTarget)
    {
        if (TryComp(target, out MobStateComponent? mob) && _mobState.IsDead(target, mob))
            return false;

        if (!spell.Friendly)
            return _factions.CanHarm(caster, target);

        if (explicitTarget)
            return true;

        if (target == caster)
            return spell.IncludeSelf;

        return !_factions.CanHarm(caster, target);
    }

    private int RollDice(int count, int faces, int flat, bool doubleDice)
    {
        if (count <= 0 || faces <= 0)
            return Math.Max(0, flat);

        return CFMelee.RollDamage(_random, count, faces, flat, doubleDice);
    }

    private void ShowNumber(EntityUid target, CFStrikeDegree degree, int amount, string damageType)
    {
        RaiseNetworkEvent(
            new CFCombatNumberEvent(GetNetEntity(target), degree, amount, damageType),
            Filter.Pvs(target));
    }
}
