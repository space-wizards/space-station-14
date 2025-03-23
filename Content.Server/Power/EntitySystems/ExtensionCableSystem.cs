using System.Diagnostics.CodeAnalysis;
using Content.Server.Power.Components;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Server.Power.EntitySystems
{
    public sealed class ExtensionCableSystem : EntitySystem
    {
        [Dependency] private readonly SharedMapSystem _map = default!;

        public override void Initialize()
        {
            base.Initialize();

            //Lifecycle events
            SubscribeLocalEvent<ExtensionCableProviderComponent, ComponentStartup>(OnProviderStarted);
            SubscribeLocalEvent<ExtensionCableProviderComponent, ComponentShutdown>(OnProviderShutdown);
            SubscribeLocalEvent<ExtensionCableReceiverComponent, ComponentStartup>(OnReceiverStarted);
            SubscribeLocalEvent<ExtensionCableReceiverComponent, ComponentShutdown>(OnReceiverShutdown);

            //Anchoring
            SubscribeLocalEvent<ExtensionCableReceiverComponent, AnchorStateChangedEvent>(OnReceiverAnchorStateChanged);
            SubscribeLocalEvent<ExtensionCableReceiverComponent, ReAnchorEvent>(OnReceiverReAnchor);

            SubscribeLocalEvent<ExtensionCableProviderComponent, AnchorStateChangedEvent>(OnProviderAnchorStateChanged);
            SubscribeLocalEvent<ExtensionCableProviderComponent, ReAnchorEvent>(OnProviderReAnchor);
        }

        #region Provider

        public void SetProviderTransferRange(EntityUid uid, int range, ExtensionCableProviderComponent? provider = null)
        {
            if (!Resolve(uid, ref provider))
                return;

            provider.TransferRange = range;
            ResetReceivers((uid, provider));
        }

        private void OnProviderStarted(Entity<ExtensionCableProviderComponent> provider, ref ComponentStartup args)
        {
            Connect(provider);
        }

        private void OnProviderShutdown(Entity<ExtensionCableProviderComponent> provider, ref ComponentShutdown args)
        {
            var xform = Transform(provider);

            // If grid deleting no need to update power.
            if (HasComp<MapGridComponent>(xform.GridUid) &&
                MetaData(xform.GridUid.Value).EntityLifeStage > EntityLifeStage.MapInitialized)
            {
                return;
            }

            Disconnect(provider);
        }

        private void OnProviderAnchorStateChanged(Entity<ExtensionCableProviderComponent> provider, ref AnchorStateChangedEvent args)
        {
            if (args.Anchored)
                Connect(provider);
            else
                Disconnect(provider);
        }

        private void Connect(Entity<ExtensionCableProviderComponent> provider)
        {
            provider.Comp.Connectable = true;

            foreach (var receiver in FindAvailableReceivers(provider.Owner, provider.Comp.TransferRange))
            {
                receiver.Comp.Provider?.Comp.LinkedReceivers.Remove(receiver);
                receiver.Comp.Provider = provider;
                provider.Comp.LinkedReceivers.Add(receiver);
                RaiseLocalEvent(receiver, new ProviderConnectedEvent(provider), broadcast: false);
                RaiseLocalEvent(provider, new ReceiverConnectedEvent(receiver), broadcast: false);
            }
        }

        private void Disconnect(Entity<ExtensionCableProviderComponent> provider)
        {
            // same as OnProviderShutdown
            provider.Comp.Connectable = false;
            ResetReceivers(provider);
        }

        private void OnProviderReAnchor(Entity<ExtensionCableProviderComponent> provider, ref ReAnchorEvent args)
        {
            Disconnect(provider);
            Connect(provider);
        }

        private void ResetReceivers(Entity<ExtensionCableProviderComponent> provider)
        {
            var providerId = provider.Owner;
            var receivers = provider.Comp.LinkedReceivers.ToArray();
            provider.Comp.LinkedReceivers.Clear();

            foreach (var receiver in receivers)
            {
                var receiverId = receiver.Owner;
                receiver.Comp.Provider = null;
                RaiseLocalEvent(receiverId, new ProviderDisconnectedEvent(provider), broadcast: false);
                RaiseLocalEvent(providerId, new ReceiverDisconnectedEvent((receiverId, receiver)), broadcast: false);
            }

            foreach (var receiver in receivers)
            {
                // No point resetting what the receiver is doing if it's deleting, plus significant perf savings
                // in not doing needless lookups
                var receiverId = receiver.Owner;
                if (!EntityManager.IsQueuedForDeletion(receiverId)
                    && MetaData(receiverId).EntityLifeStage <= EntityLifeStage.MapInitialized)
                {
                    TryFindAndSetProvider(receiver);
                }
            }
        }

        private IEnumerable<Entity<ExtensionCableReceiverComponent>> FindAvailableReceivers(EntityUid owner, float range)
        {
            var xform = Transform(owner);
            var coordinates = xform.Coordinates;

            if (!TryComp(xform.GridUid, out MapGridComponent? grid))
                yield break;

            var nearbyEntities = _map.GetCellsInSquareArea(xform.GridUid.Value, grid, coordinates, (int)Math.Ceiling(range / grid.TileSize));

            foreach (var entity in nearbyEntities)
            {
                if (entity == owner)
                    continue;

                if (EntityManager.IsQueuedForDeletion(entity) || MetaData(entity).EntityLifeStage > EntityLifeStage.MapInitialized)
                    continue;

                if (!TryComp(entity, out ExtensionCableReceiverComponent? receiver))
                    continue;

                if (!receiver.Connectable || receiver.Provider != null)
                    continue;

                if ((Transform(entity).LocalPosition - xform.LocalPosition).Length() <= Math.Min(range, receiver.ReceptionRange))
                    yield return (entity, receiver);
            }
        }

        #endregion

        #region Receiver

        public void SetReceiverReceptionRange(EntityUid uid, int range, ExtensionCableReceiverComponent? receiver = null)
        {
            if (!Resolve(uid, ref receiver))
                return;

            var provider = receiver.Provider;
            receiver.Provider = null;
            RaiseLocalEvent(uid, new ProviderDisconnectedEvent(provider), broadcast: false);

            if (provider != null)
            {
                RaiseLocalEvent(provider.Value, new ReceiverDisconnectedEvent((uid, receiver)), broadcast: false);
                provider.Value.Comp.LinkedReceivers.Remove((uid, receiver));
            }

            receiver.ReceptionRange = range;
            TryFindAndSetProvider((uid, receiver));
        }

        private void OnReceiverStarted(Entity<ExtensionCableReceiverComponent> receiver, ref ComponentStartup args)
        {
            if (EntityManager.TryGetComponent(receiver.Owner, out PhysicsComponent? physicsComponent))
            {
                receiver.Comp.Connectable = physicsComponent.BodyType == BodyType.Static;
            }

            if (receiver.Comp.Provider == null)
            {
                TryFindAndSetProvider(receiver);
            }
        }

        private void OnReceiverShutdown(Entity<ExtensionCableReceiverComponent> receiver, ref ComponentShutdown args)
        {
            Disconnect(receiver);
        }

        private void OnReceiverAnchorStateChanged(Entity<ExtensionCableReceiverComponent> receiver, ref AnchorStateChangedEvent args)
        {
            if (args.Anchored)
            {
                Connect(receiver);
            }
            else
            {
                Disconnect(receiver);
            }
        }

        private void OnReceiverReAnchor(Entity<ExtensionCableReceiverComponent> receiver, ref ReAnchorEvent args)
        {
            Disconnect(receiver);
            Connect(receiver);
        }

        private void Connect(Entity<ExtensionCableReceiverComponent> receiver)
        {
            receiver.Comp.Connectable = true;
            if (receiver.Comp.Provider == null)
            {
                TryFindAndSetProvider(receiver);
            }
        }

        private void Disconnect(Entity<ExtensionCableReceiverComponent> receiver)
        {
            receiver.Comp.Connectable = false;
            RaiseLocalEvent(receiver, new ProviderDisconnectedEvent(receiver.Comp.Provider), broadcast: false);
            if (receiver.Comp.Provider != null)
            {
                RaiseLocalEvent(receiver.Comp.Provider.Value, new ReceiverDisconnectedEvent(receiver), broadcast: false);
                receiver.Comp.Provider.Value.Comp.LinkedReceivers.Remove(receiver);
            }

            receiver.Comp.Provider = null;
        }

        private void TryFindAndSetProvider(Entity<ExtensionCableReceiverComponent> receiver, TransformComponent? xform = null)
        {
            var uid = receiver.Owner;
            if (!receiver.Comp.Connectable)
                return;

            if (!TryFindAvailableProvider(uid, receiver.Comp.ReceptionRange, out var provider, xform))
                return;

            receiver.Comp.Provider = provider;
            provider.Value.Comp.LinkedReceivers.Add(receiver);
            RaiseLocalEvent(uid, new ProviderConnectedEvent(provider), broadcast: false);
            RaiseLocalEvent(provider.Value, new ReceiverConnectedEvent((uid, receiver)), broadcast: false);
        }

        private bool TryFindAvailableProvider(EntityUid owner, float range, [NotNullWhen(true)] out Entity<ExtensionCableProviderComponent>? foundProvider, TransformComponent? xform = null)
        {
            if (!Resolve(owner, ref xform) || !TryComp(xform.GridUid, out MapGridComponent? grid))
            {
                foundProvider = null;
                return false;
            }

            var coordinates = xform.Coordinates;
            var nearbyEntities = _map.GetCellsInSquareArea(xform.GridUid.Value, grid, coordinates, (int)Math.Ceiling(range / grid.TileSize));
            var cableQuery = GetEntityQuery<ExtensionCableProviderComponent>();
            var metaQuery = GetEntityQuery<MetaDataComponent>();
            var xformQuery = GetEntityQuery<TransformComponent>();

            Entity<ExtensionCableProviderComponent>? closestCandidate = null;
            var closestDistanceFound = float.MaxValue;
            foreach (var entity in nearbyEntities)
            {
                if (entity == owner || !cableQuery.TryGetComponent(entity, out var provider) || !provider.Connectable)
                    continue;

                if (EntityManager.IsQueuedForDeletion(entity))
                    continue;

                if (!metaQuery.TryGetComponent(entity, out var meta) || meta.EntityLifeStage > EntityLifeStage.MapInitialized)
                    continue;

                // Find the closest provider
                if (!xformQuery.TryGetComponent(entity, out var entityXform))
                    continue;
                var distance = (entityXform.LocalPosition - xform.LocalPosition).Length();
                if (distance >= closestDistanceFound)
                    continue;

                closestCandidate = (entity, provider);
                closestDistanceFound = distance;
            }

            // Make sure the provider is in range before claiming success
            if (closestCandidate != null && closestDistanceFound <= Math.Min(range, closestCandidate.Value.Comp.TransferRange))
            {
                foundProvider = closestCandidate;
                return true;
            }

            foundProvider = null;
            return false;
        }

        #endregion

        #region Events

        /// <summary>
        /// Sent when a <see cref="ExtensionCableProviderComponent"/> connects to a <see cref="ExtensionCableReceiverComponent"/>
        /// </summary>
        public sealed class ProviderConnectedEvent : EntityEventArgs
        {
            /// <summary>
            /// The <see cref="ExtensionCableProviderComponent"/> that connected.
            /// </summary>
            public ExtensionCableProviderComponent Provider;

            public ProviderConnectedEvent(ExtensionCableProviderComponent provider)
            {
                Provider = provider;
            }
        }
        /// <summary>
        /// Sent when a <see cref="ExtensionCableProviderComponent"/> disconnects from a <see cref="ExtensionCableReceiverComponent"/>
        /// </summary>
        public sealed class ProviderDisconnectedEvent : EntityEventArgs
        {
            /// <summary>
            /// The <see cref="ExtensionCableProviderComponent"/> that disconnected.
            /// </summary>
            public ExtensionCableProviderComponent? Provider;

            public ProviderDisconnectedEvent(ExtensionCableProviderComponent? provider)
            {
                Provider = provider;
            }
        }
        /// <summary>
        /// Sent when a <see cref="ExtensionCableReceiverComponent"/> connects to a <see cref="ExtensionCableProviderComponent"/>
        /// </summary>
        public sealed class ReceiverConnectedEvent : EntityEventArgs
        {
            /// <summary>
            /// The <see cref="ExtensionCableReceiverComponent"/> that connected.
            /// </summary>
            public Entity<ExtensionCableReceiverComponent> Receiver;

            public ReceiverConnectedEvent(Entity<ExtensionCableReceiverComponent> receiver)
            {
                Receiver = receiver;
            }
        }
        /// <summary>
        /// Sent when a <see cref="ExtensionCableReceiverComponent"/> disconnects from a <see cref="ExtensionCableProviderComponent"/>
        /// </summary>
        public sealed class ReceiverDisconnectedEvent : EntityEventArgs
        {
            /// <summary>
            /// The <see cref="ExtensionCableReceiverComponent"/> that disconnected.
            /// </summary>
            public Entity<ExtensionCableReceiverComponent> Receiver;

            public ReceiverDisconnectedEvent(Entity<ExtensionCableReceiverComponent> receiver)
            {
                Receiver = receiver;
            }
        }

        #endregion
    }
}
