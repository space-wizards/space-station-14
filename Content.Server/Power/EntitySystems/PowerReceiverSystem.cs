using Content.Server.Power.Components;
using Content.Shared.Examine;
using Content.Shared.Power;
using Robust.Server.GameObjects;

namespace Content.Server.Power.EntitySystems
{
    public sealed class PowerReceiverSystem : EntitySystem
    {
        [Dependency] private readonly AppearanceSystem _appearance = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ApcPowerReceiverComponent, ExaminedEvent>(OnExamined);

            SubscribeLocalEvent<ApcPowerReceiverComponent, ExtensionCableSystem.ProviderConnectedEvent>(OnProviderConnected);
            SubscribeLocalEvent<ApcPowerReceiverComponent, ExtensionCableSystem.ProviderDisconnectedEvent>(OnProviderDisconnected);

            SubscribeLocalEvent<ApcPowerProviderComponent, ComponentShutdown>(OnProviderShutdown);
            SubscribeLocalEvent<ApcPowerProviderComponent, ExtensionCableSystem.ReceiverConnectedEvent>(OnReceiverConnected);
            SubscribeLocalEvent<ApcPowerProviderComponent, ExtensionCableSystem.ReceiverDisconnectedEvent>(OnReceiverDisconnected);
        }

        ///<summary>
        ///Adds some markup to the examine text of whatever object is using this component to tell you if it's powered or not, even if it doesn't have an icon state to do this for you.
        ///</summary>
        private void OnExamined(EntityUid uid, ApcPowerReceiverComponent component, ExaminedEvent args)
        {
            args.PushMarkup(Loc.GetString("power-receiver-component-on-examine-main",
                                            ("stateText", Loc.GetString( component.Powered
                                                ? "power-receiver-component-on-examine-powered"
                                                : "power-receiver-component-on-examine-unpowered"))));
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
            if (EntityManager.TryGetComponent(args.Receiver.Owner, out ApcPowerReceiverComponent? receiver))
            {
                provider.AddReceiver(receiver);
            }
        }

        private void OnReceiverDisconnected(EntityUid uid, ApcPowerProviderComponent provider, ExtensionCableSystem.ReceiverDisconnectedEvent args)
        {
            if (EntityManager.TryGetComponent(args.Receiver.Owner, out ApcPowerReceiverComponent? receiver))
            {
                provider.RemoveReceiver(receiver);
            }
        }

        private void ProviderChanged(ApcPowerReceiverComponent receiver)
        {
            receiver.NetworkLoad.LinkedNetwork = default;
            var ev = new PowerChangedEvent(receiver.Powered, receiver.NetworkLoad.ReceivingPower);

            RaiseLocalEvent(receiver.Owner, ref ev);
            _appearance.SetData(receiver.Owner, PowerDeviceVisuals.Powered, receiver.Powered);
        }

        /// <summary>
        /// If this takes power, it returns whether it has power.
        /// Otherwise, it returns 'true' because if something doesn't take power
        /// it's effectively always powered.
        /// </summary>
        public bool IsPowered(EntityUid uid, ApcPowerReceiverComponent? receiver = null)
        {
            if (!Resolve(uid, ref receiver, false))
                return true;

            return receiver.Powered;
        }
    }
}
