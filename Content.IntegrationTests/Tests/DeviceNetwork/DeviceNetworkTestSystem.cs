using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Events;
using Robust.Shared.GameObjects;
using Robust.Shared.Reflection;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Systems;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.IntegrationTests.Tests.DeviceNetwork;

[Reflect(false)]
public sealed class DeviceNetworkTestSystem : EntitySystem
{
    public NetworkPayload LastPayload = default;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeviceNetworkComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
    }

    public void SendBaselineTestEvent(EntityUid uid)
    {
        var ev = new DeviceNetworkPacketEvent(0, "", 0, "", uid, new TestPayload());
        RaiseLocalEvent(uid, ref ev);
    }

    private void OnPacketReceived(Entity<DeviceNetworkComponent> ent, ref DeviceNetworkPacketEvent args)
    {
        LastPayload = args.Data;
    }
}

[Reflect(false)]
public sealed partial class DeviceNetworkTestEntityHandlerSystem : DevicePayloadSystem<DeviceNetworkComponent>
{
    public HandledNetworkPayload LastHandledPayload = default;

    protected override void InitializeDevice()
    {
        SubscribePayload<TestPayloadStatic>(OnStaticPacketReceived);
    }

    private void OnStaticPacketReceived(Entity<DeviceNetworkComponent> ent, ref TestPayloadStatic payload, ref DeviceNetworkPacketData args)
    {
        LastHandledPayload = payload;
    }
}

[Reflect(false)]
public sealed partial class DeviceNetworkTestParallelHandlerSystem : DevicePayloadParallelSystem<DeviceNetworkComponent>
{
    public HandledNetworkPayload LastHandledPayload = default;

    protected override void InitializeDevice()
    {
        SubscribePayload<TestPayloadStatic>(OnStaticPacketReceived);
    }

    private void OnStaticPacketReceived(Entity<DeviceNetworkComponent> ent, ref TestPayloadStatic payload, ref DeviceNetworkPacketData args)
    {
        LastHandledPayload = payload;
    }
}

public sealed partial class TestPayload : NetworkPayload
{
    [DataField]
    public string TestString;

    [DataField]
    public int TestNumber;

    [DataField]
    public bool TestBool;
}

public sealed partial class SecondTestPayload : NetworkPayload;

public sealed partial class TestPayloadStatic : HandledNetworkPayload
{
    [DataField]
    public string TestString;

    [DataField]
    public int TestNumber;

    [DataField]
    public bool TestBool;
}
