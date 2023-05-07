using System.Linq;
using System.Diagnostics.CodeAnalysis;
using Content.Server.MachineLinking.Components;
using Content.Server.MachineLinking.Events;
using Content.Server.Power.Components;
using Content.Server.Tools;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Interaction;
using Content.Shared.MachineLinking;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using SignalReceivedEvent = Content.Server.DeviceLinking.Events.SignalReceivedEvent;

namespace Content.Server.MachineLinking.System
{
    public sealed class SignalLinkerSystem : EntitySystem
    {
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly IPrototypeManager _protoMan = default!;
        [Dependency] private readonly ToolSystem _tools = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SignalTransmitterComponent, ComponentStartup>(OnTransmitterStartup);
            SubscribeLocalEvent<SignalTransmitterComponent, ComponentRemove>(OnTransmitterRemoved);
            SubscribeLocalEvent<SignalTransmitterComponent, InteractUsingEvent>(OnTransmitterInteractUsing);
            SubscribeLocalEvent<SignalTransmitterComponent, GetVerbsEvent<AlternativeVerb>>(OnGetTransmitterVerbs);

            SubscribeLocalEvent<SignalReceiverComponent, ComponentStartup>(OnReceiverStartup);
            SubscribeLocalEvent<SignalReceiverComponent, ComponentRemove>(OnReceiverRemoved);
            SubscribeLocalEvent<SignalReceiverComponent, InteractUsingEvent>(OnReceiverInteractUsing);
            SubscribeLocalEvent<SignalReceiverComponent, GetVerbsEvent<AlternativeVerb>>(OnGetReceiverVerbs);

            SubscribeLocalEvent<SignalLinkerComponent, SignalPortSelected>(OnSignalPortSelected);
            SubscribeLocalEvent<SignalLinkerComponent, LinkerClearSelected>(OnLinkerClearSelected);
            SubscribeLocalEvent<SignalLinkerComponent, LinkerLinkDefaultSelected>(OnLinkerLinkDefaultSelected);
            SubscribeLocalEvent<SignalLinkerComponent, BoundUIClosedEvent>(OnLinkerUIClosed);
        }

        /// <summary>
        ///     Convenience function to add several ports to an entity.
        /// </summary>
        public void EnsureReceiverPorts(EntityUid uid, params string[] ports)
        {
            var comp = EnsureComp<SignalReceiverComponent>(uid);
            foreach (var port in ports)
            {
                comp.Inputs.TryAdd(port, new List<PortIdentifier>());
            }
        }

        public void EnsureTransmitterPorts(EntityUid uid, params string[] ports)
        {
            var comp = EnsureComp<SignalTransmitterComponent>(uid);
            foreach (var port in ports)
            {
                comp.Outputs.TryAdd(port, new List<PortIdentifier>());
            }
        }

        /// <summary>
        ///     Add an alt-click verb to allow users to link the default ports, without needing to open the UI.
        /// </summary>
        private void OnGetReceiverVerbs(EntityUid uid, SignalReceiverComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            if (!TryComp(args.Using, out SignalLinkerComponent? linker) ||
                !IsLinkerInteractable(args.Using.Value, linker))
            {
                return;
            }

            var verb = new AlternativeVerb()
            {
                Text = Loc.GetString("signal-linking-verb-text-link-default"),
                IconEntity = args.Using
            };
            args.Verbs.Add(verb);

            if (linker.SavedTransmitter != null)
            {
                verb.Act = () =>
                {
                    var msg = TryLinkDefaults(uid, linker.SavedTransmitter.Value, args.User, component)
                        ? Loc.GetString("signal-linking-verb-success", ("machine", linker.SavedTransmitter.Value))
                        : Loc.GetString("signal-linking-verb-fail", ("machine", linker.SavedTransmitter.Value));
                    _popupSystem.PopupEntity(msg, uid, args.User);
                };
                return;
            }

            verb.Disabled = true;
            verb.Message = Loc.GetString("signal-linking-verb-disabled-no-transmitter");
        }

