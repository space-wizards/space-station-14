using Content.Server.Power.Components;
using Content.Server.UserInterface;
using Content.Shared.Arcade;
using Robust.Server.GameObjects;
using Robust.Server.Player;

namespace Content.Server.Arcade.BlockGame;

public sealed class BlockGameArcadeSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlockGameArcadeComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<BlockGameArcadeComponent, AfterActivatableUIOpenEvent>(OnAfterUIOpen);
        SubscribeLocalEvent<BlockGameArcadeComponent, BoundUIClosedEvent>(OnAfterUiClose);
        SubscribeLocalEvent<BlockGameArcadeComponent, PowerChangedEvent>(OnBlockPowerChanged);
        SubscribeLocalEvent<BlockGameArcadeComponent, BlockGameMessages.BlockGamePlayerActionMessage>(OnPlayerAction);
    }

    public override void Update(float frameTime)
    {
        var query = EntityManager.EntityQueryEnumerator<BlockGameArcadeComponent>();
        while (query.MoveNext(out var _, out var blockGame))
        {
            blockGame.Game?.GameTick(frameTime);
        }
    }

    private void UpdatePlayerStatus(EntityUid uid, IPlayerSession session, PlayerBoundUserInterface? bui = null, BlockGameArcadeComponent? blockGame = null)
    {
        if (!Resolve(uid, ref blockGame))
            return;
        if (bui == null && !_uiSystem.TryGetUi(uid, BlockGameUiKey.Key, out bui))
            return;

        _uiSystem.TrySendUiMessage(bui, new BlockGameMessages.BlockGameUserStatusMessage(blockGame.Player == session), session);
    }

    private void OnComponentInit(EntityUid uid, BlockGameArcadeComponent component, ComponentInit args)
    {
        component.Game = new(uid);
    }

    private void OnAfterUIOpen(EntityUid uid, BlockGameArcadeComponent component, AfterActivatableUIOpenEvent args)
    {
        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;
        if (!_uiSystem.TryGetUi(uid, BlockGameUiKey.Key, out var bui))
            return;

        var session = actor.PlayerSession;
        if (!bui.SubscribedSessions.Contains(session))
            return;

        if (component.Player == null)
            component.Player = session;
        else
            component.Spectators.Add(session);

        UpdatePlayerStatus(uid, session, bui, component);
        component.Game?.UpdateNewPlayerUI(session);
    }

    private void OnAfterUiClose(EntityUid uid, BlockGameArcadeComponent component, BoundUIClosedEvent args)
    {
        if (args.Session is not IPlayerSession session)
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
        else
            component.Player = null;

        UpdatePlayerStatus(uid, temp, blockGame: component);
    }

    private void OnBlockPowerChanged(EntityUid uid, BlockGameArcadeComponent component, ref PowerChangedEvent args)
    {
        if (args.Powered)
            return;

        if (_uiSystem.TryGetUi(uid, BlockGameUiKey.Key, out var bui))
            _uiSystem.CloseAll(bui);
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

        component.Game.ProcessInput(msg.PlayerAction);
    }
}
