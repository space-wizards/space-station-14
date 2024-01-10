using System.Diagnostics.CodeAnalysis;
using Content.Server.Power.Components;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Server.Power.EntitySystems
{
    public sealed class ExtensionCableSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly SharedMapSystem _mapSystem = default!;

        private EntityQuery<MapGridComponent> _gridQuery;
        private EntityQuery<MetaDataComponent> _metaQuery;
        private EntityQuery<TransformComponent> _xformQuery;
        private EntityQuery<ExtensionCableProviderComponent> _providerQuery;
        private EntityQuery<ExtensionCableReceiverComponent> _receiverQuery;

        public override void Initialize()
        {
            base.Initialize();

            _gridQuery = _entityManager.GetEntityQuery<MapGridComponent>();
            _metaQuery = _entityManager.GetEntityQuery<MetaDataComponent>();
            _xformQuery = _entityManager.GetEntityQuery<TransformComponent>();
            _providerQuery = _entityManager.GetEntityQuery<ExtensionCableProviderComponent>();
            _receiverQuery = _entityManager.GetEntityQuery<ExtensionCableReceiverComponent>();

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

        public void SetProviderTransferRange(Entity<ExtensionCableProviderComponent> provider, int range)
        {
            provider.Comp.TransferRange = range;
            ResetReceivers(provider);
        }

        private void OnProviderStarted(Entity<ExtensionCableProviderComponent> provider, ref ComponentStartup args)
        {
            Connect(provider);
        }

        private void OnProviderShutdown(Entity<ExtensionCableProviderComponent> provider, ref ComponentShutdown args)
        {
            var xform = _xformQuery.GetComponent(provider);

            // If grid deleting no need to update power.
            if (HasComp<MapGridComponent>(xform.GridUid) &&
                _metaQuery.GetComponent(xform.GridUid.Value).EntityLifeStage > EntityLifeStage.MapInitialized)
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

        private void OnProviderReAnchor(Entity<ExtensionCableProviderComponent> provider, ref ReAnchorEvent args)
        {
            Disconnect(provider);
            Connect(provider);
        }

        private void Connect(Entity<ExtensionCableProviderComponent> provider)
        {
            provider.Comp.Connectable = true;

            foreach (var receiver in FindAvailableReceivers(provider, provider.Comp.TransferRange))
            {
                // When multiple providers connect at the same tick, allow receivers to prefer the closest one.
                // This helps to avoid confusing connections during grid init and when batch spawning cables.
                if (receiver.Comp.Provider != null)
                {
                    if (receiver.Comp.ProviderConnectedTick != _gameTiming.CurTick)
                        continue;

                    var receiverXform = _xformQuery.GetComponent(receiver);
                    var providerXform = _xformQuery.GetComponent(provider);
                    var connectedProviderXform = _xformQuery.GetComponent(receiver.Comp.Provider.Value);
                    if (Distance.Compare(receiverXform, providerXform, connectedProviderXform) >= 0)
                        continue;

                    Disconnect(receiver);
                    receiver.Comp.Connectable = true;
                }

                receiver.Comp.Provider = provider;
                receiver.Comp.ProviderConnectedTick = _gameTiming.CurTick;
                provider.Comp.LinkedReceivers.Add(receiver);
                RaiseLocalEvent(receiver, new ProviderConnectedEvent(provider), broadcast: false);
                RaiseLocalEvent(provider, new ReceiverConnectedEvent(receiver), broadcast: false);
            }
        }

        private void Disconnect(Entity<ExtensionCableProviderComponent> provider)
        {
            provider.Comp.Connectable = false;
            ResetReceivers(provider);
        }

        private void ResetReceivers(Entity<ExtensionCableProviderComponent> provider)
        {
            var receivers = provider.Comp.LinkedReceivers.ToArray();
            provider.Comp.LinkedReceivers.Clear();

            foreach (var receiver in receivers)
            {
                RaiseLocalEvent(receiver, new ProviderDisconnectedEvent(provider), broadcast: false);
                RaiseLocalEvent(provider, new ReceiverDisconnectedEvent(receiver), broadcast: false);
            }

            foreach (var receiver in receivers)
            {
                // No point resetting what the receiver is doing if it's deleting, plus significant perf savings
                // in not doing needless lookups
                if (!_entityManager.IsQueuedForDeletion(receiver)
                    && _metaQuery.GetComponent(receiver).EntityLifeStage <= EntityLifeStage.MapInitialized)
                {
                    TryFindAndSetClosestProvider(receiver);
                }
            }
        }

        private IEnumerable<Entity<ExtensionCableReceiverComponent>> FindAvailableReceivers(EntityUid providerUid, float range)
        {
            var xform = _xformQuery.GetComponent(providerUid);
            var coordinates = xform.Coordinates;

            if (!_gridQuery.TryGetComponent(xform.GridUid, out var grid))
                yield break;

            var nearbyEntities =
                _mapSystem.GetCellsInSquareArea(xform.GridUid.Value, grid, coordinates, (int) Math.Ceiling(range / grid.TileSize));

            foreach (var entity in nearbyEntities)
            {
                if (entity == providerUid)
                    continue;

                if (_entityManager.IsQueuedForDeletion(entity) ||
                    _metaQuery.GetComponent(entity).EntityLifeStage > EntityLifeStage.MapInitialized)
                    continue;

                if (!_receiverQuery.TryGetComponent(entity, out var receiver))
                    continue;

                if (!receiver.Connectable)
                    continue;

                var entityXform = _xformQuery.GetComponent(entity);
                var distance = (entityXform.LocalPosition - xform.LocalPosition).Length();
                if (Distance.Compare(distance, Math.Min(range, receiver.ReceptionRange)) <= 0)
                    yield return (entity, receiver);
            }
        }

        #endregion

        #region Receiver

        public void SetReceiverReceptionRange(Entity<ExtensionCableReceiverComponent> receiver, int range)
        {
            var provider = receiver.Comp.Provider;
            receiver.Comp.Provider = null;
            RaiseLocalEvent(receiver, new ProviderDisconnectedEvent(provider), broadcast: false);

            if (provider != null)
            {
                RaiseLocalEvent(provider.Value, new ReceiverDisconnectedEvent(receiver), broadcast: false);
                provider.Value.Comp.LinkedReceivers.Remove(receiver);
            }

            receiver.Comp.ReceptionRange = range;
            TryFindAndSetClosestProvider(receiver);
        }

        private void OnReceiverStarted(Entity<ExtensionCableReceiverComponent> receiver, ref ComponentStartup args)
        {
            if (_entityManager.TryGetComponent(receiver, out PhysicsComponent? physicsComponent))
            {
                receiver.Comp.Connectable = physicsComponent.BodyType == BodyType.Static;
            }

            if (receiver.Comp.Provider == null)
            {
                TryFindAndSetClosestProvider(receiver);
            }
        }

        private void OnReceiverShutdown(Entity<ExtensionCableReceiverComponent> receiver, ref ComponentShutdown args)
        {
            var xform = _xformQuery.GetComponent(receiver);

            // If grid deleting no need to update power.
            if (HasComp<MapGridComponent>(xform.GridUid) &&
                _metaQuery.GetComponent(xform.GridUid.Value).EntityLifeStage > EntityLifeStage.MapInitialized)
            {
                return;
            }

            Disconnect(receiver);
        }

        private void OnReceiverAnchorStateChanged(Entity<ExtensionCableReceiverComponent> receiver, ref AnchorStateChangedEvent args)
        {
            if (args.Anchored)
                Connect(receiver);
            else
                Disconnect(receiver);
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
                TryFindAndSetClosestProvider(receiver);
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

        private void TryFindAndSetClosestProvider(Entity<ExtensionCableReceiverComponent> receiver, TransformComponent? xform = null)
        {
            if (!receiver.Comp.Connectable)
                return;

            if (!TryFindAvailableProvider(receiver, receiver.Comp.ReceptionRange, out var provider, xform))
                return;

            receiver.Comp.Provider = provider;
            receiver.Comp.ProviderConnectedTick = _gameTiming.CurTick;
            provider.Value.Comp.LinkedReceivers.Add(receiver);
            RaiseLocalEvent(receiver, new ProviderConnectedEvent(provider), broadcast: false);
            RaiseLocalEvent(provider.Value, new ReceiverConnectedEvent(receiver), broadcast: false);
        }

        private bool TryFindAvailableProvider(EntityUid receiver, float range,
            [NotNullWhen(true)] out Entity<ExtensionCableProviderComponent>? foundProvider,
            TransformComponent? xform = null)
        {
            if (!Resolve(receiver, ref xform) || !_gridQuery.TryGetComponent(xform.GridUid, out var grid))
            {
                foundProvider = null;
                return false;
            }

            var coordinates = xform.Coordinates;
            var nearbyEntities =
                _mapSystem.GetCellsInSquareArea(xform.GridUid.Value, grid, coordinates, (int) Math.Ceiling(range / grid.TileSize));

            Entity<ExtensionCableProviderComponent>? closestCandidate = null;
            var closestDistanceFound = float.MaxValue;
            foreach (var entity in nearbyEntities)
            {
                if (entity == receiver || !_providerQuery.TryGetComponent(entity, out var provider) || !provider.Connectable)
                    continue;

                if (_entityManager.IsQueuedForDeletion(entity))
                    continue;

                if (!_metaQuery.TryGetComponent(entity, out var meta) || meta.EntityLifeStage > EntityLifeStage.MapInitialized)
                    continue;

                // Find the closest provider
                if (!_xformQuery.TryGetComponent(entity, out var entityXform))
                    continue;
                var distance = (entityXform.LocalPosition - xform.LocalPosition).Length();
                if (Distance.Compare(distance, closestDistanceFound) >= 0)
                    continue;

                closestCandidate = (entity, provider);
                closestDistanceFound = distance;
            }

            // Make sure the provider is in range before claiming success
            if (closestCandidate != null &&
                Distance.Compare(closestDistanceFound, Math.Min(range, closestCandidate.Value.Comp.TransferRange)) <= 0)
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


        private static class Distance
        {
            /// <summary>
            /// Compares transform components distances to another origin transform component
            /// </summary>
            /// <remarks>
            /// Does some weird XY pos comparisons when the distance is equal
            /// </remarks>
            public static int Compare(TransformComponent origin, TransformComponent a, TransformComponent b)
            {
                var distanceToA = (origin.LocalPosition - a.LocalPosition).Length();
                var distanceToB = (origin.LocalPosition - b.LocalPosition).Length();
                if (!MathHelper.CloseTo(distanceToA, distanceToB))
                    return distanceToA.CompareTo(distanceToB);

                // The XY check may seem unnecessary for distance comparison at first. However, it helps to make
                // provider-receiver connections more deterministic, ruling out the spawn order from the equation.
                if (!MathHelper.CloseTo(a.LocalPosition.Y, b.LocalPosition.Y))
                    return a.LocalPosition.Y.CompareTo(b.LocalPosition.Y);
                if (!MathHelper.CloseTo(a.LocalPosition.X, b.LocalPosition.X))
                    return a.LocalPosition.X.CompareTo(b.LocalPosition.X);

                return 0;
            }

            /// <summary>
            /// Compares two float values that represent a distance
            /// </summary>
            public static int Compare(float a, float b)
            {
                return MathHelper.CloseTo(a, b) ? 0 : a.CompareTo(b);
            }
        }
    }
}
