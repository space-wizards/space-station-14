using System.Collections.Generic;
using Content.Server.Hands.Components;
using Content.Server.Interaction;
using Content.Server.Morgue.Components;
using Content.Server.Storage.Components;
using Content.Shared.Body.Components;
using Content.Shared.Interaction;
using Content.Shared.MobState.Components;
using Content.Shared.Movement;
using Content.Shared.Storage;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Timing;

namespace Content.Server.Storage.EntitySystems
{
    [UsedImplicitly]
    internal sealed class StorageSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        private readonly List<IPlayerSession> _sessionCache = new();

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<EntRemovedFromContainerMessage>(HandleEntityRemovedFromContainer);
            SubscribeLocalEvent<EntInsertedIntoContainerMessage>(HandleEntityInsertedIntoContainer);

            SubscribeLocalEvent<EntityStorageComponent, GetInteractionVerbsEvent>(AddToggleOpenVerb);
            SubscribeLocalEvent<ServerStorageComponent, GetActivationVerbsEvent>(AddOpenUiVerb);
            SubscribeLocalEvent<EntityStorageComponent, RelayMovementEntityEvent>(OnRelayMovement);
            SubscribeLocalEvent<EntityStorageComponent, ContainerGettingInsertedAttemptEvent>(OnInsertAttempt);
        }

        private void OnInsertAttempt(EntityUid uid, EntityStorageComponent component, ContainerGettingInsertedAttemptEvent args)
        {
            // This entity-storage is being inserted into a container. In general this is fine (e.g., pizza box in a
            // locker). But maybe, just maybe, this is a player somehow being bugged into a backpack.

            // Firstly, the container a backpack/storage entity?
            if (!HasComp<SharedStorageComponent>(args.Container.Owner))
                return;

            // Ok, so we will check if any of the entities we contain is a mob, and if it is, we cancel this attempt. In
            // general, nesting entity-storage inside of a storage is rare. So checking every contained entity should be
            // fine. The only instances I can think of is pizza boxes and body-bags (which should only be insertable if
            // they are folded up anyways).

            foreach (var ent in component.Contents.ContainedEntities)
            {
                if (HasComp<MobStateComponent>(ent) || HasComp<SharedBodyComponent>(ent))
                {
                    args.Cancel();
                    return;
                }
            }
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

        private void AddToggleOpenVerb(EntityUid uid, EntityStorageComponent component, GetInteractionVerbsEvent args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            if (!component.CanOpen(args.User, silent: true))
                return;

            Verb verb = new();
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

        private void AddOpenUiVerb(EntityUid uid, ServerStorageComponent component, GetActivationVerbsEvent args)
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

            Verb verb = new();
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
