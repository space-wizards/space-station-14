using System.Collections.Generic;
using System.Reflection;
using Content.Shared.GameObjects.Verbs;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using static Content.Shared.GameObjects.EntitySystemMessages.VerbSystemMessages;

namespace Content.Server.GameObjects.EntitySystems
{
    public class VerbSystem : SharedVerbSystem
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<RequestVerbsMessage>(RequestVerbs);
            SubscribeNetworkEvent<UseVerbMessage>(UseVerb);

            IoCManager.InjectDependencies(this);
        }

        private void UseVerb(UseVerbMessage use, EntitySessionEventArgs eventArgs)
        {
            if (!_entityManager.TryGetEntity(use.EntityUid, out var entity))
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

            if (!_entityManager.TryGetEntity(req.EntityUid, out var entity))
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
