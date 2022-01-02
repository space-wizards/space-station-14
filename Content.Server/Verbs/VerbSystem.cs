using Content.Server.Administration.Managers;
using Content.Server.Popups;
using Content.Shared.Administration;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Hands.Components;
using Content.Shared.Verbs;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Player;

namespace Content.Server.Verbs
{
    public sealed class VerbSystem : SharedVerbSystem
    {
        [Dependency] private readonly SharedAdminLogSystem _logSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly IAdminManager _adminMgr = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<RequestServerVerbsEvent>(HandleVerbRequest);
        }

        private void HandleVerbRequest(RequestServerVerbsEvent args, EntitySessionEventArgs eventArgs)
        {
            var player = (IPlayerSession) eventArgs.SenderSession;

            if (!EntityManager.EntityExists(args.EntityUid))
            {
                Logger.Warning($"{nameof(HandleVerbRequest)} called on a non-existent entity with id {args.EntityUid} by player {player}.");
                return;
            }

            if (player.AttachedEntity is not {} attached)
            {
                Logger.Warning($"{nameof(HandleVerbRequest)} called by player {player} with no attached entity.");
                return;
            }

            // We do not verify that the user has access to the requested entity. The individual verbs should check
            // this, and some verbs (e.g. view variables) won't even care about whether an entity is accessible through
            // the entity menu or not.

            var force = args.AdminRequest && eventArgs.SenderSession is IPlayerSession playerSession &&
                        _adminMgr.HasAdminFlag(playerSession, AdminFlags.Admin);

            var response =
                new VerbsResponseEvent(args.EntityUid, GetLocalVerbs(args.EntityUid, attached, args.Type, force));
            RaiseNetworkEvent(response, player.ConnectedClient);
        }

        /// <summary>
        ///     Execute the provided verb.
        /// </summary>
        /// <remarks>
        ///     This will try to call the action delegates and raise the local events for the given verb.
        /// </remarks>
        public override void ExecuteVerb(Verb verb, EntityUid user, EntityUid target, bool forced = false)
        {
            // is this verb actually valid?
            if (verb.Disabled)
            {
                // Send an informative pop-up message
                if (!string.IsNullOrWhiteSpace(verb.Message))
                    _popupSystem.PopupEntity(verb.Message, user, Filter.Entities(user));

                return;
            }

            // first, lets log the verb. Just in case it ends up crashing the server or something.
            LogVerb(verb, user, target, forced);

            // then invoke any relevant actions
            verb.Act?.Invoke();

            // Maybe raise a local event
            if (verb.ExecutionEventArgs != null)
            {
                if (verb.EventTarget.IsValid())
                    RaiseLocalEvent(verb.EventTarget, verb.ExecutionEventArgs);
                else
                    RaiseLocalEvent(verb.ExecutionEventArgs);
            }
        }

        public void LogVerb(Verb verb, EntityUid user, EntityUid target, bool forced)
        {
            // first get the held item. again.
            EntityUid? holding = null;
            if (TryComp(user, out SharedHandsComponent? hands) &&
                hands.TryGetActiveHeldEntity(out var heldEntity))
            {
                holding = heldEntity;
            }

            // if this is a virtual pull, get the held entity
            if (holding != null && TryComp(holding, out HandVirtualItemComponent? pull))
                holding = pull.BlockingEntity;

            var verbText = $"{verb.Category?.Text} {verb.Text}".Trim();

            // lets not frame people, eh?
            var executionText = forced ? "was forced to execute" : "executed";

            if (holding == null)
            {
                _logSystem.Add(LogType.Verb, verb.Impact,
                        $"{ToPrettyString(user):user} {executionText} the [{verbText:verb}] verb targeting {ToPrettyString(target):target}");
            }
            else
            {
                _logSystem.Add(LogType.Verb, verb.Impact,
                       $"{ToPrettyString(user):user} {executionText} the [{verbText:verb}] verb targeting {ToPrettyString(target):target} while holding {ToPrettyString(holding.Value):held}");
            }
        }
    }
}
