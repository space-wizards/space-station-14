using Content.Server.Power.Components;
using Content.Shared.UserInterface;
using Content.Server.Advertise;
using Content.Server.Advertise.Components;
using Content.Shared.Arcade;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.Arcade.BlockGame;

public sealed class BlockGameArcadeSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SpeakOnUIClosedSystem _speakOnUIClosed = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlockGameArcadeComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<BlockGameArcadeComponent, AfterActivatableUIOpenEvent>(OnAfterUIOpen);
        SubscribeLocalEvent<BlockGameArcadeComponent, PowerChangedEvent>(OnBlockPowerChanged);

        Subs.BuiEvents<BlockGameArcadeComponent>(BlockGameUiKey.Key, subs =>
        {
            subs.Event<BoundUIClosedEvent>(OnAfterUiClose);
            subs.Event<BlockGameMessages.BlockGamePlayerActionMessage>(OnPlayerAction);
        });
    }

    public override void Update(float frameTime)
    {
        var query = EntityManager.EntityQueryEnumerator<BlockGameArcadeComponent>();
        while (query.MoveNext(out var _, out var blockGame))
        {
            blockGame.Game?.GameTick(frameTime);
        }
    }

    private void UpdatePlayerStatus(EntityUid uid, ICommonSession session, BlockGameArcadeComponent? blockGame = null)
    {
        if (!Resolve(uid, ref blockGame))
            return;

        _uiSystem.ServerSendUiMessage(uid, BlockGameUiKey.Key, new BlockGameMessages.BlockGameUserStatusMessage(blockGame.Player == session), session);
    }

    private void OnComponentInit(EntityUid uid, BlockGameArcadeComponent component, ComponentInit args)
    {
        component.Game = new(uid);
    }

    private void OnAfterUIOpen(EntityUid uid, BlockGameArcadeComponent component, AfterActivatableUIOpenEvent args)
    {
        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        if (component.Player == null)
            component.Player = actor.PlayerSession;
        else
            component.Spectators.Add(actor.PlayerSession);

        UpdatePlayerStatus(uid, actor.PlayerSession, component);
        component.Game?.UpdateNewPlayerUI(actor.PlayerSession);
    }

    private void OnAfterUiClose(EntityUid uid, BlockGameArcadeComponent component, BoundUIClosedEvent args)
    {
        if (args.Session is not { } session)
            return;

        if (component.Player != session)
        {
            component.Spectators.Remove(session);
            UpdatePlayerStatus(uid, session, blockGame: component);
            return;
        }

        var temp = component.Player;
        if (component.Spectators.Count > 0)
        {
            component.Player = component.Spectators[0];
            component.Spectators.Remove(component.Player);
            UpdatePlayerStatus(uid, component.Player, blockGame: component);
        }

        UpdatePlayerStatus(uid, temp, blockGame: component);
    }

    private void OnBlockPowerChanged(EntityUid uid, BlockGameArcadeComponent component, ref PowerChangedEvent args)
    {
        if (args.Powered)
            return;

        _uiSystem.CloseUi(uid, BlockGameUiKey.Key);
        component.Player = null;
        component.Spectators.Clear();
    }

    private void OnPlayerAction(EntityUid uid, BlockGameArcadeComponent component, BlockGameMessages.BlockGamePlayerActionMessage msg)
    {
        if (component.Game == null)
            return;
        if (!BlockGameUiKey.Key.Equals(msg.UiKey))
            return;
        if (msg.Session != component.Player)
            return;

        if (msg.PlayerAction == BlockGamePlayerAction.NewGame)
        {
            if (component.Game.Started == true)
                component.Game = new(uid);
            component.Game.StartGame();
            return;
        }

        if (TryComp<SpeakOnUIClosedComponent>(uid, out var speakComponent))
            _speakOnUIClosed.TrySetFlag((uid, speakComponent));

        component.Game.ProcessInput(msg.PlayerAction);
    }
}