        private void OnGetTransmitterVerbs(EntityUid uid, SignalTransmitterComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            if (!TryComp(args.Using, out SignalLinkerComponent? linker)
                || !IsLinkerInteractable(args.Using.Value, linker))
                return;

            AlternativeVerb verb = new()
            {
                Text = Loc.GetString("signal-linking-verb-text-link-default"),
                IconEntity = args.Using
            };
            args.Verbs.Add(verb);

            if (linker.SavedReceiver != null)
            {
                verb.Act = () =>
                {
                    var msg = TryLinkDefaults(linker.SavedReceiver.Value, uid, args.User, null, component)
                        ? Loc.GetString("signal-linking-verb-success", ("machine", linker.SavedReceiver.Value))
                        : Loc.GetString("signal-linking-verb-fail", ("machine", linker.SavedReceiver.Value));
                    _popupSystem.PopupEntity(msg, uid, args.User);
                };
                return;
            }

            verb.Disabled = true;
            verb.Message = Loc.GetString("signal-linking-verb-disabled-no-receiver");
        }

        public void InvokePort(EntityUid uid, string port, SignalTransmitterComponent? component = null)
        {
            InvokePort(uid, port, SignalState.Momentary, component);
        }

        public void InvokePort(EntityUid uid, string port, SignalState state, SignalTransmitterComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            if (state != SignalState.Momentary && state == component.LastState)
            {
                // no change in output signal
                return;
            }

            if (!component.Outputs.TryGetValue(port, out var receivers))
                return;

            component.LastState = state;
            foreach (var receiver in receivers)
            {
                var eventArgs = new SignalReceivedEvent(receiver.Port, uid);
                RaiseLocalEvent(receiver.Uid, ref eventArgs);
            }
        }

        private void OnTransmitterStartup(EntityUid uid, SignalTransmitterComponent transmitter, ComponentStartup args)
        {
            // validate links
            Dictionary<EntityUid, SignalReceiverComponent?> uidCache = new();
            foreach (var tport in transmitter.Outputs)
            {
                foreach (var rport in tport.Value)
                {
                    if (!uidCache.TryGetValue(rport.Uid, out var receiver))
                        uidCache.Add(rport.Uid, receiver = CompOrNull<SignalReceiverComponent>(rport.Uid));
                    if (receiver == null || !receiver.Inputs.TryGetValue(rport.Port, out var rpv))
                        tport.Value.Remove(rport);
                    else if (!rpv.Contains(new(uid, tport.Key)))
                        rpv.Add(new(uid, tport.Key));
                }
            }
        }

        private void OnReceiverStartup(EntityUid uid, SignalReceiverComponent receiver, ComponentStartup args)
        {
            // validate links
            Dictionary<EntityUid, SignalTransmitterComponent?> uidCache = new();
            foreach (var rport in receiver.Inputs)
            {
                var toRemove = new List<PortIdentifier>();
                foreach (var tport in rport.Value)
                {
                    if (!uidCache.TryGetValue(tport.Uid, out var transmitter))
                        uidCache.Add(tport.Uid, transmitter = CompOrNull<SignalTransmitterComponent>(tport.Uid));
                    if (transmitter == null || !transmitter.Outputs.TryGetValue(tport.Port, out var tpv))
                        toRemove.Add(tport);
                    else if (!tpv.Contains(new(uid, rport.Key)))
                        tpv.Add(new(uid, rport.Key));
                }
                toRemove.ForEach(tport => rport.Value.Remove(tport));
            }
        }

        private void OnTransmitterRemoved(EntityUid uid, SignalTransmitterComponent transmitter, ComponentRemove args)
        {
            Dictionary<EntityUid, SignalReceiverComponent?> uidCache = new();
            foreach (var tport in transmitter.Outputs)
                foreach (var rport in tport.Value)
                {
                    if (!uidCache.TryGetValue(rport.Uid, out var receiver))
                        uidCache.Add(rport.Uid, receiver = CompOrNull<SignalReceiverComponent>(rport.Uid));
                    if (receiver != null && receiver.Inputs.TryGetValue(rport.Port, out var rpv))
                        rpv.Remove(new(uid, tport.Key));
                }
        }

