using Content.Server.Power.Components;
using Content.Server.UserInterface;
using static Content.Shared.Arcade.SharedSpaceVillainArcadeComponent;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Random;

namespace Content.Server.Arcade.SpaceVillain;

public sealed partial class SpaceVillainArcadeSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpaceVillainArcadeComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<SpaceVillainArcadeComponent, AfterActivatableUIOpenEvent>(OnAfterUIOpenSV);
        SubscribeLocalEvent<SpaceVillainArcadeComponent, SpaceVillainArcadePlayerActionMessage>(OnSVPlayerAction);
        SubscribeLocalEvent<SpaceVillainArcadeComponent, PowerChangedEvent>(OnSVillainPower);
    }

    /// <summary>
    /// Called when the user wins the game.
    /// Dispenses a prize if the arcade machine has any left.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="arcade"></param>
    /// <param name="xform"></param>
    public void ProcessWin(EntityUid uid, SpaceVillainArcadeComponent? arcade = null, TransformComponent? xform = null)
    {
        if (!Resolve(uid, ref arcade, ref xform))
            return;
        if (arcade.RewardAmount <= 0)
            return;

        EntityManager.SpawnEntity(_random.Pick(arcade.PossibleRewards), xform.Coordinates);
        arcade.RewardAmount--;
    }

    /// <summary>
    /// Picks a fight-verb from the list of possible Verbs.
    /// </summary>
    /// <returns>A fight-verb.</returns>
    public string GenerateFightVerb(SpaceVillainArcadeComponent arcade)
    {
        return _random.Pick(arcade.PossibleFightVerbs);
    }

    /// <summary>
    /// Generates an enemy-name comprised of a first- and last-name.
    /// </summary>
    /// <returns>An enemy-name.</returns>
    public string GenerateEnemyName(SpaceVillainArcadeComponent arcade)
    {
        return $"{_random.Pick(arcade.PossibleFirstEnemyNames)} {_random.Pick(arcade.PossibleLastEnemyNames)}";
    }

    private void OnComponentInit(EntityUid uid, SpaceVillainArcadeComponent component, ComponentInit args)
    {
        // Random amount of prizes
        component.RewardAmount = new Random().Next(component.RewardMinAmount, component.RewardMaxAmount + 1);
    }

    private void OnSVPlayerAction(EntityUid uid, SpaceVillainArcadeComponent component, SpaceVillainArcadePlayerActionMessage msg)
    {
        if (component.Game == null)
            return;
        if (!TryComp<ApcPowerReceiverComponent>(uid, out var power) || !power.Powered)
            return;

        switch (msg.PlayerAction)
        {
            case PlayerAction.Attack:
            case PlayerAction.Heal:
            case PlayerAction.Recharge:
                component.Game.ExecutePlayerAction(uid, msg.PlayerAction, component);
                break;
            case PlayerAction.NewGame:
                _audioSystem.PlayPvs(component.NewGameSound, uid, AudioParams.Default.WithVolume(-4f));

                component.Game = new SpaceVillainGame(uid, component, this);
                if (_uiSystem.TryGetUi(uid, SpaceVillainArcadeUiKey.Key, out var bui))
                    _uiSystem.SendUiMessage(bui, component.Game.GenerateMetaDataMessage());
                break;
            case PlayerAction.RequestData:
                if (_uiSystem.TryGetUi(uid, SpaceVillainArcadeUiKey.Key, out bui))
                    _uiSystem.SendUiMessage(bui, component.Game.GenerateMetaDataMessage());
                break;
        }
    }

    private void OnAfterUIOpenSV(EntityUid uid, SpaceVillainArcadeComponent component, AfterActivatableUIOpenEvent args)
    {
        component.Game ??= new(uid, component, this);
    }

    private void OnSVillainPower(EntityUid uid, SpaceVillainArcadeComponent component, ref PowerChangedEvent args)
    {
        if (TryComp<ApcPowerReceiverComponent>(uid, out var power) && power.Powered)
            return;

        if (_uiSystem.TryGetUi(uid, SpaceVillainArcadeUiKey.Key, out var bui))
            _uiSystem.CloseAll(bui);
    }
}
