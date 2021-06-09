#nullable enable
using System.Collections.Generic;
using System.Reflection;
using Content.Shared.GameObjects.EntitySystemMessages;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.GameTicking;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using static Content.Shared.GameObjects.EntitySystemMessages.VerbSystemMessages;

namespace Content.Server.GameObjects.EntitySystems
{
    public class VerbSystem : SharedVerbSystem, IResettingEntitySystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        private readonly HashSet<IPlayerSession> _seesThroughContainers = new();

        public override void Initialize()
        {
            base.Initialize();

            IoCManager.InjectDependencies(this);

            SubscribeNetworkEvent<RequestVerbsMessage>(RequestVerbs);
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

        public void Reset()
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
            if (!EntityManager.TryGetEntity(use.EntityUid, out var entity))
            {
                return;
            }

            var session = eventArgs.SenderSession;
            var userEntity = session.AttachedEntity;

            if (userEntity == null)
            {
                Logger.Warning($"{nameof(UseVerb)} called by player {session} with no attached entity.");
                return;
            }

            foreach (var (component, verb) in VerbUtility.GetVerbs(entity))
            {
                if ($"{component.GetType()}:{verb.GetType()}" != use.VerbKey)
                {
                    continue;
                }

                if (!VerbUtility.VerbAccessChecks(userEntity, entity, verb))
                {
                    break;
                }

                verb.Activate(userEntity, component);
                break;
            }

            foreach (var globalVerb in VerbUtility.GetGlobalVerbs(Assembly.GetExecutingAssembly()))
            {
                if (globalVerb.GetType().ToString() != use.VerbKey)
                {
                    continue;
                }

                if (!VerbUtility.VerbAccessChecks(userEntity, entity, globalVerb))
                {
                    break;
                }

                globalVerb.Activate(userEntity, entity);
                break;
            }
        }

        private void RequestVerbs(RequestVerbsMessage req, EntitySessionEventArgs eventArgs)
        {
            var player = (IPlayerSession) eventArgs.SenderSession;

            if (!EntityManager.TryGetEntity(req.EntityUid, out var entity))
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

            if (!TryGetContextEntities(userEntity, entity.Transform.MapPosition, out var entities, true) || !entities.Contains(entity))
            {
                return;
            }

            var data = new List<VerbsResponseMessage.NetVerbData>();
            //Get verbs, component dependent.
            foreach (var (component, verb) in VerbUtility.GetVerbs(entity))
            {
                if (!VerbUtility.VerbAccessChecks(userEntity, entity, verb))
                {
                    continue;
                }

                var verbData = verb.GetData(userEntity, component);
                if (verbData.IsInvisible)
                    continue;

                // TODO: These keys being giant strings is inefficient as hell.
                data.Add(new VerbsResponseMessage.NetVerbData(verbData, $"{component.GetType()}:{verb.GetType()}"));
            }

            //Get global verbs. Visible for all entities regardless of their components.
            foreach (var globalVerb in VerbUtility.GetGlobalVerbs(Assembly.GetExecutingAssembly()))
            {
                if (!VerbUtility.VerbAccessChecks(userEntity, entity, globalVerb))
                {
                    continue;
                }

                var verbData = globalVerb.GetData(userEntity, entity);
                if (verbData.IsInvisible)
                    continue;

                data.Add(new VerbsResponseMessage.NetVerbData(verbData, globalVerb.GetType().ToString()));
            }

            var response = new VerbsResponseMessage(data.ToArray(), req.EntityUid);
            RaiseNetworkEvent(response, player.ConnectedClient);
        }
    }
}
