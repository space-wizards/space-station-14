using System.Diagnostics.CodeAnalysis;
using Content.Shared.Alert;
using Content.Shared.GameTicking;
using Content.Shared.Input;
using Content.Shared.Physics.Pull;
using Content.Shared.Pulling.Components;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;

namespace Content.Shared.Pulling
{
    [UsedImplicitly]
    public abstract partial class SharedPullingSystem : EntitySystem
    {
        [Dependency] private readonly SharedPullingStateManagementSystem _pullSm = default!;
        [Dependency] private readonly AlertsSystem _alertsSystem = default!;
        [Dependency] private readonly SharedJointSystem _joints = default!;

        /// <summary>
        ///     A mapping of pullers to the entity that they are pulling.
        /// </summary>
        private readonly Dictionary<EntityUid, EntityUid> _pullers = [];

        private readonly HashSet<EntityUid> _moving = [];
        private readonly HashSet<EntityUid> _stoppedMoving = [];

        public IReadOnlySet<EntityUid> Moving => _moving;

        public override void Initialize()
        {
            base.Initialize();

            UpdatesOutsidePrediction = true;

            SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
            SubscribeLocalEvent<PullStartedMessage>(OnPullStarted);
            SubscribeLocalEvent<PullStoppedMessage>(OnPullStopped);
            SubscribeLocalEvent<EntInsertedIntoContainerMessage>(HandleContainerInsert);
            SubscribeLocalEvent<SharedPullableComponent, JointRemovedEvent>(OnJointRemoved);
            SubscribeLocalEvent<SharedPullableComponent, CollisionChangeEvent>(OnPullableCollisionChange);

            SubscribeLocalEvent<SharedPullableComponent, PullStartedMessage>(PullableHandlePullStarted);
            SubscribeLocalEvent<SharedPullableComponent, PullStoppedMessage>(PullableHandlePullStopped);

            SubscribeLocalEvent<SharedPullableComponent, GetVerbsEvent<Verb>>(AddPullVerbs);

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.MovePulledObject, new PointerInputCmdHandler(HandleMovePulledObject))
                .Register<SharedPullingSystem>();
        }

        private void OnPullableCollisionChange(Entity<SharedPullableComponent> entity, ref CollisionChangeEvent args)
        {
            if (entity.Comp.PullJointId != null && !args.CanCollide)
            {
                _joints.RemoveJoint(entity, entity.Comp.PullJointId);
            }
        }

        private void OnJointRemoved(Entity<SharedPullableComponent> entity, ref JointRemovedEvent args)
        {
            if (entity.Comp.Puller != args.OtherEntity)
                return;

            // Do we have some other join with our Puller?
            // or alternatively:
            // TODO track the relevant joint.

            if (TryComp(entity, out JointComponent? joints))
            {
                foreach (var jt in joints.GetJoints.Values)
                {
                    if (jt.BodyAUid == entity.Comp.Puller.Value || jt.BodyBUid == entity.Comp.Puller.Value)
                        return;
                }
            }

            // No more joints with puller -> force stop pull.
            _pullSm.ForceDisconnectPullable(entity.Owner, entity.Comp);
        }

        private void AddPullVerbs(Entity<SharedPullableComponent> entity, ref GetVerbsEvent<Verb> args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            // Are they trying to pull themselves up by their bootstraps?
            if (args.User == args.Target)
                return;

            //TODO VERB ICONS add pulling icon
            if (entity.Comp.Puller == args.User)
            {
                var user = args.User;
                Verb verb = new()
                {
                    Text = Loc.GetString("pulling-verb-get-data-text-stop-pulling"),
                    Act = () => TryStopPull(entity.Owner, entity.Comp, user),
                    DoContactInteraction = false // pulling handle its own contact interaction.
                };
                args.Verbs.Add(verb);
            }
            else if (CanPull(args.User, args.Target))
            {
                var user = args.User;
                var target = args.Target;
                Verb verb = new()
                {
                    Text = Loc.GetString("pulling-verb-get-data-text"),
                    Act = () => TryStartPull(user, target),
                    DoContactInteraction = false // pulling handle its own contact interaction.
                };
                args.Verbs.Add(verb);
            }
        }