        private void OnReceiverRemoved(EntityUid uid, SignalReceiverComponent component, ComponentRemove args)
        {
            Dictionary<EntityUid, SignalTransmitterComponent?> uidCache = new();
            foreach (var rport in component.Inputs)
                foreach (var tport in rport.Value)
                {
                    if (!uidCache.TryGetValue(tport.Uid, out var transmitter))
                        uidCache.Add(tport.Uid, transmitter = CompOrNull<SignalTransmitterComponent>(tport.Uid));
                    if (transmitter != null && transmitter.Outputs.TryGetValue(tport.Port, out var receivers))
                        receivers.Remove(new(uid, rport.Key));
                }
        }

        private void OnTransmitterInteractUsing(EntityUid uid, SignalTransmitterComponent transmitter, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            if (!TryComp(args.Used, out SignalLinkerComponent? linker) || !IsLinkerInteractable(args.Used, linker) ||
                !TryComp(args.User, out ActorComponent? actor))
                return;

            if (!linker.LinkTX())
                return;

            linker.SavedTransmitter = uid;

            if (!TryComp(linker.SavedReceiver, out SignalReceiverComponent? receiver))
            {
                _popupSystem.PopupCursor(Loc.GetString("signal-linker-component-saved", ("machine", uid)),
                    args.User, PopupType.Medium);
                args.Handled = true;
                return;
            }

            if (TryGetOrOpenUI(actor, linker, out var bui))
            {
                TryUpdateUI(linker, transmitter, receiver, bui);
                args.Handled = true;
            }
        }

        private void OnReceiverInteractUsing(EntityUid uid, SignalReceiverComponent receiver, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            if (!TryComp(args.Used, out SignalLinkerComponent? linker) || !IsLinkerInteractable(args.Used, linker) ||
                !TryComp(args.User, out ActorComponent? actor))
                return;

            if (!linker.LinkRX())
                return;

            linker.SavedReceiver = uid;

            if (!TryComp(linker.SavedTransmitter, out SignalTransmitterComponent? transmitter))
            {
                _popupSystem.PopupCursor(Loc.GetString("signal-linker-component-saved", ("machine", uid)),
                    args.User, PopupType.Medium);
                args.Handled = true;
                return;
            }

            if (TryGetOrOpenUI(actor, linker, out var bui))
            {
                TryUpdateUI(linker, transmitter, receiver, bui);
                args.Handled = true;
            }
        }

        private bool TryGetOrOpenUI(ActorComponent actor, SignalLinkerComponent linker, [NotNullWhen(true)] out BoundUserInterface? bui)
        {
            if (_userInterfaceSystem.TryGetUi(linker.Owner, SignalLinkerUiKey.Key, out bui))
            {
                bui.Open(actor.PlayerSession);
                return true;
            }
            return false;
        }

        private bool TryUpdateUI(SignalLinkerComponent linker, SignalTransmitterComponent transmitter, SignalReceiverComponent receiver, BoundUserInterface? bui = null)
        {
            if (bui == null && !_userInterfaceSystem.TryGetUi(linker.Owner, SignalLinkerUiKey.Key, out bui))
                return false;

            var outKeys = transmitter.Outputs.Keys.ToList();
            var inKeys = receiver.Inputs.Keys.ToList();
            List<(int, int)> links = new();
            for (var i = 0; i < outKeys.Count; i++)
            {
                foreach (var re in transmitter.Outputs[outKeys[i]])
                {
                    if (re.Uid == receiver.Owner)
                        links.Add((i, inKeys.IndexOf(re.Port)));
                }
            }

            bui.SetState(new SignalPortsState($"{Name(transmitter.Owner)} ({transmitter.Owner})", outKeys,
                $"{Name(receiver.Owner)} ({receiver.Owner})", inKeys, links));
            return true;

        }

