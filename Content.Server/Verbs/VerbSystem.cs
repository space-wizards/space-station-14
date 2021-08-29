using System.Collections.Generic;
using Content.Shared.GameTicking;
using Content.Shared.Verbs;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;

namespace Content.Server.Verbs
{
    public class VerbSystem : SharedVerbSystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        private readonly HashSet<IPlayerSession> _seesThroughContainers = new();

        public override void Initialize()
        {
            base.Initialize();

            IoCManager.InjectDependencies(this);

            SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
            SubscribeNetworkEvent<RequestServerVerbsEvent>(RequestVerbs);
            SubscribeNetworkEvent<UseVerbMessage>(UseVerb);

            _playerManager.PlayerStatusChanged += PlayerStatusChanged;
        }

        private void PlayerStatusChanged(object? sender, SessionStatusEventArgs args)
        {
            if (args.NewStatus == SessionStatus.Disconnected)
            {
                _seesThroughContainers.Remove(args.Session);
            }
        }

        public void Reset(RoundRestartCleanupEvent ev)
        {
            _seesThroughContainers.Clear();
        }

        public void AddContainerVisibility(IPlayerSession session)
        {
            if (!_seesThroughContainers.Add(session))
            {
                return;
            }

            var message = new PlayerContainerVisibilityMessage(true);
            RaiseNetworkEvent(message, session.ConnectedClient);
        }

        public void RemoveContainerVisibility(IPlayerSession session)
        {
            if (!_seesThroughContainers.Remove(session))
            {
                return;
            }

            var message = new PlayerContainerVisibilityMessage(false);
            RaiseNetworkEvent(message, session.ConnectedClient);
        }

        public bool HasContainerVisibility(IPlayerSession session)
        {
            return _seesThroughContainers.Contains(session);
        }

        private void UseVerb(UseVerbMessage use, EntitySessionEventArgs eventArgs)
        {
            var session = eventArgs.SenderSession;
            var userEntity = session.AttachedEntity;

            if (userEntity == null)
            {
                Logger.Warning($"{nameof(UseVerb)} called by player {session} with no attached entity.");
                return;
            }

            if (!EntityManager.TryGetEntity(use.EntityUid, out var targetEntity))
            {
                return;
            }

            // Generate the list of verbs
            var verbAssembly = new AssembleVerbsEvent(userEntity, targetEntity, prepareGUI: false);
            RaiseLocalEvent(targetEntity.Uid, verbAssembly, false);

            // Run the applicable verb
            if (verbAssembly.Verbs.TryGetValue(use.VerbKey, out var verb))
            {
                verb.Act?.Invoke();
            }
            else
            {
                Logger.Warning($"{nameof(UseVerb)} called by player {session} with an invalid verb key: {use.VerbKey}");
                return;
            }
        }

        private void RequestVerbs(RequestServerVerbsEvent req, EntitySessionEventArgs eventArgs)
        {
            var player = (IPlayerSession) eventArgs.SenderSession;

            if (!EntityManager.TryGetEntity(req.EntityUid, out var targetEntity))
            {
                Logger.Warning($"{nameof(RequestVerbs)} called on a nonexistant entity with id {req.EntityUid} by player {player}.");
                return;
            }

            var userEntity = player.AttachedEntity;

            if (userEntity == null)
            {
                Logger.Warning($"{nameof(UseVerb)} called by player {player} with no attached entity.");
                return;
            }

            if (!TryGetContextEntities(userEntity, targetEntity.Transform.MapPosition, out var entities, true) || !entities.Contains(targetEntity))
            {
                return;
            }

            var verbAssembly = new AssembleVerbsEvent(userEntity, targetEntity, prepareGUI: true);
            RaiseLocalEvent(targetEntity.Uid, verbAssembly, false);

            var response = new VerbsResponseMessage(verbAssembly.Verbs, req.EntityUid);
            RaiseNetworkEvent(response, player.ConnectedClient);
        }
    }
}
