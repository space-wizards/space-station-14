using System.Linq;
using Content.Server.Construction.Components;
using Content.Server.Radio.Components;
using Content.Server.Radio.Components.Telecomms;

namespace Content.Server.Radio.EntitySystems;

// the totaly real telecomms simulator™
public sealed partial class RadioSystem
{
    #region Telecomms Machine Utils

    private void OnTelecommsMachineInit(EntityUid uid, TelecommsMachine component, ComponentInit args)
    {
        AddMachine(component.Network, uid);
    }

    private void OnTelecommsMachineDelete(EntityUid uid, TelecommsMachine component, ComponentShutdown args)
    {
        RemoveMachine(component.Network, uid);
    }

    /// <summary>
    /// For completeness.
    /// </summary>
    /// <param name="frameTime"></param>
    private void TelecommsMachineUpdate(float frameTime)
    {
        foreach (var machine in EntityManager.EntityQuery<TelecommsMachine>(true))
        {
            if (machine.Traffic <= 0) continue;
            machine.Traffic -= machine.Netspeed;
        }
    }

    private void AddMachine(string network, EntityUid theMachine)
    {
        if (_tcommsMachine.TryGetValue(network, out var value))
        {
            value.Add(theMachine);
            return;
        }
        _tcommsMachine.Add(network, new List<EntityUid>() { theMachine });
    }

    private void RemoveMachine(string network, EntityUid theMachine)
    {
        if (_tcommsMachine.TryGetValue(network, out var value))
        {
            value.Remove(theMachine);
            return;
        }
        // if the network doesnt exist then ???? what hte fuck
    }

    private void SwapNetwork(string oldnet, string newnet, EntityUid theMachine)
    {
        RemoveMachine(oldnet, theMachine);
        AddMachine(newnet, theMachine);
    }

    #endregion

    /// <summary>
    /// Sends to all telecomms machine ignoring the network.
    /// </summary>
    /// <typeparam name="T">Type of the telecomms machine</typeparam>
    /// <param name="packet"></param>
    /// <param name="lastSender"></param>
    /// <param name="maxiter">Limits each network send to this much.</param>
    /// <returns></returns>
    private int SendToAllMachines<T>(MessagePacket packet, EntityUid? lastSender = null, int maxiter = 20) where T : class, IComponent
    {
        return _tcommsMachine.Keys.Sum(item => SendToMachines<T>(item, packet, lastSender, maxiter));
    }

    /// <summary>
    /// Send a packet to each machine within a network.
    /// </summary>
    /// <typeparam name="T">Type of the telecomms machine</typeparam>
    /// <param name="network">The selected network</param>
    /// <param name="packet"></param>
    /// <param name="lastSender"></param>
    /// <param name="maxiter">Limits each network send to this much.</param>
    /// <returns></returns>
    private int SendToMachines<T>(string network, MessagePacket packet, EntityUid? lastSender = null, int maxiter = 20) where T : class, IComponent
    {
        var i = 0;
        if (!_tcommsMachine.TryGetValue(network, out var list))
        {
            return i;
        }
        foreach (var euid in list)
        {
            if (i >= maxiter)
            {
                break;
            }
            var isTheTypeWeWant = CompOrNull<T>(euid);
            if (isTheTypeWeWant == null)
            {
                continue;
            }
            ProcessMessage(euid, packet, lastSender);
            i++;
        }
        return i;
    }

