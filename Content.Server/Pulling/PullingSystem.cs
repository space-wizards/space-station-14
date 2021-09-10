using Content.Shared.Input;
using Content.Shared.Pulling;
using Content.Shared.Pulling.Components;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Input.Binding;
using Robust.Shared.Localization;
using Robust.Shared.Physics;
using Robust.Shared.Players;
using System;

namespace Content.Server.Pulling
{
    [UsedImplicitly]
    public class PullingSystem : SharedPullingSystem
    {
        public override void Initialize()
        {
            base.Initialize();

            UpdatesAfter.Add(typeof(PhysicsSystem));

            SubscribeLocalEvent<PullableComponent, PullableMoveMessage>(OnPullableMove);
            SubscribeLocalEvent<PullableComponent, PullableStopMovingMessage>(OnPullableStopMove);

            SubscribeLocalEvent<PullableComponent, GetOtherVerbsEvent>(AddPullVerbs);

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.ReleasePulledObject, InputCmdHandler.FromDelegate(HandleReleasePulledObject))
                .Register<PullingSystem>();
        }

        private void AddPullVerbs(Robust.Shared.GameObjects.EntityUid uid, PullableComponent component, GetOtherVerbsEvent args)
        {
            if (args.Hands == null || !args.CanAccess || !args.CanInteract || !CanPull(args.User, args.Target))
                return;

            // Are they trying to pull themselves up by their bootstraps?
            if (args.User == args.Target)
                return;

            Verb verb = new("togglepull");
            verb.Text = (component.Puller == args.User)
                ? Loc.GetString("pulling-verb-get-data-text-stop-pulling")
                : Loc.GetString("pulling-verb-get-data-text");
            verb.Act = (component.Puller == args.User)
                ? () => component.TryStopPull()
                : () => component.TryStartPull(args.User);
            //TODO VERB ICONS add pulling icon

            args.Verbs.Add(verb);
        }

        private void HandleReleasePulledObject(ICommonSession? session)
        {
            var player = session?.AttachedEntity;

            if (player == null)
            {
                return;
            }

            if (!TryGetPulled(player, out var pulled))
            {
                return;
            }

            if (!pulled.TryGetComponent(out PullableComponent? pullable))
            {
                return;
            }

            pullable.TryStopPull();
        }
    }
}
