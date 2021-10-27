using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Server.Power.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics;

namespace Content.Server.Power.EntitySystems
{
    public sealed class ExtensionCableSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            //Lifecycle events
            SubscribeLocalEvent<ExtensionCableProviderComponent, ComponentStartup>(OnProviderStarted);
            SubscribeLocalEvent<ExtensionCableProviderComponent, ComponentShutdown>(OnProviderShutdown);
            SubscribeLocalEvent<ExtensionCableReceiverComponent, ComponentStartup>(OnReceiverStarted);
            SubscribeLocalEvent<ExtensionCableReceiverComponent, ComponentShutdown>(OnReceiverShutdown);

            //Anchoring
            SubscribeLocalEvent<ExtensionCableReceiverComponent, AnchorStateChangedEvent>(AnchorStateChanged);
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
                RaiseLocalEvent(receiver.Owner.Uid, new ProviderConnectedEvent(provider), broadcast: false);
                RaiseLocalEvent(uid, new ReceiverConnectedEvent(receiver), broadcast: false);
            }
        }

        private void OnProviderShutdown(EntityUid uid, ExtensionCableProviderComponent provider, ComponentShutdown args)
        {
            provider.Connectable = false;
            ResetReceivers(provider);
        }

        private void ResetReceivers(ExtensionCableProviderComponent provider)
        {
            var receivers = provider.LinkedReceivers.ToArray();

            foreach (var receiver in receivers)
            {
                receiver.Provider = null;
                RaiseLocalEvent(receiver.Owner.Uid, new ProviderDisconnectedEvent(provider), broadcast: false);
                RaiseLocalEvent(provider.Owner.Uid, new ReceiverDisconnectedEvent(receiver), broadcast: false);
            }

            foreach (var receiver in receivers)
            {
                TryFindAndSetProvider(receiver);
            }
        }

        private IEnumerable<ExtensionCableReceiverComponent> FindAvailableReceivers(EntityUid uid, float range)
        {
            var owner = EntityManager.GetEntity(uid);

            var nearbyEntities = IoCManager.Resolve<IEntityLookup>()
                .GetEntitiesInRange(owner, range);

            foreach (var entity in nearbyEntities)
            {
                if (EntityManager.TryGetComponent<ExtensionCableReceiverComponent>(entity.Uid, out var receiver) &&
                    receiver.Connectable &&
                    receiver.Provider == null &&
                    entity.Transform.Coordinates.TryDistance(owner.EntityManager, owner.Transform.Coordinates, out var distance) &&
                    distance < Math.Min(range, receiver.ReceptionRange))
                {
                    yield return receiver;
                }
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
                RaiseLocalEvent(provider.Owner.Uid, new ReceiverDisconnectedEvent(receiver), broadcast: false);
                provider.LinkedReceivers.Remove(receiver);
            }

            receiver.ReceptionRange = range;
            TryFindAndSetProvider(receiver);
        }

        private void OnReceiverStarted(EntityUid uid, ExtensionCableReceiverComponent receiver, ComponentStartup args)
        {
            if (receiver.Owner.TryGetComponent(out PhysicsComponent? physicsComponent))
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
            RaiseLocalEvent(receiver.Provider.Owner.Uid, new ReceiverDisconnectedEvent(receiver), broadcast: false);
        }

        private void AnchorStateChanged(EntityUid uid, ExtensionCableReceiverComponent receiver, ref AnchorStateChangedEvent args)
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
                    RaiseLocalEvent(receiver.Provider.Owner.Uid, new ReceiverDisconnectedEvent(receiver), broadcast: false);
                    receiver.Provider.LinkedReceivers.Remove(receiver);
                }

                receiver.Provider = null;
            }
        }

        private void TryFindAndSetProvider(ExtensionCableReceiverComponent receiver)
        {
            if (!TryFindAvailableProvider(receiver.Owner, receiver.ReceptionRange, out var provider)) return;

            receiver.Provider = provider;
            provider.LinkedReceivers.Add(receiver);
            RaiseLocalEvent(receiver.Owner.Uid, new ProviderConnectedEvent(provider), broadcast: false);
            RaiseLocalEvent(provider.Owner.Uid, new ReceiverConnectedEvent(receiver), broadcast: false);
        }

        private static bool TryFindAvailableProvider(IEntity owner, float range, [NotNullWhen(true)] out ExtensionCableProviderComponent? foundProvider)
        {
            var nearbyEntities = IoCManager.Resolve<IEntityLookup>()
                .GetEntitiesInRange(owner, range);

            foreach (var entity in nearbyEntities)
            {
                if (!entity.TryGetComponent<ExtensionCableProviderComponent>(out var provider)) continue;

                if (!provider.Connectable) continue;

                if (!entity.Transform.Coordinates.TryDistance(owner.EntityManager, owner.Transform.Coordinates, out var distance)) continue;

                if (!(distance < Math.Min(range, provider.TransferRange))) continue;

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
