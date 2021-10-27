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
    public sealed class VerbSystem : SharedVerbSystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        /// <summary>
        ///     List of players that can see all entities on the context menu, ignoring normal visibility rules.
        /// </summary>
        public readonly HashSet<IPlayerSession> SeeAllContextPlayers = new();

        public override void Initialize()
        {
            base.Initialize();

            IoCManager.InjectDependencies(this);

            SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
            SubscribeNetworkEvent<RequestServerVerbsEvent>(HandleVerbRequest);
            SubscribeNetworkEvent<TryExecuteVerbEvent>(HandleTryExecuteVerb);

            _playerManager.PlayerStatusChanged += PlayerStatusChanged;
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _playerManager.PlayerStatusChanged -= PlayerStatusChanged;
        }

        private void PlayerStatusChanged(object? sender, SessionStatusEventArgs args)
        {
            if (args.NewStatus == SessionStatus.Disconnected)
            {
                SeeAllContextPlayers.Remove(args.Session);
            }
        }

        public void Reset(RoundRestartCleanupEvent ev)
        {
            SeeAllContextPlayers.Clear();
        }

        public void ToggleSeeAllContext(IPlayerSession player)
        {
            if (!SeeAllContextPlayers.Add(player))
            {
                SeeAllContextPlayers.Remove(player);
            }

            SetSeeAllContextEvent args = new() { CanSeeAllContext = SeeAllContextPlayers.Contains(player) };
            RaiseNetworkEvent(args, player.ConnectedClient);
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

            // Get the list of verbs. This effectively also checks that the requested verb is in fact a valid verb that
            // the user can perform. In principle, this might waste time checking & preparing unrelated verbs even
            // though we know precisely which one we want. However, MOST entities will only have 1 or 2 verbs of a given
            // type. The one exception here is the "other" verb type, which has 3-4 verbs + all the debug verbs. So maybe
            // the debug verbs should be made a separate type?
            var verbs = GetVerbs(targetEntity, userEntity, args.Type)[args.Type];

            // Find the requested verb.
            if (verbs.TryGetValue(args.RequestedVerb, out var verb))
                TryExecuteVerb(verb);
            else
                // 404 Verb not found
                Logger.Warning($"{nameof(HandleTryExecuteVerb)} called by player {session} with an invalid verb: {args.RequestedVerb.Category?.Text} {args.RequestedVerb.Text}");
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

            // Validate input (check that the user can see the entity)
            TryGetContextEntities(user,
                target.Transform.MapPosition,
                out var entities,
                buffer: true,
                ignoreVisibility: SeeAllContextPlayers.Contains(player));

            VerbsResponseEvent response;
            if (entities != null && entities.Contains(target))
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
