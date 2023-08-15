using Content.Server.DeviceNetwork.Components;
using Content.Shared.SensorMonitoring;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Collections;
using ConsoleUIState = Content.Shared.SensorMonitoring.SensorMonitoringConsoleBoundInterfaceState;
using IncrementalUIState = Content.Shared.SensorMonitoring.SensorMonitoringIncrementalUpdate;

namespace Content.Server.SensorMonitoring;

public sealed partial class SensorMonitoringConsoleSystem
{
    private void InitUI()
    {
        SubscribeLocalEvent<SensorMonitoringConsoleComponent, BoundUIClosedEvent>(ConsoleUIClosed);
    }

    private void UpdateConsoleUI(EntityUid uid, SensorMonitoringConsoleComponent comp)
    {
        if (!_userInterface.TryGetUi(uid, SensorMonitoringConsoleUiKey.Key, out var ui))
            return;

        if (ui.SubscribedSessions.Count == 0)
            return;

        ConsoleUIState? fullState = null;
        SensorMonitoringIncrementalUpdate? incrementalUpdate = null;

        foreach (var session in ui.SubscribedSessions)
        {
            if (comp.InitialUIStateSent.Contains(session))
            {
                incrementalUpdate ??= CalculateIncrementalUpdate();
                _userInterface.TrySendUiMessage(ui, incrementalUpdate, session);
            }
            else
            {
                fullState ??= CalculateFullState();
                _userInterface.SetUiState(ui, fullState, session);
                comp.InitialUIStateSent.Add(session);
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

        if (args.Session is not IPlayerSession player)
            return;

        component.InitialUIStateSent.Remove(player);
    }
}
