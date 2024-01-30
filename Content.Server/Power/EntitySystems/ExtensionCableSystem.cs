using System.Diagnostics.CodeAnalysis;
using Content.Server.Power.Components;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Server.Power.EntitySystems
{
    public sealed class ExtensionCableSystem : EntitySystem
    {
        [Dependency] private readonly SharedMapSystem _mapSystem = default!;

        private EntityQuery<MapGridComponent> _gridQuery;
        private readonly Queue<EntityUid> _providerConnectionQueue = new();
        private readonly Queue<EntityUid> _receiverConnectionQueue = new();

        public override void Initialize()
        {
            base.Initialize();

            _gridQuery = EntityManager.GetEntityQuery<MapGridComponent>();

            //Lifecycle events
            SubscribeLocalEvent<ExtensionCableProviderComponent, ComponentStartup>(OnProviderStarted);
            SubscribeLocalEvent<ExtensionCableProviderComponent, ComponentShutdown>(OnProviderShutdown);
            SubscribeLocalEvent<ExtensionCableReceiverComponent, ComponentStartup>(OnReceiverStarted);
            SubscribeLocalEvent<ExtensionCableReceiverComponent, ComponentShutdown>(OnReceiverShutdown);

            //Anchoring
            // Note that anchoring happens earlier than the startup
            // see SharedTransformSystem.OnCompStartup
            SubscribeLocalEvent<ExtensionCableReceiverComponent, AnchorStateChangedEvent>(OnReceiverAnchorStateChanged);
            SubscribeLocalEvent<ExtensionCableReceiverComponent, ReAnchorEvent>(OnReceiverReAnchor);

            SubscribeLocalEvent<ExtensionCableProviderComponent, AnchorStateChangedEvent>(OnProviderAnchorStateChanged);
            SubscribeLocalEvent<ExtensionCableProviderComponent, ReAnchorEvent>(OnProviderReAnchor);
        }

        public override void Update(float frameTime)
        {
            while (_receiverConnectionQueue.TryDequeue(out var receiverUid))
            {
                if (!TryComp<ExtensionCableReceiverComponent>(receiverUid, out var receiverComp))
                    continue;

                if (receiverComp.Connectable)
                    Connect((receiverUid, receiverComp));
            }

            while (_providerConnectionQueue.TryDequeue(out var providerUid))
            {
                if (!TryComp<ExtensionCableProviderComponent>(providerUid, out var providerComp))
                    continue;

                if (providerComp.Connectable)
                    Connect((providerUid, providerComp));
            }
        }

        #region Provider

        public void SetProviderTransferRange(Entity<ExtensionCableProviderComponent?> provider, int range)
        {
            if (!Resolve(provider, ref provider.Comp))
                return;

            provider.Comp.TransferRange = range;
            ResetReceivers((provider, provider.Comp));
        }

        private void OnProviderStarted(Entity<ExtensionCableProviderComponent> provider, ref ComponentStartup args)
        {
            EnqueueConnect(provider);
        }

        private void OnProviderShutdown(Entity<ExtensionCableProviderComponent> provider, ref ComponentShutdown args)
        {
            var xform = Transform(provider);

            // If grid deleting no need to update power.
            if (HasComp<MapGridComponent>(xform.GridUid) &&
                TerminatingOrDeleted(xform.GridUid.Value))
            {
                return;
            }

            Disconnect(provider);
        }

        private void OnProviderAnchorStateChanged(Entity<ExtensionCableProviderComponent> provider, ref AnchorStateChangedEvent args)
        {
            if (args.Anchored)
                EnqueueConnect(provider);
            else
                Disconnect(provider);
        }

        private void EnqueueConnect(Entity<ExtensionCableProviderComponent> provider)
        {
            provider.Comp.Connectable = true;
            _providerConnectionQueue.Enqueue(provider);
        }

        private void Connect(Entity<ExtensionCableProviderComponent> provider)
        {
            if (provider.Comp.IsConnectCalled)
                return;
            provider.Comp.IsConnectCalled = true;
            var nearbyReceivers =
                GetNearbyEntities<ExtensionCableReceiverComponent>(provider, provider.Comp.TransferRange);
            foreach (var receiver in nearbyReceivers)
            {
                if (receiver.Comp is { Connectable: true, Provider: null })
                    ConnectToClosestProvider(receiver);
            }
        }

        private void Disconnect(Entity<ExtensionCableProviderComponent> provider)
        {
            provider.Comp.Connectable = false;
            provider.Comp.IsConnectCalled = false;
            ResetReceivers(provider);
        }

        private void OnProviderReAnchor(Entity<ExtensionCableProviderComponent> provider, ref ReAnchorEvent args)
        {
            Disconnect(provider);
            EnqueueConnect(provider);
        }

        private void ResetReceivers(Entity<ExtensionCableProviderComponent> provider)
        {
            var receivers = provider.Comp.LinkedReceivers.ToArray();
            provider.Comp.LinkedReceivers.Clear();

            foreach (var receiver in receivers)
            {
                receiver.Comp.Provider = null;
                RaiseLocalEvent(receiver, new ProviderDisconnectedEvent(provider), broadcast: false);
                RaiseLocalEvent(provider, new ReceiverDisconnectedEvent(receiver), broadcast: false);
            }

            foreach (var receiver in receivers)
            {
                // No point resetting what the receiver is doing if it's deleting, plus significant perf savings
                // in not doing needless lookups
                if (TerminatingOrDeleted(receiver) || EntityManager.IsQueuedForDeletion(receiver))
                    continue;

                ConnectToClosestProvider(receiver);
            }
        }

        #endregion

        #region Receiver

        public void SetReceiverReceptionRange(Entity<ExtensionCableReceiverComponent?> receiver, int range)
        {
            if (!Resolve(receiver, ref receiver.Comp))
                return;

            var provider = receiver.Comp.Provider;
            receiver.Comp.Provider = null;
            RaiseLocalEvent(receiver, new ProviderDisconnectedEvent(provider), broadcast: false);

            if (provider != null)
            {
                RaiseLocalEvent(provider.Value, new ReceiverDisconnectedEvent((receiver, receiver.Comp)), broadcast: false);
                provider.Value.Comp.LinkedReceivers.Remove((receiver, receiver.Comp));
            }

            receiver.Comp.ReceptionRange = range;
            ConnectToClosestProvider((receiver, receiver.Comp));
        }

        private void OnReceiverStarted(Entity<ExtensionCableReceiverComponent> receiver, ref ComponentStartup args)
        {
            if (TryComp(receiver, out PhysicsComponent? physicsComponent))
            {
                receiver.Comp.Connectable = physicsComponent.BodyType == BodyType.Static;
            }

            if (receiver.Comp.Connectable)
                EnqueueConnect(receiver);
        }

        private void OnReceiverShutdown(Entity<ExtensionCableReceiverComponent> receiver, ref ComponentShutdown args)
        {
            Disconnect(receiver);
        }

        private void OnReceiverAnchorStateChanged(Entity<ExtensionCableReceiverComponent> receiver, ref AnchorStateChangedEvent args)
        {
            if (args.Anchored)
            {
                EnqueueConnect(receiver);
            }
            else
            {
                Disconnect(receiver);
            }
        }

        private void OnReceiverReAnchor(Entity<ExtensionCableReceiverComponent> receiver, ref ReAnchorEvent args)
        {
            Disconnect(receiver);
            EnqueueConnect(receiver);
        }

        private void EnqueueConnect(Entity<ExtensionCableReceiverComponent> receiver)
        {
            receiver.Comp.Connectable = true;
            _receiverConnectionQueue.Enqueue(receiver);
        }

        private void Connect(Entity<ExtensionCableReceiverComponent> receiver)
        {
            if (receiver.Comp.Provider == null)
            {
                ConnectToClosestProvider(receiver);
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

        private void ConnectToClosestProvider(Entity<ExtensionCableReceiverComponent> receiver)
        {
            if (!receiver.Comp.Connectable)
                return;

            if (!TryFindClosestProvider(receiver, receiver.Comp.ReceptionRange, out var provider))
                return;

            receiver.Comp.Provider = provider;
            provider.Value.Comp.LinkedReceivers.Add(receiver);
            RaiseLocalEvent(receiver, new ProviderConnectedEvent(provider), broadcast: false);
            RaiseLocalEvent(provider.Value, new ReceiverConnectedEvent(receiver), broadcast: false);
        }

        private bool TryFindClosestProvider(
            EntityUid receiver,
            float range,
            [NotNullWhen(true)] out Entity<ExtensionCableProviderComponent>? foundProvider)
        {
            if (!TryComp<TransformComponent>(receiver, out var xform))
            {
                foundProvider = null;
                return false;
            }

            Entity<ExtensionCableProviderComponent>? closestCandidate = null;
            var closestDistanceFound = float.MaxValue;
            var nearbyProviders = GetNearbyEntities<ExtensionCableProviderComponent>(receiver, range);
            foreach (var provider in nearbyProviders)
            {
                if (!provider.Comp.Connectable)
                    continue;

                // Find the closest provider
                if (!TryComp<TransformComponent>(provider, out var providerXform))
                    continue;

                var distance = (providerXform.LocalPosition - xform.LocalPosition).Length();
                if (CompareDistance(distance, closestDistanceFound) >= 0)
                    continue;

                closestCandidate = provider;
                closestDistanceFound = distance;
            }

            // Make sure the provider is in range before claiming success
            if (closestCandidate != null &&
                CompareDistance(closestDistanceFound, Math.Min(range, closestCandidate.Value.Comp.TransferRange)) <= 0)
            {
                foundProvider = closestCandidate;
                return true;
            }

            foundProvider = null;
            return false;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Returns all anchored entities having the <see cref="T"/> component
        /// in the area around the specified entity
        /// </summary>
        /// <param name="uid">Origin entity to look around</param>
        /// <param name="range">The look around distance</param>
        /// <typeparam name="T">Filter entries by the component type</typeparam>
        /// <returns>Entries enumerator</returns>
        private IEnumerable<Entity<T>> GetNearbyEntities<T>(EntityUid uid, float range) where T : IComponent
        {
            if (!TryComp<TransformComponent>(uid, out var xform) ||
                !_gridQuery.TryGetComponent(xform.GridUid, out var grid))
                yield break;

            var nearbyEntities = _mapSystem.GetCellsInSquareArea(xform.GridUid.Value, grid, xform.Coordinates,
                (int) Math.Ceiling(range / grid.TileSize));

            foreach (var entity in nearbyEntities)
            {
                if (entity == uid)
                    continue;

                if (TerminatingOrDeleted(entity) || EntityManager.IsQueuedForDeletion(entity))
                    continue;

                if (!TryComp<T>(entity, out var component))
                    continue;

                yield return (entity, component);
            }
        }

        /// <summary>
        /// Compares two float values that represent a distance
        /// </summary>
        public static int CompareDistance(float a, float b)
        {
            return MathHelper.CloseTo(a, b) ? 0 : a.CompareTo(b);
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
