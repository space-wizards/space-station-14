using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Server.Power.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Physics;

namespace Content.Server.Power.EntitySystems
{
    public sealed class ExtensionCableSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

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
            SubscribeLocalEvent<ExtensionCableProviderComponent, AnchorStateChangedEvent>(OnProviderAnchorStateChanged);
        }

        #region Provider

        public void SetProviderTransferRange(EntityUid uid, int range, ExtensionCableProviderComponent? provider = null)
        {
            if (!Resolve(uid, ref provider))
                return;

            provider.TransferRange = range;
            ResetReceivers(provider);
        }

        private void OnProviderStarted(EntityUid uid, ExtensionCableProviderComponent provider, ComponentStartup args)
        {
            foreach (var receiver in FindAvailableReceivers(uid, provider.TransferRange))
            {
                receiver.Provider?.LinkedReceivers.Remove(receiver);
                receiver.Provider = provider;
                provider.LinkedReceivers.Add(receiver);
                RaiseLocalEvent(receiver.Owner, new ProviderConnectedEvent(provider), broadcast: false);
                RaiseLocalEvent(uid, new ReceiverConnectedEvent(receiver), broadcast: false);
            }
        }

        private void OnProviderShutdown(EntityUid uid, ExtensionCableProviderComponent provider, ComponentShutdown args)
        {
            var xform = Transform(uid);

            // If grid deleting no need to update power.
            if (_mapManager.TryGetGrid(xform.GridID, out var grid))
            {
                if (MetaData(grid.GridEntityId).EntityLifeStage > EntityLifeStage.MapInitialized) return;
            }

            provider.Connectable = false;
            ResetReceivers(provider);
        }

        private void OnProviderAnchorStateChanged(EntityUid uid, ExtensionCableProviderComponent provider, ref AnchorStateChangedEvent args)
        {
            if (args.Anchored)
            {
                provider.Connectable = true;

                // same as OnProviderStarted
                foreach (var receiver in FindAvailableReceivers(uid, provider.TransferRange))
                {
                    receiver.Provider?.LinkedReceivers.Remove(receiver);
                    receiver.Provider = provider;
                    provider.LinkedReceivers.Add(receiver);
                    RaiseLocalEvent(receiver.Owner, new ProviderConnectedEvent(provider), broadcast: false);
                    RaiseLocalEvent(uid, new ReceiverConnectedEvent(receiver), broadcast: false);
                }
            }
            else
            {
                // same as OnProviderShutdown
                provider.Connectable = false;
                ResetReceivers(provider);
            }
        }

        private void ResetReceivers(ExtensionCableProviderComponent provider)
        {
            var receivers = provider.LinkedReceivers.ToArray();
            provider.LinkedReceivers.Clear();

            foreach (var receiver in receivers)
            {
                receiver.Provider = null;
                RaiseLocalEvent(receiver.Owner, new ProviderDisconnectedEvent(provider), broadcast: false);
                RaiseLocalEvent(provider.Owner, new ReceiverDisconnectedEvent(receiver), broadcast: false);
            }

            foreach (var receiver in receivers)
            {
                // No point resetting what the receiver is doing if it's deleting, plus significant perf savings
                // in not doing needless lookups
                if (!EntityManager.IsQueuedForDeletion(receiver.Owner)
                    && MetaData(receiver.Owner).EntityLifeStage <= EntityLifeStage.MapInitialized)
                {
                    TryFindAndSetProvider(receiver);
                }
            }
        }

        private IEnumerable<ExtensionCableReceiverComponent> FindAvailableReceivers(EntityUid owner, float range)
        {
            var xform = Transform(owner);
            var coordinates = xform.Coordinates;

            if (!_mapManager.TryGetGrid(xform.GridID, out var grid))
                yield break;

            var nearbyEntities = grid.GetCellsInSquareArea(coordinates, (int) Math.Ceiling(range / grid.TileSize));

            foreach (var entity in nearbyEntities)
            {
                if (entity == owner) continue;

                if (EntityManager.IsQueuedForDeletion(entity) || MetaData(entity).EntityLifeStage > EntityLifeStage.MapInitialized)
                    continue;

                if (!TryComp(entity, out ExtensionCableReceiverComponent? receiver))
                    continue;

                if (!receiver.Connectable || receiver.Provider != null)
                    continue;

                if ((Transform(entity).LocalPosition - xform.LocalPosition).Length < Math.Min(range, receiver.ReceptionRange))
                    yield return receiver;
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
                RaiseLocalEvent(provider.Owner, new ReceiverDisconnectedEvent(receiver), broadcast: false);
                provider.LinkedReceivers.Remove(receiver);
            }

            receiver.ReceptionRange = range;
            TryFindAndSetProvider(receiver);
        }

        private void OnReceiverStarted(EntityUid uid, ExtensionCableReceiverComponent receiver, ComponentStartup args)
        {
            if (EntityManager.TryGetComponent(receiver.Owner, out PhysicsComponent? physicsComponent))
            {
                receiver.Connectable = physicsComponent.BodyType == BodyType.Static;
            }

            if (receiver.Provider == null)
            {
                TryFindAndSetProvider(receiver);
            }
        }

        private void OnReceiverShutdown(EntityUid uid, ExtensionCableReceiverComponent receiver, ComponentShutdown args)
        {
            if (receiver.Provider == null) return;

            receiver.Provider.LinkedReceivers.Remove(receiver);
            RaiseLocalEvent(uid, new ProviderDisconnectedEvent(receiver.Provider), broadcast: false);
            RaiseLocalEvent(receiver.Provider.Owner, new ReceiverDisconnectedEvent(receiver), broadcast: false);
        }

        private void OnReceiverAnchorStateChanged(EntityUid uid, ExtensionCableReceiverComponent receiver, ref AnchorStateChangedEvent args)
        {
            if (args.Anchored)
            {
                receiver.Connectable = true;
                if (receiver.Provider == null)
                {
                    TryFindAndSetProvider(receiver);
                }
            }
            else
            {
                receiver.Connectable = false;
                RaiseLocalEvent(uid, new ProviderDisconnectedEvent(receiver.Provider), broadcast: false);
                if (receiver.Provider != null)
                {
                    RaiseLocalEvent(receiver.Provider.Owner, new ReceiverDisconnectedEvent(receiver), broadcast: false);
                    receiver.Provider.LinkedReceivers.Remove(receiver);
                }

                receiver.Provider = null;
            }
        }

        private void TryFindAndSetProvider(ExtensionCableReceiverComponent receiver, TransformComponent? xform = null)
        {
            if (!receiver.Connectable) return;

            if (!TryFindAvailableProvider(receiver.Owner, receiver.ReceptionRange, out var provider, xform)) return;

            receiver.Provider = provider;
            provider.LinkedReceivers.Add(receiver);
            RaiseLocalEvent(receiver.Owner, new ProviderConnectedEvent(provider), broadcast: false);
            RaiseLocalEvent(provider.Owner, new ReceiverConnectedEvent(receiver), broadcast: false);
        }

        private bool TryFindAvailableProvider(EntityUid owner, float range, [NotNullWhen(true)] out ExtensionCableProviderComponent? foundProvider, TransformComponent? xform = null)
        {
            if (!Resolve(owner, ref xform) || !_mapManager.TryGetGrid(xform.GridID, out var grid))
            {
                foundProvider = null;
                return false;
            }

            var coordinates = xform.Coordinates;
            var nearbyEntities = grid.GetCellsInSquareArea(coordinates, (int) Math.Ceiling(range / grid.TileSize));

            foreach (var entity in nearbyEntities)
            {
                if (entity == owner || !EntityManager.TryGetComponent<ExtensionCableProviderComponent?>(entity, out var provider)) continue;

                if (EntityManager.IsQueuedForDeletion(entity)) continue;

                if (MetaData(entity).EntityLifeStage > EntityLifeStage.MapInitialized) continue;

                if (!provider.Connectable) continue;

                if ((Transform(entity).LocalPosition - xform.LocalPosition).Length > Math.Min(range, provider.TransferRange)) continue;

                foundProvider = provider;
                return true;
            }

            foundProvider = default;
            return false;
        }

        #endregion

        #region Events

        /// <summary>
        /// Sent when a <see cref="ExtensionCableProviderComponent"/> connects to a <see cref="ExtensionCableReceiverComponent"/>
        /// </summary>
        public class ProviderConnectedEvent : EntityEventArgs
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
        public class ProviderDisconnectedEvent : EntityEventArgs
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
        public class ReceiverConnectedEvent : EntityEventArgs
        {
            /// <summary>
            /// The <see cref="ExtensionCableReceiverComponent"/> that connected.
            /// </summary>
            public ExtensionCableReceiverComponent Receiver;

            public ReceiverConnectedEvent(ExtensionCableReceiverComponent receiver)
            {
                Receiver = receiver;
            }
        }
        /// <summary>
        /// Sent when a <see cref="ExtensionCableReceiverComponent"/> disconnects from a <see cref="ExtensionCableProviderComponent"/>
        /// </summary>
        public class ReceiverDisconnectedEvent : EntityEventArgs
        {
            /// <summary>
            /// The <see cref="ExtensionCableReceiverComponent"/> that disconnected.
            /// </summary>
            public ExtensionCableReceiverComponent Receiver;

            public ReceiverDisconnectedEvent(ExtensionCableReceiverComponent receiver)
            {
                Receiver = receiver;
            }
        }

        #endregion
    }
}
