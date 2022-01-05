using Content.Server.Power.Components;
using Robust.Shared.GameObjects;

namespace Content.Server.Power.EntitySystems
{
    public sealed class PowerReceiverSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ApcPowerReceiverComponent, ExtensionCableSystem.ProviderConnectedEvent>(OnProviderConnected);
            SubscribeLocalEvent<ApcPowerReceiverComponent, ExtensionCableSystem.ProviderDisconnectedEvent>(OnProviderDisconnected);

            SubscribeLocalEvent<ApcPowerProviderComponent, ComponentShutdown>(OnProviderShutdown);
            SubscribeLocalEvent<ApcPowerProviderComponent, ExtensionCableSystem.ReceiverConnectedEvent>(OnReceiverConnected);
            SubscribeLocalEvent<ApcPowerProviderComponent, ExtensionCableSystem.ReceiverDisconnectedEvent>(OnReceiverDisconnected);
        }

        private void OnProviderShutdown(EntityUid uid, ApcPowerProviderComponent component, ComponentShutdown args)
        {
            foreach (var receiver in component.LinkedReceivers)
            {
                receiver.NetworkLoad.LinkedNetwork = default;
                component.Net?.QueueNetworkReconnect();
            }

            component.LinkedReceivers.Clear();
        }

        private void OnProviderConnected(EntityUid uid, ApcPowerReceiverComponent receiver, ExtensionCableSystem.ProviderConnectedEvent args)
        {
            var providerUid = args.Provider.Owner;
            if (!EntityManager.TryGetComponent<ApcPowerProviderComponent>(providerUid, out var provider))
                return;

            receiver.Provider = provider;

            ProviderChanged(receiver);
        }

        private void OnProviderDisconnected(EntityUid uid, ApcPowerReceiverComponent receiver, ExtensionCableSystem.ProviderDisconnectedEvent args)
        {
            receiver.Provider = null;

            ProviderChanged(receiver);
        }

        private void OnReceiverConnected(EntityUid uid, ApcPowerProviderComponent provider, ExtensionCableSystem.ReceiverConnectedEvent args)
        {
            if (EntityManager.TryGetComponent(args.Receiver.Owner, out ApcPowerReceiverComponent receiver))
            {
                provider.AddReceiver(receiver);
            }
        }

        private void OnReceiverDisconnected(EntityUid uid, ApcPowerProviderComponent provider, ExtensionCableSystem.ReceiverDisconnectedEvent args)
        {
            if (EntityManager.TryGetComponent(args.Receiver.Owner, out ApcPowerReceiverComponent receiver))
            {
                provider.RemoveReceiver(receiver);
            }
        }

        private static void ProviderChanged(ApcPowerReceiverComponent receiver)
        {
            receiver.NetworkLoad.LinkedNetwork = default;
            receiver.ApcPowerChanged();
        }
    }
}
