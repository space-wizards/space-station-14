using Content.Server.Arcade.Components.SpaceVillain;
using Content.Server.Power.EntitySystems;
using Content.Shared.Arcade.SpaceVillain;
using Content.Shared.Arcade.SpaceVillain.Events;
using Content.Shared.Power;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Arcade.EntitySystems.SpaceVillain;

/// <summary>
///
/// </summary>
public sealed class SpaceVillainArcadeSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PowerReceiverSystem _powerReceiverSystem = default!;
    [Dependency] private readonly ArcadeRewardsSystem _rewardsSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly ArcadeSystem _arcadeSystem = default!;
    [Dependency] private readonly AudioSystem _audioSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpaceVillainArcadeComponent, PowerChangedEvent>(OnPowerChanged);

        Subs.BuiEvents<SpaceVillainArcadeComponent>(SpaceVillainArcadeUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnBUIOpened);
            subs.Event<SpaceVillainAttackActionMessage>(OnAttackAction);
            subs.Event<SpaceVillainHealActionMessage>(OnHealAction);
            subs.Event<SpaceVillainRechargeActionMessage>(OnRechargeAction);
            subs.Event<SpaceVillainNewGameActionMessage>(OnNewGameAction);
            subs.Event<BoundUIClosedEvent>(OnBUIClosed);
        });
    }

    private void OnPowerChanged(Entity<SpaceVillainArcadeComponent> ent, ref PowerChangedEvent args)
    {
        if (args.Powered)
            return;

        _uiSystem.CloseUi(ent.Owner, SpaceVillainArcadeUiKey.Key);
    }

    private void OnBUIOpened(Entity<SpaceVillainArcadeComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!_powerReceiverSystem.IsPowered(ent.Owner))
            return;

        if (_arcadeSystem.GetPlayer(ent) == null)
            _arcadeSystem.SetPlayer(ent, args.Actor);

        SendData(ent, args.Actor);
    }

    private void OnAttackAction(Entity<SpaceVillainArcadeComponent> ent, ref SpaceVillainAttackActionMessage args)
    {
        var component = ent.Comp;

        if (!CanUseAction(ent, args.Actor))
            return;

        var damage = component.VillainInvincible ? 0 : _random.Next(2, 6);

        component.PlayerStatus = Loc.GetString(
            "space-villain-game-player-attack-message",
            ("enemyName", component.VillainName),
            ("attackAmount", damage)
        );

        component.VillainHP -= damage;
        component.HealTracker -= component.HealTracker > 0 ? 1 : 0;

        // No need for AI turn if the game ended.
        if (!UpdateGameState(ent))
        {
            SendData(ent, null);
            return;
        }

        PerformAIAction(component);
        UpdateGameState(ent);

        SendData(ent, null);

        _audioSystem.PlayPvs(component.AttackSound, ent.Owner);
    }

    private void OnHealAction(Entity<SpaceVillainArcadeComponent> ent, ref SpaceVillainHealActionMessage args)
    {
        var component = ent.Comp;

        if (!CanUseAction(ent, args.Actor))
            return;

        var cost = component.PlayerInvincible ? 0 : _random.Next(1, 3);
        var amount = _random.Next(6, 8);

        component.PlayerStatus = Loc.GetString(
            "space-villain-game-player-heal-message",
            ("magicPointAmount", cost),
            ("healAmount", amount)
        );

        component.PlayerMP -= cost;
        component.PlayerHP += amount;
        component.HealTracker++;

        // No need for AI turn if the game ended.
        if (!UpdateGameState(ent))
        {
            SendData(ent, null);
            return;
        }

        PerformAIAction(component);
        UpdateGameState(ent);

        SendData(ent, null);

        _audioSystem.PlayPvs(component.HealSound, ent.Owner);
    }

    private void OnRechargeAction(Entity<SpaceVillainArcadeComponent> ent, ref SpaceVillainRechargeActionMessage args)
    {
        var component = ent.Comp;

        if (!CanUseAction(ent, args.Actor))
            return;

        var amount = _random.Next(4, 7);

        component.PlayerStatus = Loc.GetString(
            "space-villain-game-player-recharge-message",
            ("regainedPoints", amount)
        );

        component.PlayerMP += amount;
        component.HealTracker -= component.HealTracker > 0 ? 1 : 0;

        // No need for AI turn if the game ended.
        if (!UpdateGameState(ent))
        {
            SendData(ent, null);
            return;
        }

        PerformAIAction(component);
        UpdateGameState(ent);

        SendData(ent, null);

        _audioSystem.PlayPvs(component.RechargeSound, ent.Owner);
    }

    private void OnNewGameAction(Entity<SpaceVillainArcadeComponent> ent, ref SpaceVillainNewGameActionMessage args)
    {
        var component = ent.Comp;

        if (_arcadeSystem.GetPlayer(ent) != args.Actor || !_powerReceiverSystem.IsPowered(ent.Owner))
            return;

        if (!_prototypeManager.TryIndex(component.VillainFirstNames, out var firstNames) || !_prototypeManager.TryIndex(component.VillainLastNames, out var lastNames))
            return;

        // Setup fields for the new game.
        component.PlayerHP = component.PlayerMaxHP;
        component.PlayerMP = component.PlayerMaxMP;

        component.VillainName = $"{Loc.GetString(_random.Pick(firstNames.Values))} {Loc.GetString(_random.Pick(lastNames.Values))}";
        component.VillainHP = component.VillainMaxHP;
        component.VillainMP = component.VillainMaxMP;

        component.HealTracker = 0;

        SendData(ent, null);
        _arcadeSystem.PlayNewGameSound(ent);
    }

    private void OnBUIClosed(Entity<SpaceVillainArcadeComponent> ent, ref BoundUIClosedEvent args)
    {
        if (_arcadeSystem.GetPlayer(ent) != args.Actor)
            return;

        _arcadeSystem.SetPlayer(ent, null);

        _uiSystem.CloseUi(ent.Owner, SpaceVillainArcadeUiKey.Key);
    }

    /// <summary>
    ///
    /// </summary>
    private void PerformAIAction(SpaceVillainArcadeComponent component)
    {
        if (component.HealTracker >= 4)
        {
            var damage = component.PlayerInvincible ? 0 : _random.Next(5, 10);

            component.VillainStatus = Loc.GetString(
                "space-villain-game-enemy-throws-bomb-message",
                ("enemyName", component.VillainName),
                ("damageReceived", damage)
            );

            component.PlayerHP -= damage;
            component.HealTracker--;
        }
        else if (component.VillainMP <= 5 && _random.Prob(0.7f))
        {
            var amount = component.PlayerInvincible ? 0 : _random.Next(2, 3);

            component.VillainStatus = Loc.GetString(
                "space-villain-game-enemy-steals-player-power-message",
                ("enemyName", component.VillainName),
                ("stolenAmount", amount)
            );

            component.PlayerMP -= amount;
            component.VillainMP += amount;
        }
        else if (component.VillainHP <= 10 && component.VillainMP > 4)
        {
            component.VillainStatus = Loc.GetString(
                "space-villain-game-enemy-heals-message",
                ("enemyName", component.VillainName),
                ("healedAmount", 4)
            );

            component.VillainHP += 4;
            component.VillainMP -= component.VillainInvincible ? 0 : 4;
        }
        else
        {
            var damage = component.PlayerInvincible ? 0 : _random.Next(3, 6);

            component.VillainStatus = Loc.GetString(
                "space-villain-game-enemy-attacks-message",
                ("enemyName", component.VillainName),
                ("damageDealt", damage)
            );

            component.PlayerHP -= damage;
        }
    }

    /// <summary>
    ///
    /// </summary>
    private void SendData(Entity<SpaceVillainArcadeComponent> ent, EntityUid? actor = null)
    {
        var component = ent.Comp;

        var message = new SpaceVillainUpdateDataMessage(component.PlayerHP, component.PlayerMP, component.VillainName, component.VillainHP,
            component.VillainMP, component.PlayerStatus, component.VillainStatus);

        if (actor != null)
            _uiSystem.ServerSendUiMessage(ent.Owner, SpaceVillainArcadeUiKey.Key, message, actor.Value);
        else
            _uiSystem.ServerSendUiMessage(ent.Owner, SpaceVillainArcadeUiKey.Key, message);
    }

    /// <summary>
    ///
    /// </summary>
    private bool UpdateGameState(Entity<SpaceVillainArcadeComponent> ent)
    {
        var component = ent.Comp;

        switch (IsActorLoss(component), IsAILoss(component))
        {
            case (false, true):
                component.PlayerStatus = Loc.GetString("space-villain-game-player-wins-message");
                component.VillainStatus = Loc.GetString("space-villain-game-enemy-dies-message", ("enemyName", component.VillainName));

                _arcadeSystem.PlayWinSound(ent);
                _rewardsSystem.GiveReward(ent);

                return false;
            case (true, false):
                component.PlayerStatus = Loc.GetString("space-villain-game-player-loses-message");
                component.VillainStatus = Loc.GetString("space-villain-game-enemy-cheers-message", ("enemyName", component.VillainName));

                _arcadeSystem.PlayLossSound(ent);

                return false;
            case (true, true):
                component.PlayerStatus = Loc.GetString("space-villain-game-player-loses-message");
                component.VillainStatus = Loc.GetString("space-villain-game-enemy-dies-with-player-message ", ("enemyName", component.VillainName));

                _arcadeSystem.PlayLossSound(ent);

                return false;
        }

        return true;
    }

    /// <summary>
    ///
    /// </summary>
    private bool CanUseAction(Entity<SpaceVillainArcadeComponent> ent, EntityUid actor)
    {
        var component = ent.Comp;

        if (_arcadeSystem.GetPlayer(ent) != actor || !_powerReceiverSystem.IsPowered(ent.Owner))
            return false;

        if (IsActorLoss(component) || IsAILoss(component))
            return false;

        return true;
    }

    /// <summary>
    ///
    /// </summary>
    private static bool IsActorLoss(SpaceVillainArcadeComponent component)
    {
        return component.PlayerHP <= 0 || component.PlayerMP <= 0;
    }

    /// <summary>
    ///
    /// </summary>
    private static bool IsAILoss(SpaceVillainArcadeComponent component)
    {
        return component.VillainHP <= 0 || component.VillainMP <= 0;
    }

    /// <summary>
    ///
    /// </summary>
    public void SetPlayerInvincibility(EntityUid uid, bool invincibility, SpaceVillainArcadeComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.PlayerInvincible = invincibility;
    }

    /// <summary>
    ///
    /// </summary>
    public bool GetPlayerInvincibility(EntityUid uid, SpaceVillainArcadeComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        return component.PlayerInvincible;
    }

    /// <summary>
    ///
    /// </summary>
    public void SetVillainInvincibility(EntityUid uid, bool invincibility, SpaceVillainArcadeComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.VillainInvincible = invincibility;
    }

    /// <summary>
    ///
    /// </summary>
    public bool GetVillainInvincibility(EntityUid uid, SpaceVillainArcadeComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        return component.VillainInvincible;
    }
}