        // Raise a "you are being pulled" alert if the pulled entity has alerts.
        private void PullableHandlePullStarted(Entity<SharedPullableComponent> entity, ref PullStartedMessage args)
        {
            if (args.Pulled != entity.Owner)
                return;

            _alertsSystem.ShowAlert(entity, AlertType.Pulled);
        }

        private void PullableHandlePullStopped(Entity<SharedPullableComponent> entity, ref PullStoppedMessage args)
        {
            if (args.Pulled != entity.Owner)
                return;

            _alertsSystem.ClearAlert(entity, AlertType.Pulled);
        }

        public bool IsPulled(Entity<SharedPullableComponent?> entity)
        {
            return Resolve(entity, ref entity.Comp, false) && entity.Comp.BeingPulled;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            _moving.ExceptWith(_stoppedMoving);
            _stoppedMoving.Clear();
        }

        public void Reset(RoundRestartCleanupEvent ev)
        {
            _pullers.Clear();
            _moving.Clear();
            _stoppedMoving.Clear();
        }

        private void OnPullStarted(PullStartedMessage message)
        {
            SetPuller(message.Puller, message.Pulled);
        }

        private void OnPullStopped(PullStoppedMessage message)
        {
            RemovePuller(message.Puller);
        }

        protected void OnPullableMove(Entity<SharedPullableComponent> entity, ref PullableMoveMessage args)
        {
            _moving.Add(entity);
        }

        protected void OnPullableStopMove(Entity<SharedPullableComponent> entity, ref PullableStopMovingMessage args)
        {
            _stoppedMoving.Add(entity);
        }

        // TODO: When Joint networking is less shitcodey fix this to use a dedicated joints message.
        private void HandleContainerInsert(EntInsertedIntoContainerMessage message)
        {
            if (TryComp(message.Entity, out SharedPullableComponent? pullable))
            {
                TryStopPull((message.Entity, pullable));
            }

            if (TryComp(message.Entity, out SharedPullerComponent? puller))
            {
                if (puller.Pulling == null) return;

                if (!TryComp(puller.Pulling.Value, out SharedPullableComponent? pulling))
                    return;

                TryStopPull((puller.Pulling.Value, pulling));
            }
        }

        private bool HandleMovePulledObject(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
        {
            if (session?.AttachedEntity is not { } player ||
                !player.IsValid())
                return false;

            if (!TryComp<SharedPullerComponent>(player, out var pullerComp))
                return false;

            if (!TryGetPulled((player, pullerComp), out var pulled))
                return false;

            if (!TryComp(pulled.Value, out SharedPullableComponent? pullable))
                return false;

            if (_containerSystem.IsEntityInContainer(player))
                return false;

            TryMoveTo((pulled.Value, pullable), coords);

            return false;
        }

        private void SetPuller(Entity<SharedPullerComponent?> puller, Entity<SharedPullableComponent?> pulled)
        {
            _pullers[puller] = pulled;
        }

        private bool RemovePuller(Entity<SharedPullerComponent?> puller)
        {
            return _pullers.Remove(puller);
        }

        public EntityUid GetPulled(EntityUid by, SharedPullerComponent? comp = null)
        {
            if (!Resolve(by, ref comp))
                return EntityUid.Invalid;

            return GetPulled((by, comp));
        }
        public EntityUid GetPulled(Entity<SharedPullerComponent?> by)
        {
            return _pullers.GetValueOrDefault(by);
        }

        public bool TryGetPulled(EntityUid by, [NotNullWhen(true)] out EntityUid? pulled, SharedPullerComponent? comp = null)
        {
            if (!Resolve(by, ref comp))
            {
                pulled = default;
                return false;
            }

            return TryGetPulled((by, comp), out pulled);
        }

        public bool TryGetPulled(Entity<SharedPullerComponent?> by, [NotNullWhen(true)] out EntityUid? pulled)
        {
            return (pulled = GetPulled(by)) != null;
        }

        public bool IsPulling(Entity<SharedPullerComponent?> puller)
        {
            return _pullers.ContainsKey(puller);
        }
    }
}
