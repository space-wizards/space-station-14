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
            SubscribeNetworkEvent<RequestServerVerbsEvent>(HanddleVerbRequest);
            SubscribeNetworkEvent<UseVerbEvent>(UseVerb);

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

        private void UseVerb(UseVerbEvent use, EntitySessionEventArgs eventArgs)
        {
            var session = eventArgs.SenderSession;
            var userEntity = session.AttachedEntity;

            if (userEntity == null)
            {
                Logger.Warning($"{nameof(UseVerb)} called by player {session} with no attached entity.");
                return;
            }

            if (!EntityManager.TryGetEntity(use.Target, out var targetEntity))
            {
                return;
            }

            // Generate the list of verbs
            var verbEvent = new GetOtherVerbsEvent(userEntity, targetEntity, prepareGUI: false);
            RaiseLocalEvent(targetEntity.Uid, verbEvent);

            // Find the verb that matches the key specified by the message. Maybe verbs should be a dictionary
            // instead of a list. But then you can't use Verbs.Sort(), and other logic becomes messier.
            var requestedVerb = verbEvent.Verbs.FirstOrDefault(verb => verb.Key == use.VerbKey);

            if (requestedVerb == null)
            {
                Logger.Warning($"{nameof(UseVerb)} called by player {session} with an invalid verb key: {use.VerbKey}");
                return;
            }

            TryExecuteVerb(requestedVerb);
        }

        private void HanddleVerbRequest(RequestServerVerbsEvent req, EntitySessionEventArgs eventArgs)
        {
            var player = (IPlayerSession) eventArgs.SenderSession;

            if (!EntityManager.TryGetEntity(req.EntityUid, out var target))
            {
                Logger.Warning($"{nameof(HanddleVerbRequest)} called on a nonexistant entity with id {req.EntityUid} by player {player}.");
                return;
            }

            var user = player.AttachedEntity;

            if (user == null)
            {
                Logger.Warning($"{nameof(UseVerb)} called by player {player} with no attached entity.");
                return;
            }

            // Send the verbs if the user has access to the requested item. Note that this is not perfect, and the
            // entity can be considered invalid/hidden by the server despite being accessible by the client.
            if (TryGetContextEntities(user, target.Transform.MapPosition, out var entities, true))
            {
                GetInteractionVerbsEvent interactionVerbs = new(user, target, prepareGUI: true);
                RaiseLocalEvent(target.Uid, interactionVerbs);

                GetActivationVerbsEvent activationVerbs = new(user, target, prepareGUI: true);
                RaiseLocalEvent(target.Uid, activationVerbs);

                GetAlternativeVerbsEvent alternativeVerbs = new(user, target, prepareGUI: true);
                RaiseLocalEvent(target.Uid, alternativeVerbs);

                GetOtherVerbsEvent otherVerbs = new(user, target, prepareGUI: true);
                RaiseLocalEvent(target.Uid, otherVerbs);

                var response = new VerbsResponseEvent(req.EntityUid,
                    interactionVerbs.Verbs,
                    activationVerbs.Verbs,
                    alternativeVerbs.Verbs,
                    otherVerbs.Verbs);
                RaiseNetworkEvent(response, player.ConnectedClient);
            }

            // TODO VERBS Don't leave the client hanging on "Waiting for server....", send empty response 
        }
    }
}