        private bool TryLink(SignalTransmitterComponent transmitter, SignalReceiverComponent receiver, SignalPortSelected args, EntityUid? user, bool quiet = false, bool checkRange = true)
        {
            if (!transmitter.Outputs.TryGetValue(args.TransmitterPort, out var linkedReceivers) ||
                !receiver.Inputs.TryGetValue(args.ReceiverPort, out var linkedTransmitters))
            {
                return false;
            }

            quiet |= !user.HasValue;

            // Does the link already exist? Under the assumption that nothing has broken, lets only check the
            // transmitter ports.
            foreach (var identifier in linkedTransmitters)
            {
                if (identifier.Uid == transmitter.Owner && identifier.Port == args.TransmitterPort)
                    return true;
            }

            if (checkRange && !IsInRange(transmitter, receiver))
            {
                if (!quiet)
                    _popupSystem.PopupCursor(Loc.GetString("signal-linker-component-out-of-range"), user!.Value);
                return false;
            }

            // allow other systems to refuse the connection
            var linkAttempt = new LinkAttemptEvent(user, transmitter.Owner, args.TransmitterPort, receiver.Owner, args.ReceiverPort);
            RaiseLocalEvent(transmitter.Owner, linkAttempt, true);
            if (linkAttempt.Cancelled)
            {
                if (!quiet)
                    _popupSystem.PopupCursor(Loc.GetString("signal-linker-component-connection-refused", ("machine", transmitter.Owner)), user!.Value);
                return false;
            }
            RaiseLocalEvent(receiver.Owner, linkAttempt, true);
            if (linkAttempt.Cancelled)
            {
                if (!quiet)
                    _popupSystem.PopupCursor(Loc.GetString("signal-linker-component-connection-refused", ("machine", receiver.Owner)), user!.Value);
                return false;
            }

            linkedReceivers.Add(new(receiver.Owner, args.ReceiverPort));
            linkedTransmitters.Add(new(transmitter.Owner, args.TransmitterPort));
            if (!quiet)
            {
                _popupSystem.PopupCursor(Loc.GetString("signal-linker-component-linked-port",
                        ("machine1", transmitter.Owner), ("port1", PortName<TransmitterPortPrototype>(args.TransmitterPort)),
                        ("machine2", receiver.Owner), ("port2", PortName<ReceiverPortPrototype>(args.ReceiverPort))),
                    user!.Value, PopupType.Medium);
            }

            var newLink = new NewLinkEvent(user, transmitter.Owner, args.TransmitterPort, receiver.Owner, args.ReceiverPort);
            RaiseLocalEvent(receiver.Owner, newLink);
            RaiseLocalEvent(transmitter.Owner, newLink);

            return true;
        }

        private void OnSignalPortSelected(EntityUid uid, SignalLinkerComponent linker, SignalPortSelected args)
        {
            if (!TryComp(linker.SavedTransmitter, out SignalTransmitterComponent? transmitter) ||
                !TryComp(linker.SavedReceiver, out SignalReceiverComponent? receiver) ||
                !transmitter.Outputs.TryGetValue(args.TransmitterPort, out var receivers) ||
                !receiver.Inputs.TryGetValue(args.ReceiverPort, out var transmitters))
                return;

            if (args.Session.AttachedEntity is not { Valid: true} attached)
                return;

            if (receivers.Contains(new(receiver.Owner, args.ReceiverPort)) ||
                transmitters.Contains(new(transmitter.Owner, args.TransmitterPort)))
            {
                // link already exists, remove it
                if (receivers.Remove(new(receiver.Owner, args.ReceiverPort)) &&
                    transmitters.Remove(new(transmitter.Owner, args.TransmitterPort)))
                {
                    RaiseLocalEvent(receiver.Owner, new PortDisconnectedEvent(args.ReceiverPort), true);
                    RaiseLocalEvent(transmitter.Owner, new PortDisconnectedEvent(args.TransmitterPort), true);

                    _popupSystem.PopupCursor(Loc.GetString("signal-linker-component-unlinked-port",
                        ("machine1", transmitter.Owner), ("port1", PortName<TransmitterPortPrototype>(args.TransmitterPort)),
                        ("machine2", receiver.Owner), ("port2", PortName<ReceiverPortPrototype>(args.ReceiverPort))),
                        attached, PopupType.Medium);
                }
                else
                { // something weird happened
                  // TODO log error
                }
            }
            else
            {
                TryLink(transmitter, receiver, args, attached);
            }

            TryUpdateUI(linker, transmitter, receiver);
        }

        /// <summary>
        ///     Convenience function to retrieve the name of a port prototype.
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public string PortName<TPort>(string port) where TPort : MachinePortPrototype, IPrototype
        {
            if (!_protoMan.TryIndex<TPort>(port, out var proto))
                return port;

            return Loc.GetString(proto.Name);
        }

