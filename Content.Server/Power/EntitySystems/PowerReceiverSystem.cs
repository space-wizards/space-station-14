using System.Diagnostics.CodeAnalysis;
using Content.Server.Administration.Managers;
using Content.Server.Power.Components;
using Content.Shared.Administration;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Verbs;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Server.Power.EntitySystems
{
    public sealed class PowerReceiverSystem : SharedPowerReceiverSystem
    {
        [Dependency] private readonly IAdminManager _adminManager = default!;

        private EntityQuery<ApcPowerReceiverComponent> _recQuery;
        private EntityQuery<ApcPowerProviderComponent> _provQuery;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ApcPowerReceiverComponent, ExaminedEvent>(OnExamined);

            SubscribeLocalEvent<ApcPowerReceiverComponent, ExtensionCableSystem.ProviderConnectedEvent>(OnProviderConnected);
            SubscribeLocalEvent<ApcPowerReceiverComponent, ExtensionCableSystem.ProviderDisconnectedEvent>(OnProviderDisconnected);

            SubscribeLocalEvent<ApcPowerProviderComponent, ComponentShutdown>(OnProviderShutdown);
            SubscribeLocalEvent<ApcPowerProviderComponent, ExtensionCableSystem.ReceiverConnectedEvent>(OnReceiverConnected);
            SubscribeLocalEvent<ApcPowerProviderComponent, ExtensionCableSystem.ReceiverDisconnectedEvent>(OnReceiverDisconnected);

            SubscribeLocalEvent<ApcPowerReceiverComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
            SubscribeLocalEvent<PowerSwitchComponent, GetVerbsEvent<AlternativeVerb>>(AddSwitchPowerVerb);

            SubscribeLocalEvent<ApcPowerReceiverComponent, ComponentGetState>(OnGetState);

            _recQuery = GetEntityQuery<ApcPowerReceiverComponent>();
            _provQuery = GetEntityQuery<ApcPowerProviderComponent>();
        }

        private void OnExamined(Entity<ApcPowerReceiverComponent> ent, ref ExaminedEvent args)
        {
            args.PushMarkup(GetExamineText(ent.Comp.Powered));
        }

        private void OnGetVerbs(EntityUid uid, ApcPowerReceiverComponent component, GetVerbsEvent<Verb> args)
        {
            if (!_adminManager.HasAdminFlag(args.User, AdminFlags.Admin))
                return;

            // add debug verb to toggle power requirements
            args.Verbs.Add(new()
            {
                Text = Loc.GetString("verb-debug-toggle-need-power"),
                Category = VerbCategory.Debug,
                Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/smite.svg.192dpi.png")), // "smite" is a lightning bolt
                Act = () =>
                {
                    SetNeedsPower(uid, !component.NeedsPower, component);
                }
            });
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

        private void OnProviderConnected(Entity<ApcPowerReceiverComponent> receiver, ref ExtensionCableSystem.ProviderConnectedEvent args)
        {
            var providerUid = args.Provider.Owner;
            if (!_provQuery.TryGetComponent(providerUid, out var provider))
                return;

            receiver.Comp.Provider = provider;

            ProviderChanged(receiver);
        }

        private void OnProviderDisconnected(Entity<ApcPowerReceiverComponent> receiver, ref ExtensionCableSystem.ProviderDisconnectedEvent args)
        {
            receiver.Comp.Provider = null;

            ProviderChanged(receiver);
        }

        private void OnReceiverConnected(Entity<ApcPowerProviderComponent> provider, ref ExtensionCableSystem.ReceiverConnectedEvent args)
        {
            if (_recQuery.TryGetComponent(args.Receiver, out var receiver))
            {
                provider.Comp.AddReceiver(receiver);
            }
        }

        private void OnReceiverDisconnected(EntityUid uid, ApcPowerProviderComponent provider, ExtensionCableSystem.ReceiverDisconnectedEvent args)
        {
            if (_recQuery.TryGetComponent(args.Receiver, out var receiver))
            {
                provider.RemoveReceiver(receiver);
            }
        }

        private void AddSwitchPowerVerb(EntityUid uid, PowerSwitchComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if(!args.CanAccess || !args.CanInteract)
                return;

            if (!HasComp<HandsComponent>(args.User))
                return;

            if (!_recQuery.TryGetComponent(uid, out var receiver))
                return;

            if (!receiver.NeedsPower)
                return;

            AlternativeVerb verb = new()
            {
                Act = () =>
                {
                    TogglePower(uid, user: args.User);
                },
                Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/Spare/poweronoff.svg.192dpi.png")),
                Text = Loc.GetString("power-switch-component-toggle-verb"),
                Priority = -3
            };
            args.Verbs.Add(verb);
        }

        private void OnGetState(EntityUid uid, ApcPowerReceiverComponent component, ref ComponentGetState args)
        {
            args.State = new ApcPowerReceiverComponentState
            {
                Powered = component.Powered,
                NeedsPower = component.NeedsPower,
                PowerDisabled = component.PowerDisabled,
            };
        }

        private void ProviderChanged(Entity<ApcPowerReceiverComponent> receiver)
        {
            var comp = receiver.Comp;
            comp.NetworkLoad.LinkedNetwork = default;
        }

        /// <summary>
        /// If this takes power, it returns whether it has power.
        /// Otherwise, it returns 'true' because if something doesn't take power
        /// it's effectively always powered.
        /// </summary>
        /// <returns>True when entity has no ApcPowerReceiverComponent or is Powered. False when not.</returns>
        public bool IsPowered(EntityUid uid, ApcPowerReceiverComponent? receiver = null)
        {
            return !_recQuery.Resolve(uid, ref receiver, false) || receiver.Powered;
        }

        public void SetLoad(ApcPowerReceiverComponent comp, float load)
        {
            comp.Load = load;
        }

        public override bool ResolveApc(EntityUid entity, [NotNullWhen(true)] ref SharedApcPowerReceiverComponent? component)
        {
            if (component != null)
                return true;

            if (!TryComp(entity, out ApcPowerReceiverComponent? receiver))
                return false;

            component = receiver;
            return true;
        }
    }
}
