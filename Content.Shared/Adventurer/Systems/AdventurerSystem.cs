using Content.Shared.Adventurer.Components;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.Dice;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Adventurer.Systems;

/// <summary>
/// Handles the adventuring party mechanics:
/// d20 armor class rolls against incoming attacks, mob threshold overrides
/// and the restriction to adventurer-approved guns.
/// </summary>
public sealed class AdventurerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly MobThresholdSystem _thresholds = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDiceSystem _dice = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public const int DieSides = 20;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AdventurerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<AdventurerComponent, BeforeDamageChangedEvent>(OnBeforeDamageChanged);
        SubscribeLocalEvent<AdventurerComponent, BeforeStaminaDamageEvent>(OnBeforeStaminaDamage);
        SubscribeLocalEvent<AdventurerComponent, ShotAttemptedEvent>(OnShotAttempted);
    }

    private void OnStartup(Entity<AdventurerComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.CritThreshold is { } crit)
            _thresholds.SetMobStateThreshold(ent, crit, MobState.Critical);

        if (ent.Comp.DeadThreshold is { } dead)
            _thresholds.SetMobStateThreshold(ent, dead, MobState.Dead);
    }

    /// <summary>
    /// Rolls a d20 against the adventurer's armor class whenever another entity attacks them.
    /// The outcome is decided instantly; the physical die that spawns is purely cosmetic
    /// and displays the already-decided roll before despawning.
    /// </summary>
    private void OnBeforeDamageChanged(Entity<AdventurerComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (args.Cancelled)
            return;

        // Attacks only: environmental damage (fire, vacuum, poison...) and self-damage
        // apply normally without a roll.
        if (args.Origin is not { } origin || origin == ent.Owner)
            return;

        // Never interfere with healing.
        if (args.Damage.GetTotal() <= FixedPoint2.Zero)
            return;

        var blocked = RollAttack(ent, out var roll);
        if (blocked)
            args.Cancelled = true;

        SpawnFateDie(ent, roll, blocked);
    }

    /// <summary>
    /// Same roll for stamina damage. The event carries no origin, but stamina damage
    /// only ever comes from attacks (batons, disablers, shoves), so it always rolls.
    /// </summary>
    private void OnBeforeStaminaDamage(Entity<AdventurerComponent> ent, ref BeforeStaminaDamageEvent args)
    {
        if (args.Cancelled || args.Value <= 0f)
            return;

        var blocked = RollAttack(ent, out var roll);
        if (blocked)
            args.Cancelled = true;

        SpawnFateDie(ent, roll, blocked);
    }

    /// <summary>
    /// Rolls a d20 against the armor class. Returns true if the attack is blocked.
    /// Deterministic on client and server so damage prediction stays intact; an attack
    /// dealing both regular and stamina damage in the same tick shares one roll.
    /// </summary>
    private bool RollAttack(Entity<AdventurerComponent> ent, out int roll)
    {
        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent.Owner));
        roll = rand.Next(1, DieSides + 1);
        return roll < ent.Comp.ArmorClass;
    }

    /// <summary>
    /// Spawns the cosmetic d20 under the adventurer, sets it to the rolled value and gives it
    /// a light shove. Rate-limited so sustained fire can't flood the map with entities.
    /// </summary>
    private void SpawnFateDie(Entity<AdventurerComponent> ent, int roll, bool blocked)
    {
        // Spawning isn't predicted; the server variant is what everyone sees.
        if (!_net.IsServer)
            return;

        var now = _timing.CurTime;
        if (now < ent.Comp.NextDieTime)
            return;

        ent.Comp.NextDieTime = now + ent.Comp.DieCooldown;

        if (blocked)
        {
            // Broadcast to PVS so the attacker gets miss feedback too.
            _popup.PopupEntity(
                Loc.GetString(ent.Comp.AttackBlockedMessage, ("roll", roll), ("ac", ent.Comp.ArmorClass)),
                ent);
        }

        var die = Spawn(ent.Comp.DiePrototype, Transform(ent).Coordinates);
        if (TryComp<DiceComponent>(die, out var dice))
        {
            _dice.SetCurrentValue((die, dice), roll);
            _audio.PlayPvs(dice.Sound, die);
        }

        if (TryComp<PhysicsComponent>(die, out var physics))
        {
            var impulse = _random.NextAngle().ToVec() * ent.Comp.DieImpulseStrength * physics.Mass;
            _physics.ApplyLinearImpulse(die, impulse, body: physics);
            _physics.ApplyAngularImpulse(die, ent.Comp.DieImpulseStrength * physics.Mass, body: physics);
        }
    }

    /// <summary>
    /// Adventurers refuse to operate guns unless they're adventuring equipment.
    /// This runs before any ammo is taken, so they can't waste the ammo of
    /// weapons they find either.
    /// </summary>
    private void OnShotAttempted(Entity<AdventurerComponent> ent, ref ShotAttemptedEvent args)
    {
        if (args.Cancelled)
            return;

        if (_whitelist.IsWhitelistPassOrNull(ent.Comp.GunWhitelist, args.Used.Owner))
            return;

        args.Cancel();

        // This event fires every tick while the trigger is held; don't spam the popup.
        var now = _timing.CurTime;
        if (now < ent.Comp.NextGunPopupTime)
            return;

        ent.Comp.NextGunPopupTime = now + ent.Comp.GunPopupCooldown;
        _popup.PopupClient(Loc.GetString(ent.Comp.GunFailedMessage, ("gun", args.Used.Owner)), ent, ent);
    }
}
