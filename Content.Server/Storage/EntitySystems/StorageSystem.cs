using System.Collections.Generic;
using System.Linq;
using Content.Server.Disposal.Unit.Components;
using Content.Server.Disposal.Unit.EntitySystems;
using Content.Server.Hands.Components;
using Content.Server.Storage.Components;
using Content.Shared.Interaction;
using Content.Shared.Movement;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Storage.EntitySystems
{
    [UsedImplicitly]
    public sealed partial class StorageSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly DisposalUnitSystem _disposalSystem = default!;

        private readonly List<IPlayerSession> _sessionCache = new();

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<EntRemovedFromContainerMessage>(HandleEntityRemovedFromContainer);
            SubscribeLocalEvent<EntInsertedIntoContainerMessage>(HandleEntityInsertedIntoContainer);

            SubscribeLocalEvent<EntityStorageComponent, GetVerbsEvent<InteractionVerb>>(AddToggleOpenVerb);
            SubscribeLocalEvent<EntityStorageComponent, RelayMovementEntityEvent>(OnRelayMovement);

            SubscribeLocalEvent<ServerStorageComponent, GetVerbsEvent<ActivationVerb>>(AddOpenUiVerb);
            SubscribeLocalEvent<ServerStorageComponent, GetVerbsEvent<UtilityVerb>>(AddTransferVerbs);

            SubscribeLocalEvent<StorageFillComponent, MapInitEvent>(OnStorageFillMapInit);
        }

        private void OnRelayMovement(EntityUid uid, EntityStorageComponent component, RelayMovementEntityEvent args)
        {
            if (!EntityManager.HasComponent<HandsComponent>(args.Entity))
                return;

            if (_gameTiming.CurTime <
                component.LastInternalOpenAttempt + EntityStorageComponent.InternalOpenAttemptDelay)
            {
                return;
            }

            component.LastInternalOpenAttempt = _gameTiming.CurTime;
            component.TryOpenStorage(args.Entity);
        }

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            foreach (var component in EntityManager.EntityQuery<ServerStorageComponent>())
            {
                CheckSubscribedEntities(component);
            }
        }

        private void AddToggleOpenVerb(EntityUid uid, EntityStorageComponent component, GetVerbsEvent<InteractionVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            if (!component.CanOpen(args.User, silent: true))
                return;

            InteractionVerb verb = new();
            if (component.Open)
            {
                verb.Text = Loc.GetString("verb-common-close");
                verb.IconTexture = "/Textures/Interface/VerbIcons/close.svg.192dpi.png";
            }
            else
            {
                verb.Text = Loc.GetString("verb-common-open");
                verb.IconTexture = "/Textures/Interface/VerbIcons/open.svg.192dpi.png";
            }
            verb.Act = () => component.ToggleOpen(args.User);
            args.Verbs.Add(verb);
        }

        private void AddOpenUiVerb(EntityUid uid, ServerStorageComponent component, GetVerbsEvent<ActivationVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            if (EntityManager.TryGetComponent(uid, out LockComponent? lockComponent) && lockComponent.Locked)
                return;

            // Get the session for the user
            var session = EntityManager.GetComponentOrNull<ActorComponent>(args.User)?.PlayerSession;
            if (session == null)
                return;

            // Does this player currently have the storage UI open?
            var uiOpen = component.SubscribedSessions.Contains(session);

            ActivationVerb verb = new();
            verb.Act = () => component.OpenStorageUI(args.User);
            if (uiOpen)
            {
                verb.Text = Loc.GetString("verb-common-close-ui");
                verb.IconTexture = "/Textures/Interface/VerbIcons/close.svg.192dpi.png";
            }
            else
            {
                verb.Text = Loc.GetString("verb-common-open-ui");
                verb.IconTexture = "/Textures/Interface/VerbIcons/open.svg.192dpi.png";
            }
            args.Verbs.Add(verb);
        }

        private void AddTransferVerbs(EntityUid uid, ServerStorageComponent component, GetVerbsEvent<UtilityVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            var entities = component.Storage?.ContainedEntities;
            if (entities == null || entities.Count == 0)
                return;

            if (TryComp(uid, out LockComponent? lockComponent) && lockComponent.Locked)
                return;

            // if the target is storage, add a verb to transfer storage.
            if (TryComp(args.Target, out ServerStorageComponent? targetStorage)
                && (!TryComp(uid, out LockComponent? targetLock) || !targetLock.Locked))
            {
                UtilityVerb verb = new()
                {
                    Text = Loc.GetString("storage-component-transfer-verb"),
                    IconEntity = args.Using,
                    Act = () => TransferEntities(uid, args.Target, component, lockComponent, targetStorage, targetLock)
                };

                args.Verbs.Add(verb);
            }

            // if the target is a disposal unit, add a verb to transfer storage into the unit (e.g., empty a trash bag).
            if (!TryComp(args.Target, out DisposalUnitComponent? disposal))
                return;

            UtilityVerb dispose = new()
            {
                Text = Loc.GetString("storage-component-dispose-verb"),
                IconEntity = args.Using,
                Act = () => DisposeEntities(args.User, uid, args.Target, component, lockComponent, disposal)
            };

            args.Verbs.Add(dispose);
        }

        /// <summary>
        ///     Move entities from one storage to another.
        /// </summary>
        public void TransferEntities(EntityUid source, EntityUid target,
            ServerStorageComponent? sourceComp = null, LockComponent? sourceLock = null,
            ServerStorageComponent? targetComp = null, LockComponent? targetLock = null)
        {
            if (!Resolve(source, ref sourceComp) || !Resolve(target, ref targetComp))
                return;

            var entities = sourceComp.Storage?.ContainedEntities;
            if (entities == null || entities.Count == 0)
                return;

            if (Resolve(source, ref sourceLock, false) && sourceLock.Locked
                || Resolve(target, ref targetLock, false) && targetLock.Locked)
                return;

            foreach (var entity in entities.ToList())
            {
                targetComp.Insert(entity);
            }
        }

        /// <summary>
        ///     Move entities from storage into a disposal unit.
        /// </summary>
        public void DisposeEntities(EntityUid user, EntityUid source, EntityUid target,
            ServerStorageComponent? sourceComp = null, LockComponent? sourceLock = null,
            DisposalUnitComponent? disposalComp = null)
        {
            if (!Resolve(source, ref sourceComp) || !Resolve(target, ref disposalComp))
                return;

            var entities = sourceComp.Storage?.ContainedEntities;
            if (entities == null || entities.Count == 0)
                return;

            if (Resolve(source, ref sourceLock, false) && sourceLock.Locked)
                return;

            foreach (var entity in entities.ToList())
            {
                if (_disposalSystem.CanInsert(disposalComp, entity)
                    && disposalComp.Container.Insert(entity))
                {
                    _disposalSystem.AfterInsert(disposalComp, entity);
                }
            }
        }

        private void HandleEntityRemovedFromContainer(EntRemovedFromContainerMessage message)
        {
            var oldParentEntity = message.Container.Owner;

            if (EntityManager.TryGetComponent(oldParentEntity, out ServerStorageComponent? storageComp))
            {
                storageComp.HandleEntityMaybeRemoved(message);
            }
        }

        private void HandleEntityInsertedIntoContainer(EntInsertedIntoContainerMessage message)
        {
            var oldParentEntity = message.Container.Owner;

            if (EntityManager.TryGetComponent(oldParentEntity, out ServerStorageComponent? storageComp))
            {
                storageComp.HandleEntityMaybeInserted(message);
            }
        }

        private void CheckSubscribedEntities(ServerStorageComponent storageComp)
        {

            // We have to cache the set of sessions because Unsubscribe modifies the original.
            _sessionCache.Clear();
            _sessionCache.AddRange(storageComp.SubscribedSessions);

            if (_sessionCache.Count == 0)
                return;

            var storagePos = EntityManager.GetComponent<TransformComponent>(storageComp.Owner).WorldPosition;
            var storageMap = EntityManager.GetComponent<TransformComponent>(storageComp.Owner).MapID;

            foreach (var session in _sessionCache)
            {
                // The component manages the set of sessions, so this invalid session should be removed soon.
                if (session.AttachedEntity is not {} attachedEntity || !EntityManager.EntityExists(attachedEntity))
                    continue;

                if (storageMap != EntityManager.GetComponent<TransformComponent>(attachedEntity).MapID)
                    continue;

                var distanceSquared = (storagePos - EntityManager.GetComponent<TransformComponent>(attachedEntity).WorldPosition).LengthSquared;
                if (distanceSquared > SharedInteractionSystem.InteractionRangeSquared)
                {
                    storageComp.UnsubscribeSession(session);
                }
            }
        }
    }
}
