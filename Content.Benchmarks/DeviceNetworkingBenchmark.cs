using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Content.IntegrationTests;
using Content.IntegrationTests.Tests.DeviceNetwork;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Systems;
using Robust.Shared;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Benchmarks;

[Virtual]
[MemoryDiagnoser]
public class DeviceNetworkingBenchmark
{
    private PairTracker _pair = default!;
    private DeviceNetworkTestSystem _deviceNetTestSystem = default!;
    private DeviceNetworkSystem _deviceNetworkSystem = default!;
    private EntityUid _sourceEntity;
    private EntityUid _sourceWirelessEntity;
    private List<EntityUid> _targetEntities = new();
    private List<EntityUid> _targetWirelessEntities = new();


    private NetworkPayload _payload = default!;
    private const string Prototypes = @"
- type: entity
  name: DummyNetworkDevice
  id: DummyNetworkDevice
  components:
    - type: DeviceNetwork
      transmitFrequency: 100
      receiveFrequency: 100
      deviceNetId: Private
- type: entity
  name: DummyWirelessNetworkDevice
  id: DummyWirelessNetworkDevice
  components:
    - type: DeviceNetwork
      transmitFrequency: 100
      receiveFrequency: 100
      deviceNetId: Wireless
    - type: WirelessNetworkConnection
      range: 100
        ";

    //public static IEnumerable<int> EntityCountSource { get; set; }

    //[ParamsSource(nameof(EntityCountSource))]
    public int EntityCount = 500;

    [GlobalSetup]
    public async Task SetupAsync()
    {
        ProgramShared.PathOffset = "../../../../";
        _pair = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
        var server = _pair.Pair.Server;

        await server.WaitPost(() =>
        {
            var entityManager = server.InstanceDependencyCollection.Resolve<IEntityManager>();
            _deviceNetworkSystem = entityManager.EntitySysManager.GetEntitySystem<DeviceNetworkSystem>();
            _deviceNetTestSystem = entityManager.EntitySysManager.GetEntitySystem<DeviceNetworkTestSystem>();

            var testValue = "test";
            _payload = new NetworkPayload
            {
                ["Test"] = testValue,
                ["testnumber"] = 1,
                ["testbool"] = true
            };

            _sourceEntity = entityManager.SpawnEntity("DummyNetworkDevice", MapCoordinates.Nullspace);
            _sourceWirelessEntity = entityManager.SpawnEntity("DummyWirelessNetworkDevice", MapCoordinates.Nullspace);

            for (var i = 0; i < EntityCount; i++)
            {
                _targetEntities.Add(entityManager.SpawnEntity("DummyNetworkDevice", MapCoordinates.Nullspace));
                _targetWirelessEntities.Add(entityManager.SpawnEntity("DummyWirelessNetworkDevice", MapCoordinates.Nullspace));
            }
        });
    }

    [Benchmark(Baseline = true, Description = "Entity Events")]
    public async Task EventSentBaseline()
    {
        var server = _pair.Pair.Server;

        _pair.Pair.Server.Post(() =>
        {
            foreach (var entity in _targetEntities)
            {
                _deviceNetTestSystem.SendBaselineTestEvent(entity);
            }
        });

        await server.WaitRunTicks(1);
        await server.WaitIdleAsync();
    }

    [Benchmark(Description = "Device Net Broadcast No Connection Checks")]
    public async Task DeviceNetworkBroadcastNoConnectionChecks()
    {
        var server = _pair.Pair.Server;

        _pair.Pair.Server.Post(() =>
        {
            _deviceNetworkSystem.QueuePacket(_sourceEntity, null, _payload, 100);
        });

        await server.WaitRunTicks(1);
        await server.WaitIdleAsync();
    }

    [Benchmark(Description = "Device Net Broadcast Wireless Connection Checks")]
    public async Task DeviceNetworkBroadcastWirelessConnectionChecks()
    {
        var server = _pair.Pair.Server;

        _pair.Pair.Server.Post(() =>
        {
            _deviceNetworkSystem.QueuePacket(_sourceWirelessEntity, null, _payload, 100);
        });

        await server.WaitRunTicks(1);
        await server.WaitIdleAsync();
    }
}