        private void OnLinkerClearSelected(EntityUid uid, SignalLinkerComponent linker, LinkerClearSelected args)
        {
            if (!TryComp(linker.SavedTransmitter, out SignalTransmitterComponent? transmitter) ||
                !TryComp(linker.SavedReceiver, out SignalReceiverComponent? receiver))
                return;

            foreach (var (port, receivers) in transmitter.Outputs)
            {
                if (receivers.RemoveAll(id => id.Uid == receiver.Owner) > 0)
                    RaiseLocalEvent(transmitter.Owner, new PortDisconnectedEvent(port), true);
            }

            foreach (var (port, transmitters) in receiver.Inputs)
            {
                if (transmitters.RemoveAll(id => id.Uid == transmitter.Owner) > 0)
                    RaiseLocalEvent(receiver.Owner, new PortDisconnectedEvent(port), true);
            }

            TryUpdateUI(linker, transmitter, receiver);
        }

        private void OnLinkerLinkDefaultSelected(EntityUid uid, SignalLinkerComponent linker, LinkerLinkDefaultSelected args)
        {
            if (!TryComp(linker.SavedTransmitter, out SignalTransmitterComponent? transmitter) ||
                !TryComp(linker.SavedReceiver, out SignalReceiverComponent? receiver))
                return;

            if (args.Session.AttachedEntity is not { Valid: true } user)
                return;

            TryLinkDefaults(linker.SavedReceiver!.Value, linker.SavedTransmitter!.Value, user, receiver, transmitter);
            TryUpdateUI(linker, transmitter, receiver);
        }

        /// <summary>
        ///     Attempt to link all default ports connections. Returns true if all links succeeded. Otherwise returns
        ///     false.
        /// </summary>
        public bool TryLinkDefaults(EntityUid receiverUid, EntityUid transmitterUid, EntityUid? user,
            SignalReceiverComponent? receiver = null, SignalTransmitterComponent? transmitter = null)
        {
            if (!Resolve(receiverUid, ref receiver, false) || !Resolve(transmitterUid, ref transmitter, false))
                return false;

            if (!IsInRange(transmitter, receiver))
                return false;

            var allLinksSucceeded = true;

            // First, disconnect existing links.
            foreach (var (port, receivers) in transmitter.Outputs)
            {
                if (receivers.RemoveAll(id => id.Uid == receiver.Owner) > 0)
                    RaiseLocalEvent(transmitter.Owner, new PortDisconnectedEvent(port), true);
            }

            foreach (var (port, transmitters) in receiver.Inputs)
            {
                if (transmitters.RemoveAll(id => id.Uid == transmitter.Owner) > 0)
                    RaiseLocalEvent(receiver.Owner, new PortDisconnectedEvent(port), true);
            }

            // Then make any valid default connections.
            foreach (var outPort in transmitter.Outputs.Keys)
            {
                var prototype = _protoMan.Index<TransmitterPortPrototype>(outPort);
                if (prototype.DefaultLinks == null)
                    continue;

                foreach (var inPort in prototype.DefaultLinks)
                {
                    if (receiver.Inputs.ContainsKey(inPort))
                        allLinksSucceeded &= TryLink(transmitter, receiver, new(outPort, inPort), user, quiet: true, checkRange: false);
                }
            }

            return allLinksSucceeded;
        }

        private void OnLinkerUIClosed(EntityUid uid, SignalLinkerComponent component, BoundUIClosedEvent args)
        {
            component.SavedTransmitter = null;
            component.SavedReceiver = null;
        }

        private bool IsInRange(SignalTransmitterComponent transmitterComponent, SignalReceiverComponent receiverComponent)
        {
            if (TryComp(transmitterComponent.Owner, out ApcPowerReceiverComponent? transmitterPower) &&
                TryComp(receiverComponent.Owner, out ApcPowerReceiverComponent? receiverPower) &&
                transmitterPower.Provider?.Net == receiverPower.Provider?.Net)
                return true;

            // TODO: As elsewhere don't use mappos inrange.
            return Comp<TransformComponent>(transmitterComponent.Owner).MapPosition.InRange(
                   Comp<TransformComponent>(receiverComponent.Owner).MapPosition, transmitterComponent.TransmissionRange);
        }

        private bool IsLinkerInteractable(EntityUid uid, SignalLinkerComponent linkerComponent)
        {
            var quality = linkerComponent.RequiredQuality;
            return quality == null || _tools.HasQuality(uid, quality);
        }
    }
}
