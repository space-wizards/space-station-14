using Content.Shared.SensorMonitoring;
using Robust.Shared.Collections;
using ConsoleUIState = Content.Shared.SensorMonitoring.SensorMonitoringConsoleBoundInterfaceState;
using Content.Shared.DeviceNetwork.Components;
using IncrementalUIState = Content.Shared.SensorMonitoring.SensorMonitoringIncrementalUpdate;

namespace Content.Server.SensorMonitoring;

public sealed partial class SensorMonitoringConsoleSystem
{
    private void InitUI()
    {
        Subs.BuiEvents<SensorMonitoringConsoleComponent>(SensorMonitoringConsoleUiKey.Key, subs =>
        {
            subs.Event<BoundUIClosedEvent>(ConsoleUIClosed);
        });
    }

    private void UpdateConsoleUI(EntityUid uid, SensorMonitoringConsoleComponent comp)
    {
        if (!_userInterface.IsUiOpen(uid, SensorMonitoringConsoleUiKey.Key))
        {
            return;
        }

        ConsoleUIState? fullState = null;
        SensorMonitoringIncrementalUpdate? incrementalUpdate = null;

        foreach (var actorUid in _userInterface.GetActors(uid, SensorMonitoringConsoleUiKey.Key))
        {
            if (comp.InitialUIStateSent.Contains(actorUid))
            {
                incrementalUpdate ??= CalculateIncrementalUpdate();
                _userInterface.ServerSendUiMessage(uid, SensorMonitoringConsoleUiKey.Key, incrementalUpdate, actorUid);
            }
            else
            {
                fullState ??= CalculateFullState();
                _userInterface.SetUiState(uid, SensorMonitoringConsoleUiKey.Key, fullState);
                comp.InitialUIStateSent.Add(actorUid);
            }
        }

        comp.LastUIUpdate = _gameTiming.CurTime;
        comp.RemovedSensors.Clear();

        ConsoleUIState CalculateFullState()
        {
            var sensors = new ValueList<ConsoleUIState.SensorData>();
            var streams = new ValueList<ConsoleUIState.SensorStream>();

            foreach (var (ent, data) in comp.Sensors)
            {
                streams.Clear();
                var name = MetaData(ent).EntityName;
                var address = Comp<DeviceNetworkComponent>(ent).Address;

                foreach (var (streamName, stream) in data.Streams)
                {
                    streams.Add(new ConsoleUIState.SensorStream
                    {
                        NetId = stream.NetId,
                        Name = streamName,
                        Unit = stream.Unit,
                        Samples = stream.Samples.ToArray()
                    });
                }

                sensors.Add(new ConsoleUIState.SensorData
                {
                    NetId = data.NetId,
                    Name = name,
                    Address = address,
                    DeviceType = data.DeviceType,
                    Streams = streams.ToArray()
                });
            }

            return new ConsoleUIState
            {
                RetentionTime = comp.RetentionTime,
                Sensors = sensors.ToArray()
            };
        }

        SensorMonitoringIncrementalUpdate CalculateIncrementalUpdate()
        {
            var sensors = new ValueList<IncrementalUIState.SensorData>();
            var streams = new ValueList<IncrementalUIState.SensorStream>();
            var samples = new ValueList<SensorSample>();

            foreach (var data in comp.Sensors.Values)
            {
                streams.Clear();

                foreach (var stream in data.Streams.Values)
                {
                    samples.Clear();
                    foreach (var (sampleTime, value) in stream.Samples)
                    {
                        if (sampleTime >= comp.LastUIUpdate)
                            samples.Add(new SensorSample(sampleTime - comp.LastUIUpdate, value));
                    }

                    streams.Add(new IncrementalUIState.SensorStream
                    {
                        NetId = stream.NetId,
                        Unit = stream.Unit,
                        Samples = samples.ToArray()
                    });
                }

                sensors.Add(new IncrementalUIState.SensorData { NetId = data.NetId, Streams = streams.ToArray() });
            }

            return new IncrementalUIState
            {
                RelTime = comp.LastUIUpdate,
                RemovedSensors = comp.RemovedSensors.ToArray(),
                Sensors = sensors.ToArray(),
            };
        }
    }

    private static void ConsoleUIClosed(
        EntityUid uid,
        SensorMonitoringConsoleComponent component,
        BoundUIClosedEvent args)
    {
        if (!args.UiKey.Equals(SensorMonitoringConsoleUiKey.Key))
            return;

        component.InitialUIStateSent.Remove(args.Actor);
    }
}
