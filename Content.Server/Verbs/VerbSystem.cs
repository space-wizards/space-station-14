using System.Collections.Generic;
using System.Linq;
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
            SubscribeNetworkEvent<RequestServerVerbsEvent>(HandleVerbRequest);
            SubscribeNetworkEvent<TryExecuteVerbEvent>(HandleTryExecuteVerb);

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

        /// <summary>
        ///     Called when asked over the network to run a given verb.
        /// </summary>
        public void HandleTryExecuteVerb(TryExecuteVerbEvent args, EntitySessionEventArgs eventArgs)
        {
            var session = eventArgs.SenderSession;
            var userEntity = session.AttachedEntity;

            if (userEntity == null)
            {
                Logger.Warning($"{nameof(HandleTryExecuteVerb)} called by player {session} with no attached entity.");
                return;
            }

            if (!EntityManager.TryGetEntity(args.Target, out var targetEntity))
            {
                return;
            }

            var verbs = GetVerbs(targetEntity, userEntity, args.Type)[args.Type];
            var verb = verbs.Where((v) => v.Key == args.VerbKey).FirstOrDefault();
            if (verb != null)
            {
                TryExecuteVerb(verb);
            }
            else
            {
                Logger.Warning($"{nameof(HandleTryExecuteVerb)} called by player {session} with an invalid verb key: {args.VerbKey}");
                return;
            }
        }

        private void HandleVerbRequest(RequestServerVerbsEvent args, EntitySessionEventArgs eventArgs)
        {
            var player = (IPlayerSession) eventArgs.SenderSession;

            if (!EntityManager.TryGetEntity(args.EntityUid, out var target))
            {
                Logger.Warning($"{nameof(HandleVerbRequest)} called on a non-existent entity with id {args.EntityUid} by player {player}.");
                return;
            }

            var user = player.AttachedEntity;

            if (user == null)
            {
                Logger.Warning($"{nameof(HandleVerbRequest)} called by player {player} with no attached entity.");
                return;
            }

            VerbsResponseEvent response;

            // Validate input (check that the user can see the entity)
            if (TryGetContextEntities(user, target.Transform.MapPosition, out var entities, true))
            {
                response = new(args.EntityUid, GetVerbs(target, user, args.Type));
            }
            else
            {
                // Don't leave the client hanging on "Waiting for server....", send empty response.
                response = new(args.EntityUid, null);
            }

            RaiseNetworkEvent(response, player.ConnectedClient);
        }
    }
}
