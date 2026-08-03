using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Events;
using Robust.Shared.GameObjects;
using Robust.Shared.Reflection;
using Content.Shared.DeviceNetwork.Components;
using Robust.Shared.Analyzers;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.IntegrationTests.Tests.DeviceNetwork;

[Reflect(false)]
public sealed class DeviceNetworkTestSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DeviceNetworkComponent, DeviceNetworkPacketData>(OnBaselinePacketReceived);
        SubscribeLocalEvent<DeviceNetworkComponent, DeviceNetworkPacketEvent<TestPayload>>(OnTypedPacketReceived);
        SubscribeLocalEvent<DeviceNetworkComponent, DeviceNetworkPacketEvent<SecondTestPayload>>(OnTypedPacketReceived);
    }

    public TestPayload LastPayload = default;
    public SecondTestPayload LastPayloadSecond = default;
    public TestPayloadClass LastPayloadClass = default;

    public void SendBaselineTestEvent(EntityUid uid)
    {
        var ev = new DeviceNetworkPacketData(0, 0, 0, 0, uid, new TestPayloadClass());
        RaiseLocalEvent(uid, ref ev);
    }

    private void OnBaselinePacketReceived(Entity<DeviceNetworkComponent> ent, ref DeviceNetworkPacketData args)
    {
        LastPayloadClass = (TestPayloadClass) args.Data;
    }

    private void OnTypedPacketReceived(Entity<DeviceNetworkComponent> ent, ref DeviceNetworkPacketEvent<TestPayload> args)
    {
        LastPayload = args.Data;
    }

    private void OnTypedPacketReceived(Entity<DeviceNetworkComponent> ent, ref DeviceNetworkPacketEvent<SecondTestPayload> args)
    {
        LastPayloadSecond = args.Data;
    }
}

public readonly partial record struct TestPayload(string TestString, int TestNumber, bool TestBool) : INetworkPayload;

public readonly partial record struct SecondTestPayload(string TestString, int TestNumber, bool TestBool) : INetworkPayload;

public sealed partial class TestPayloadClass : INetworkPayload
{
    [DataField]
    public string TestString;

    [DataField]
    public int TestNumber;

    [DataField]
    public bool TestBool;
}
