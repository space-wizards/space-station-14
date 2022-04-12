using System.Threading;
using Content.Server.Power.Components;
using Content.Server.Wires;
using Content.Shared.Examine;
using Content.Shared.Power;

namespace Content.Server.Power.EntitySystems
{
    public sealed class PowerReceiverSystem : EntitySystem
    {
        [Dependency] WiresSystem _wiresSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ApcPowerReceiverComponent, ExaminedEvent>(OnExamined);

            SubscribeLocalEvent<ApcPowerReceiverComponent, ExtensionCableSystem.ProviderConnectedEvent>(OnProviderConnected);
            SubscribeLocalEvent<ApcPowerReceiverComponent, ExtensionCableSystem.ProviderDisconnectedEvent>(OnProviderDisconnected);

            SubscribeLocalEvent<ApcPowerProviderComponent, ComponentShutdown>(OnProviderShutdown);
            SubscribeLocalEvent<ApcPowerProviderComponent, ExtensionCableSystem.ReceiverConnectedEvent>(OnReceiverConnected);
            SubscribeLocalEvent<ApcPowerProviderComponent, ExtensionCableSystem.ReceiverDisconnectedEvent>(OnReceiverDisconnected);
            SubscribeLocalEvent<ApcPowerProviderComponent, PowerChangedEvent>(OnPowerChanged);
        }

        private void OnPowerChanged(EntityUid uid, ApcPowerProviderComponent component, PowerChangedEvent args)
        {
            if (EntityManager.TryGetComponent(uid, out WiresComponent? wires)
                && !args.Powered
                && _wiresSystem.TryGetData(uid, PowerWireActionKey.ElectrifiedCancel, out var electrifiedObject)
                && electrifiedObject is CancellationTokenSource electrifiedToken)
            {
                electrifiedToken.Cancel();
            }
        }

        ///<summary>
        ///Adds some markup to the examine text of whatever object is using this component to tell you if it's powered or not, even if it doesn't have an icon state to do this for you.
        ///</summary>
        private void OnExamined(EntityUid uid, ApcPowerReceiverComponent component, ExaminedEvent args)
        {
            args.PushMarkup(Loc.GetString("power-receiver-component-on-examine-main",
                                            ("stateText", Loc.GetString( component.Powered ? "power-receiver-component-on-examine-powered" :
                                                                                   "power-receiver-component-on-examine-unpowered"))));
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
