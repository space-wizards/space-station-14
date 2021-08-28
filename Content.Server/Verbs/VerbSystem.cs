using System.Collections.Generic;
using System.Reflection;
using Content.Server.Hands.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.GameTicking;
using Content.Shared.Verbs;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;

using static Content.Shared.Verbs.VerbSystemMessages;

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
            SubscribeNetworkEvent<RequestVerbsEvent>(RequestVerbs);
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

            var verbAssembly = new AssembleVerbsEvent(userEntity, targetEntity, prepareGUI: false);
            RaiseLocalEvent(targetEntity.Uid, verbAssembly, false);

            foreach (var verb in verbAssembly.Verbs)
            {
                if (verb.Key == use.VerbKey)
                {
                    verb.Execute();
                    break;
                }
            }
        }

        private void RequestVerbs(RequestVerbsEvent req, EntitySessionEventArgs eventArgs)
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

            var data = new List<VerbsResponseMessage.NetVerbData>();

            foreach (var verb in verbAssembly.Verbs)
            {
                // TODO: These keys being giant strings is inefficient as hell.
                data.Add(new VerbsResponseMessage.NetVerbData(verb));
            }

            var response = new VerbsResponseMessage(data.ToArray(), req.EntityUid);
            RaiseNetworkEvent(response, player.ConnectedClient);
        }
    }

    /// <summary>
    ///     The types of interactions to include when assembling a list of verbs. If null, assembles all verbs
    /// </summary>
    /// <remarks>
    ///     Primary verbs are those that should be triggered when using left-click, 'Z', or 'E' to interact with
    ///     entities. Secondary verbs are for alternative interactions that can be triggered by using the 'alt'
    ///     modifier. Tertiary interactions are global interactions like "examine" or "Debug". Activation verbs are a
    ///     subset of primary interactions that do not try to use the contents of the hand, e.g., to open up a PDA UI
    ///     without picking up the PDA.
    /// </remarks>
    public enum InteractionType
    {
        Primary,
        Secondary,
        Tertiary,
        Activation
    }

    public class AssembleVerbsEvent : EntityEventArgs
    {
        /// <summary>
        ///     Event output. List of verbs that can be executed.
        /// </summary>
        public List<Verb> Verbs = new();

        /// <summary>
        ///     What kind of verbs to assemble. If this is null, includes all verbs.
        /// </summary>
        public InteractionType? Interaction; 

        /// <summary>
        ///     Constant for determining whether the target verb is 'In Range' for physical interactions.
        /// </summary>
        public const float InteractionRangeSquared = 4;

        /// <summary>
        ///     Is the user in range of the target for physical interactions?
        /// </summary>
        public bool InRange;

        /// <summary>
        ///     The entity being targeted for the verb.
        /// </summary>
        public IEntity Target;

        /// <summary>
        ///     The entity that will be "performing" the verb.
        /// </summary>
        public IEntity User;

        /// <summary>
        ///     The entity currently being held by the active hand.
        /// </summary>
        /// <remarks>
        ///     If this is null, but the user has a HandsComponent, the hand is probably empty.
        /// </remarks>
        public IEntity? Using;

        /// <summary>
        ///     The User's hand component.
        /// </summary>
        public HandsComponent? Hands;

        /// <summary>
        ///     Whether or not to load icons and string localizations in preparation for displaying in a GUI.
        /// </summary>
        public bool PrepareGUI;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user">The user that will perform the verb.</param>
        /// <param name="target">The target entity.</param>
        /// <param name="prepareGUI">Whether the verbs will be displayed in a GUI</param>
        /// <param name="interaction">The type of interactions to include as verbs.</param>
        public AssembleVerbsEvent(IEntity user, IEntity target, bool prepareGUI = false, InteractionType? interaction = null)
        {
            Interaction = interaction;
            User = user;
            Target = target;
            PrepareGUI = prepareGUI;

            // Here we check if physical interactions are permitted. First, does the user have hands?
            if (!user.TryGetComponent<HandsComponent>(out var hands))
                return;

            // Are physical interactions blocked somehow?
            if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user))
                return;

            // Can the user physically access the target?
            if (!user.IsInSameOrParentContainer(target))
                return;

            // Physical interactions are allowed.
            Hands = hands;
            Hands.TryGetActiveHeldEntity(out Using);

            // Are they in range? Some verbs may not require this.
            var distanceSquared = (user.Transform.WorldPosition - target.Transform.WorldPosition).LengthSquared;
            InRange = distanceSquared <= InteractionRangeSquared;
        }
    }
}