    /// <summary>
    /// Emulates the telecomms messaging system.
    /// </summary>
    private void ProcessMessage(EntityUid target, MessagePacket messagePacket, EntityUid? lastSender = null)
    {
        if (messagePacket.Channel == null)
        {
            return; // fuck you
        }

        var channel = (int) messagePacket.Channel;
        var freqFilter = CompOrNull<ITelecommsFrequencyFilter>(target);
        var theMachine = CompOrNull<TelecommsMachine>(target);
        if (freqFilter != null && !freqFilter.IsFrequencyListening(channel) || theMachine is not
            {
                CanRun: true
            } || messagePacket.IsFinished)
        {
            return;
        }
        var frequencyChanger = CompOrNull<ITelecommsFrequencyChanger>(target);
        if (frequencyChanger is { FrequencyToChange: { } })
        {
            // change freq. Only the bus and allinone can only do this
            messagePacket.Channel = frequencyChanger.FrequencyToChange.Value;
        }

        // add traffic, it has just received this packet
        theMachine.Traffic++;

        // if it has a logger cmp log it
        var loggerCmp = CompOrNull<ITelecommsLogger>(target);
        loggerCmp?.TelecommsLog.Add(new TelecommsLog()
        {
            Message = messagePacket.Message,
            Speaker = _entMan.GetComponent<MetaDataComponent>(messagePacket.Speaker).EntityName,
            Frequency = (int)messagePacket.Channel
        });

        // specific all in one component
        if (HasComp<TelecommsAllInOneComponent>(target))
        {
            messagePacket.IsFinished = true;
            BroadcastMessage(messagePacket);
            return;
        }

        /*
        var processorCMP = CompOrNull<TelecommsProcessorComponent>(target);
        if (processorCMP != null)
        {
            // its a processor
            messagePacket.Compression = 0;
            if (lastSender.HasValue)
            {
                var senderBusMaybe = CompOrNull<TelecommsBusComponent>(lastSender);
                if (senderBusMaybe != null)
                {
                    ProcessMessage(lastSender.Value, messagePacket, target);
                    return;
                }
                // no bus detected - send the signal to servers instead
                SendToMachines<ITelecommsLogger>(theMachine.Network, messagePacket, target);
            }
        }

        var busCMP = CompOrNull<TelecommsBusComponent>(target);
        if (busCMP != null)
        {
            // its a bus
            // Signal must be ready (stupid assuming machine), let's send it
            var processorSenderMaybe = CompOrNull<TelecommsProcessorComponent>(target);
            if (processorSenderMaybe != null)
            {
                if (SendToMachines<TelecommsProcessorComponent>(theMachine.Network, messagePacket, target) > 0)
                {
                    return; // they accepted it so this msg is bad
                }
            }

            if (SendToMachines<ITelecommsLogger>(theMachine.Network, messagePacket, target) > 0)
            {
                // server exists? we route it there.
                return;
            }
            if (SendToMachines<TelecommsHubComponent>(theMachine.Network, messagePacket, target) > 0)
            {
                // Bus exists? relay it there then
                return;
            }
            // None? direct to broadcaster i guess sadge
            SendToMachines<TelecommsBroadcasterComponent>(theMachine.Network, messagePacket, target);
        }

        var hubCMP = CompOrNull<TelecommsHubComponent>(target);
        if (hubCMP != null)
        {
            if (lastSender != null)
            {
                var isReceiver = CompOrNull<TelecommsReceiverComponent>(lastSender);
                if (isReceiver != null) {
                    SendToMachines<TelecommsBusComponent>(theMachine.Network, messagePacket, target);
                    return;
                }
            }
            // Send it to each relay (TO BE IMPLMENTED) so their levels get added...
            //SendToMachines<TelecommsBus>(theMachine.Network, messagePacket, target);
            // Then broadcast that signal to
            SendToMachines<TelecommsBroadcasterComponent>(theMachine.Network, messagePacket, target);
        }

        var receiverCMP = CompOrNull<TelecommsReceiverComponent>(target);
        if (receiverCMP != null)
        {
            // send the signal to the hub if possible, or a bus otherwise
            if (SendToMachines<TelecommsHubComponent>(theMachine.Network, messagePacket, target) > 0)
            {
                return;
            }
            SendToMachines<TelecommsBroadcasterComponent>(theMachine.Network, messagePacket, target);
        }

        var broadcasterCMP = CompOrNull<TelecommsBroadcasterComponent>(target);
        if (broadcasterCMP != null)
        {
            messagePacket.MarkDone();
            Get<TelecommsMachineVisualizerSystem>().DoTransmitFlick(target, theMachine);
            SendToRadios(messagePacket.Source, messagePacket.Speaker, messagePacket.Message, messagePacket.Channel);
        }
        */
    }
}
