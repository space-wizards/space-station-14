using Content.Shared.Verbs;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;

namespace Content.Server.Verbs
{
    public sealed class VerbSystem : SharedVerbSystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<RequestServerVerbsEvent>(HandleVerbRequest);
            SubscribeNetworkEvent<ExecuteVerbEvent>(HandleTryExecuteVerb);
        }

        /// <summary>
        ///     Called when asked over the network to run a given verb.
        /// </summary>
        public void HandleTryExecuteVerb(ExecuteVerbEvent args, EntitySessionEventArgs eventArgs)
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

            // Get the list of verbs. This effectively also checks that the requested verb is in fact a valid verb that
            // the user can perform.
            var verbs = GetLocalVerbs(targetEntity, userEntity, args.Type)[args.Type];

            // Note that GetLocalVerbs might waste time checking & preparing unrelated verbs even though we know
            // precisely which one we want to run. However, MOST entities will only have 1 or 2 verbs of a given type.
            // The one exception here is the "other" verb type, which has 3-4 verbs + all the debug verbs.

            // Find the requested verb.
            if (verbs.TryGetValue(args.RequestedVerb, out var verb))
                ExecuteVerb(verb);
            else
                // 404 Verb not found. Note that this could happen due to something as simple as opening the verb menu, walking away, then trying
                // to run the pickup-item verb. So maybe this shouldn't even be logged?
                Logger.Info($"{nameof(HandleTryExecuteVerb)} called by player {session} with an invalid verb: {args.RequestedVerb.Category?.Text} {args.RequestedVerb.Text}");
        }

        private void HandleVerbRequest(RequestServerVerbsEvent args, EntitySessionEventArgs eventArgs)
        {
            var player = (IPlayerSession) eventArgs.SenderSession;

            if (!EntityManager.TryGetEntity(args.EntityUid, out var target))
            {
                Logger.Warning($"{nameof(HandleVerbRequest)} called on a non-existent entity with id {args.EntityUid} by player {player}.");
                return;
            }

            if (player.AttachedEntity == null)
            {
                Logger.Warning($"{nameof(HandleVerbRequest)} called by player {player} with no attached entity.");
                return;
            }

            // We do not verify that the user has access to the requested entity. The individual verbs should check
            // this, and some verbs (e.g. view variables) won't even care about whether an entity is accessible through
            // the entity menu or not.

            var response = new VerbsResponseEvent(args.EntityUid, GetLocalVerbs(target, player.AttachedEntity, args.Type));
            RaiseNetworkEvent(response, player.ConnectedClient);
        }
    }
}
